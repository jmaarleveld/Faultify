extern alias MC;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Faultify.AssemblyDissection;
using Faultify.ProjectBuilder;
using Faultify.TestHostRunner;
using Faultify.TestHostRunner.Results;
using Faultify.TestHostRunner.TestHostRunners;
using Faultify.ProjectDuplicator;
using MC::Mono.Cecil;

namespace Faultify.CoverageCollector
{
    public class CoverageCollector
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        ///     This maps a method to all the tests that cover that method.
        /// </summary>
        /// <param name="coverageProject"></param>
        /// <param name="dependencyAssemblies"></param>
        /// <param name="testHost"></param>
        /// <param name="timeoutSetting"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<Tuple<Dictionary<Tuple<string, int>, HashSet<string>>, TimeSpan>>
            GetTestsPerMutation(
                ITestProjectDuplication coverageProject,
                Dictionary<string, AssemblyAnalyzer> dependencyAssemblies,
                TestHost testHost,
                TimeSpan timeoutSetting,
                CancellationToken cancellationToken = default)
        {
            // Rewrites assemblies
            PrepareAssembliesForCodeCoverage(coverageProject, testHost, dependencyAssemblies);

            Stopwatch coverageTimer = new Stopwatch();
            coverageTimer.Start();
            Dictionary<string, List<Tuple<string, int>>>? coverage =
                await RunCoverage(coverageProject.TestProjectFile.FullFilePath(), testHost
                    , cancellationToken);
            coverageTimer.Stop();
            if (coverage == null)
            {
                Logger.Fatal("Coverage failed exiting with exit code 16");
                Environment.Exit(16);
            }

            TimeSpan timeout = CreateTimeOut(coverageTimer, timeoutSetting);

            Logger.Info("Collecting garbage");
            GC.Collect();
            GC.WaitForPendingFinalizers();

            Logger.Info("Freeing test project");
            coverageProject.MarkAsFree();

            // Start test session.
            var testsPerMutation = GroupMutationsWithTests(coverage);
            return new Tuple<Dictionary<Tuple<string, int>, HashSet<string>>, TimeSpan>(
                testsPerMutation, timeout);
        }

        /// <summary>
        ///     Injects the test project and all mutation assemblies with coverage injection code.
        ///     This code that is injected will register calls to methods/tests.
        ///     Those calls determine what tests cover which methods.
        /// </summary>
        /// <param name="coverageProject"></param>
        /// <param name="testHost"></param>
        /// <param name="dependencyAssemblies"></param>
        private static void PrepareAssembliesForCodeCoverage(
            ITestProjectDuplication coverageProject, TestHost testHost
            , Dictionary<string, AssemblyAnalyzer> dependencyAssemblies)
        {
            Logger.Info("Preparing assemblies for code coverage");
            ModuleDefinition testModule
                = ModuleDefinition.ReadModule(coverageProject.TestProjectFile.FullFilePath());

            TestCoverageInjector.Instance.InjectTestCoverage(testModule);
            TestCoverageInjector.Instance.InjectModuleInit(testModule);
            TestCoverageInjector.Instance.InjectAssemblyReferences(testModule);

            using MemoryStream ms = new MemoryStream();
            testModule.Write(ms);
            testModule.Dispose();

            File.WriteAllBytes(testModule.FileName, ms.ToArray());

            foreach (var assembly in dependencyAssemblies.Values)
            {
                Logger.Trace($"Writing assembly {assembly.Module.FileName}");
                TestCoverageInjector.Instance.InjectAssemblyReferences(assembly.Module);
                TestCoverageInjector.Instance.InjectTargetCoverage(assembly.Module);
                assembly.Flush();
                assembly.Dispose();
            }

            if (testHost == TestHost.XUnit)
            {
                DirectoryInfo testDirectory = new FileInfo(testModule.FileName).Directory;
                string xunitConfigFileName
                    = Path.Combine(testDirectory.FullName, "xunit.runner.json");
                JObject xunitCoverageSettings
                    = JObject.FromObject(new { parallelizeTestCollections = false });
                if (!File.Exists(xunitConfigFileName))
                {
                    File.WriteAllText(xunitConfigFileName, xunitCoverageSettings.ToString());
                }
                else
                {
                    JObject? originalJsonConfig
                        = JObject.Parse(File.ReadAllText(xunitConfigFileName));
                    originalJsonConfig.Merge(xunitCoverageSettings);
                    File.WriteAllText(xunitConfigFileName, originalJsonConfig.ToString());
                }
            }
        }

        /// <summary>
        ///     Runs the coverage process.
        ///     This process will get all tests with the methods covered by that test.
        /// </summary>
        /// <param name="testAssemblyPath"></param>
        /// <param name="testHost"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task<Dictionary<string, List<Tuple<string, int>>>?> RunCoverage(
            string testAssemblyPath, TestHost testHost, CancellationToken cancellationToken)
        {
            Logger.Info("Running coverage analysis");
            using MemoryMappedFile file = ResultsUtils
                .CreateCoverageMemoryMappedFile();
            ITestHostRunner testRunner;
            try
            {
                testRunner = TestHostRunnerFactory
                    .CreateTestRunner(testAssemblyPath, TimeSpan.FromSeconds(300), testHost);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Unable to create Test Runner returning null");
                return null;
            }

            return await testRunner.RunCodeCoverage(cancellationToken);
        }

        /// <summary>
        ///     Groups methods with all tests which cover those methods.
        /// </summary>
        /// <param name="coverage"></param>
        /// <returns></returns>
        private static Dictionary<Tuple<string, int>, HashSet<string>> GroupMutationsWithTests(
            Dictionary<string, List<Tuple<string, int>>> coverage)
        {
            Logger.Info("Grouping mutations with registered tests");
            // Group mutations with tests.
            Dictionary<Tuple<string, int>, HashSet<string>> testsPerMutation =
                new Dictionary<Tuple<string, int>, HashSet<string>>();
            foreach (var (testName, mutationIds) in coverage)
            {
                foreach (var registeredCoverage in mutationIds)
                {
                    if (!testsPerMutation.TryGetValue(registeredCoverage, out var testNames))
                    {
                        testNames = new HashSet<string>();
                        testsPerMutation.Add(registeredCoverage, testNames);
                    }

                    testNames.Add(testName);
                }
            }

            return testsPerMutation;
        }

        /// Sets the time out for the mutations to be either the specified number of seconds or the time it takes to run
        /// the test project.
        /// When timeout is less then 0.51 seconds it will be set to .51 seconds to make sure the MaxTestDuration is at
        /// least one second.
        private static TimeSpan CreateTimeOut(Stopwatch stopwatch, TimeSpan timeoutSetting)
        {
            TimeSpan timeout = timeoutSetting;
            if (timeoutSetting.Equals(TimeSpan.FromSeconds(0)))
            {
                timeout = stopwatch.Elapsed;
            }

            return timeout < TimeSpan.FromSeconds(.51) ? TimeSpan.FromSeconds(.51) : timeout;
        }
    }
}