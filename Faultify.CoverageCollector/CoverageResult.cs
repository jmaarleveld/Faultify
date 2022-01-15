using Mono.Cecil;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Faultify.AssemblyDissection;
using Faultify.MutationSessionProgressTracker;
using Faultify.TestHostRunner;
using Faultify.TestHostRunner.Results;
using Faultify.TestHostRunner.TestHostRunners;
using Faultify.ProjectDuplicator;

namespace Faultify.CoverageCollector
{
    public class CoverageResult
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        ///     This maps a method to all the tests that cover that method.
        /// </summary>
        /// <param name="progressTracker"></param>
        /// <param name="coverageProject"></param>
        /// <param name="testHost"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Dictionary<Tuple<string, int>, HashSet<string>>>
            GetTestsPerMutation(
                IMutationSessionProgressTracker progressTracker,
                ITestProjectDuplication coverageProject,
                TestHost testHost,
                CancellationToken cancellationToken = default)
        {
            // Measure the test coverage 
            progressTracker.LogBeginCoverage();

            // Rewrites assemblies
            PrepareAssembliesForCodeCoverage(coverageProject, testHost);

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

            Logger.Info("Collecting garbage");
            GC.Collect();
            GC.WaitForPendingFinalizers();

            Logger.Info("Freeing test project");
            coverageProject.MarkAsFree();

            // Start test session.
            return GroupMutationsWithTests(coverage);
        }

        /// <summary>
        ///     Injects the test project and all mutation assemblies with coverage injection code.
        ///     This code that is injected will register calls to methods/tests.
        ///     Those calls determine what tests cover which methods.
        /// </summary>
        /// <param name="duplication"></param>
        /// <param name="testHost"></param>
        private static void PrepareAssembliesForCodeCoverage(
            ITestProjectDuplication duplication, TestHost testHost)
        {
            Logger.Info("Preparing assemblies for code coverage");
            ModuleDefinition testModule
                = ModuleDefinition.ReadModule(duplication.TestProjectFile.FullFilePath());

            List<AssemblyAnalyzer> dependencyAssemblies = new List<AssemblyAnalyzer>();
            LoadInMemory(duplication, dependencyAssemblies);

            TestCoverageInjector.Instance.InjectTestCoverage(testModule);
            TestCoverageInjector.Instance.InjectModuleInit(testModule);
            TestCoverageInjector.Instance.InjectAssemblyReferences(testModule);

            using MemoryStream ms = new MemoryStream();
            testModule.Write(ms);
            testModule.Dispose();

            File.WriteAllBytes(testModule.FileName, ms.ToArray());

            foreach (var assembly in dependencyAssemblies)
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
        private Dictionary<Tuple<string, int>, HashSet<string>> GroupMutationsWithTests(
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

        /// <summary>
        ///     Foreach project reference load it in memory as an <see cref="AssemblyAnalyzer"/>.
        /// </summary>
        /// <param name="duplication"></param>
        /// <param name="dependencyAssemblies"></param>
        private static void LoadInMemory(
            ITestProjectDuplication duplication, List<AssemblyAnalyzer> dependencyAssemblies)
        {
            foreach (FileDuplication projectReferencePath in duplication.TestProjectReferences)
            {
                try
                {
                    AssemblyAnalyzer loadProjectReferenceModel
                        = new AssemblyAnalyzer(projectReferencePath.FullFilePath());

                    if (loadProjectReferenceModel.Types.Count > 0)
                    {
                        dependencyAssemblies.Add(loadProjectReferenceModel);
                    }
                }
                catch (FileNotFoundException e)
                {
                    Logger.Error(e
                        , $"Faultify was unable to read the file {projectReferencePath.FullFilePath()}.");
                }
            }
        }
    }
}