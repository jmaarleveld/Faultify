using Mono.Cecil;
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
using Faultify.MutationSessionProgressTracker;
using Faultify.TestHostRunner;
using Faultify.TestHostRunner.Results;
using Faultify.TestHostRunner.TestHostRunners;
using Faultify.ProjectDuplicator;
using Faultify.ProjectDuplicator.ProjectAnalyzing;

namespace Faultify.CoverageCollector
{
    public class CoverageResult
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly TestHost _testHost;
        private readonly TimeSpan _timeOut;
        
        public async Task<Dictionary<Tuple<string, int>, HashSet<string>>>
            GetCoverageResult(
                MutationSessionProgressTracker.MutationSessionProgressTracker progressTracker,
                string testProjectPath,
                ITestProjectDuplication testProjectDuplication,
                CancellationToken cancellationToken = default)
        {
            // Build project
            progressTracker.LogBeginPreBuilding();
            IProjectInfo? projectInfo = await BuildProject(progressTracker, testProjectPath);
            progressTracker.LogEndPreBuilding();

            // Copy project N times
            TestProjectDuplicator? testProjectCopier =
                new TestProjectDuplicator(Directory.GetParent(projectInfo.AssemblyPath).FullName);

            // This is for some reason necessary when running tests with Dotnet,
            // otherwise the coverage analysis breaks future clones.
            testProjectCopier.MakeInitialCopy(projectInfo);

            // Begin code coverage on first project.
            TestProjectDuplication coverageProject = testProjectCopier.MakeCopy(1);
            TestProjectInfo coverageProjectInfo = GetTestProjectInfo(coverageProject, projectInfo);

            // Measure the test coverage 
            progressTracker.LogBeginCoverage();

            // Rewrites assemblies
            PrepareAssembliesForCodeCoverage(coverageProjectInfo);

            Stopwatch? coverageTimer = new Stopwatch();
            coverageTimer.Start();
            Dictionary<string, List<Tuple<string, int>>>? coverage =
                await RunCoverage(coverageProject.TestProjectFile.FullFilePath(), cancellationToken);
            coverageTimer.Stop();
            if (coverage == null)
            {
                Logger.Fatal("Coverage failed exiting with exit code 16");
                Environment.Exit(16);
            }

            TimeSpan timeout = CreateTimeOut(coverageTimer);

            Logger.Info("Collecting garbage");
            GC.Collect();
            GC.WaitForPendingFinalizers();

            Logger.Info("Freeing test project");
            coverageProject.MarkAsFree();

            // Start test session.
            return GroupMutationsWithTests(coverage);
        }
        
        /// Sets the time out for the mutations to be either the specified number of seconds or the time it takes to run
        /// the test project.
        /// When timeout is less then 0.51 seconds it will be set to .51 seconds to make sure the MaxTestDuration is at
        /// least one second.
        private TimeSpan CreateTimeOut(Stopwatch stopwatch)
        {
            TimeSpan timeOut = _timeOut;
            if (_timeOut.Equals(TimeSpan.FromSeconds(0)))
            {
                timeOut = stopwatch.Elapsed;
            }

            return timeOut < TimeSpan.FromSeconds(.51) ? TimeSpan.FromSeconds(.51) : timeOut;
        }
        
        /// <summary>
        ///     Returns information about the test project.
        /// </summary>
        /// <returns></returns>
        private TestProjectInfo GetTestProjectInfo(TestProjectDuplication duplication, IProjectInfo testProjectInfo)
        {
            TestHost testFramework = GetTestHost(testProjectInfo);

            TestProjectInfo? projectInfo = new TestProjectInfo(
                testFramework,
                ModuleDefinition.ReadModule(duplication.TestProjectFile.FullFilePath())
            );
            LoadInMemory(duplication, projectInfo);

            return projectInfo;
        }
        
        /// <summary>
        ///     Foreach project reference load it in memory as an <see cref="AssemblyMutator"/>.
        /// </summary>
        /// <param name="duplication"></param>
        /// <param name="projectInfo"></param>
        private static void LoadInMemory(TestProjectDuplication duplication, TestProjectInfo projectInfo)
        {
            foreach (FileDuplication projectReferencePath in duplication.TestProjectReferences)
            {
                try
                {
                    AssemblyMutator loadProjectReferenceModel = new AssemblyMutator(projectReferencePath.FullFilePath());

                    if (loadProjectReferenceModel.Types.Count > 0)
                    {
                        projectInfo.DependencyAssemblies.Add(loadProjectReferenceModel);
                    }
                }
                catch (FileNotFoundException e)
                {
                    Logger.Error(e, $"Faultify was unable to read the file {projectReferencePath.FullFilePath()}.");
                }
            }
        }
        
        private TestHost GetTestHost(IProjectInfo projectInfo)
        {
            string projectFile = File.ReadAllText(projectInfo.ProjectFilePath);

            if (Regex.Match(projectFile, "xunit").Captures.Any())
            {
                return TestHost.XUnit;
            }

            if (Regex.Match(projectFile, "nunit").Captures.Any())
            {
                return TestHost.NUnit;
            }

            if (Regex.Match(projectFile, "mstest").Captures.Any())
            {
                return TestHost.MsTest;
            }

            return TestHost.DotnetTest;
        }
        
        /// <summary>
        ///     Builds the project at the given project path.
        /// </summary>
        /// <param name="sessionProgressTracker"></param>
        /// <param name="projectPath"></param>
        /// <returns></returns>
        private async Task<IProjectInfo> BuildProject(
            MutationSessionProgressTracker.MutationSessionProgressTracker sessionProgressTracker,
            string projectPath
        )
        {
            ProjectReader? projectReader = new ProjectReader();
            return await projectReader.ReadProjectAsync(projectPath, sessionProgressTracker);
        }
        
        /// <summary>
        ///     Injects the test project and all mutation assemblies with coverage injection code.
        ///     This code that is injected will register calls to methods/tests.
        ///     Those calls determine what tests cover which methods.
        /// </summary>
        /// <param name="projectInfo"></param>
        private void PrepareAssembliesForCodeCoverage(TestProjectInfo projectInfo)
        {
            Logger.Info("Preparing assemblies for code coverage");
            TestCoverageInjector.Instance.InjectTestCoverage(projectInfo.TestModule);
            TestCoverageInjector.Instance.InjectModuleInit(projectInfo.TestModule);
            TestCoverageInjector.Instance.InjectAssemblyReferences(projectInfo.TestModule);

            using MemoryStream? ms = new MemoryStream();
            projectInfo.TestModule.Write(ms);
            projectInfo.TestModule.Dispose();

            File.WriteAllBytes(projectInfo.TestModule.FileName, ms.ToArray());

            foreach (var assembly in projectInfo.DependencyAssemblies)
            {
                Logger.Trace($"Writing assembly {assembly.Module.FileName}");
                TestCoverageInjector.Instance.InjectAssemblyReferences(assembly.Module);
                TestCoverageInjector.Instance.InjectTargetCoverage(assembly.Module);
                assembly.Flush();
                assembly.Dispose();
            }

            if (projectInfo.TestFramework == TestHost.XUnit)
            {
                DirectoryInfo testDirectory = new FileInfo(projectInfo.TestModule.FileName).Directory;
                string xunitConfigFileName = Path.Combine(testDirectory.FullName, "xunit.runner.json");
                JObject xunitCoverageSettings = JObject.FromObject(new { parallelizeTestCollections = false });
                if (!File.Exists(xunitConfigFileName))
                {
                    File.WriteAllText(xunitConfigFileName, xunitCoverageSettings.ToString());
                }
                else
                {
                    JObject? originalJsonConfig = JObject.Parse(File.ReadAllText(xunitConfigFileName));
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
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<Dictionary<string, List<Tuple<string, int>>>?> RunCoverage(string testAssemblyPath, CancellationToken cancellationToken)
        {
            Logger.Info("Running coverage analysis");
            using MemoryMappedFile? file = ResultsUtils
            .CreateCoverageMemoryMappedFile();
            ITestHostRunner testRunner;
            try
            {
                testRunner = TestHostRunnerFactory
                    .CreateTestRunner(testAssemblyPath, TimeSpan.FromSeconds(300), _testHost);
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
        private Dictionary<Tuple<string, int>, HashSet<string>> GroupMutationsWithTests(Dictionary<string, List<Tuple<string, int>>> coverage)
        {
            Logger.Info("Grouping mutations with registered tests");
            // Group mutations with tests.
            Dictionary<Tuple<string, int>, HashSet<string>>? testsPerMutation =
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
    }
}