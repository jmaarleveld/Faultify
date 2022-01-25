extern alias MC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NLog;
using Faultify.AssemblyDissection;
using Faultify.CodeDecompiler;
using Faultify.MutationCollector;
using Faultify.MutationCollector.Mutation;
using coverageCollector = Faultify.CoverageCollector.CoverageCollector;
using Faultify.ProjectBuilder;
using Faultify.ProjectDuplicator;
using Faultify.MutationSessionProgressTracker;
using Faultify.MutationSessionRunner;
using Faultify.MutationSessionScheduler;
using Faultify.Report;
using Faultify.Report.Models;
using ModuleDefinition = MC::Mono.Cecil.ModuleDefinition;

namespace Faultify.Pipeline
{
    public class Pipeline
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IMutationSessionProgressTracker _progressTracker;
        private readonly Settings _settings;

        private HashSet<int> _timedOutGroups = new HashSet<int>();
        private readonly object _timedOutGroupsLock = new object();
        private int _completedRuns;
        private readonly object _completedRunsLock = new object();
        private int _failedRuns;
        private readonly object _failedRunsLock = new object();
        private int _totalRunsCount;

        private List<ReportData> _reportData
            = new List<ReportData>();

        private readonly object _reportDataLock = new object();

        public Pipeline(IMutationSessionProgressTracker progressTracker, Settings settings)
        {
            _progressTracker = progressTracker;
            _settings = settings;
        }

        /// <summary>
        ///     This should be called to start the pipeline.
        /// </summary>
        /// <param name="testProjectPath"></param>
        public async Task Start(string testProjectPath)
        {
            // Build the project
            IProjectInfo projectInfo = await BuildProject(testProjectPath);

            // Duplicate the test project
            var (coverageProject, dependencyAssemblies, testProjectDuplicator)
                = DuplicateTestProject(projectInfo);

            // Collect mutations for each assembly
            // Collect mutation before bytecode is inserted
            // Call ToList() to prevent lazy evaluation
            var mutations = CollectMutations(
                dependencyAssemblies,
                _settings.MutationLevel,
                _settings.ExcludeMutationGroups.ToHashSet(),
                _settings.ExcludeSingleMutations).ToList().Select(x => x.ToList());

            // Obtain mapping of a method to the tests that cover that method
            _progressTracker.LogBeginCoverage();
            (Dictionary<Tuple<string, int>, HashSet<string>> testsPerMethod, TimeSpan timeout)
                = await coverageCollector.GetTestsPerMutation(
                    coverageProject.TestProjectFile.FullFilePath(),
                    dependencyAssemblies,
                    _settings.TestHost,
                    _settings.TimeOut,
                    CancellationToken.None);

            Logger.Info("Freeing test project");
            coverageProject.MarkAsFree();

            // Map the testsPerMethod with the mutations
            var coveragePerMutation
                = CoverageMapper.MapCoverageToMutations(testsPerMethod, mutations);

            // Generate the mutation test runs based on the mapped coverage and mutations
            Logger.Info("Starting mutation session");
            IMutationTestRunGenerator mutationTestRunGenerator
                = new DefaultMutationTestRunGenerator();
            var mutationTestRuns
                = mutationTestRunGenerator.GenerateMutationTestRuns(coveragePerMutation).ToList();

            // Run the created test runs
            (var mutationCount, TimeSpan allRunsDuration)
                = RunTestRuns(mutationTestRuns, timeout, testProjectDuplicator);

            // Create the report model
            var testProjectName = ModuleDefinition
                .ReadModule(coverageProject.TestProjectFile.FullFilePath()).Name;
            var testProjectReportModel = new TestProjectReportModel(
                testProjectName,
                _reportData,
                _totalRunsCount,
                allRunsDuration);

            _progressTracker.LogEndTestSession(allRunsDuration, _completedRuns,
                mutationCount, testProjectReportModel.ScorePercentage);

            // Cleanup
            testProjectDuplicator.DeleteFolder();

            // Generate the report
            _progressTracker.LogBeginReportBuilding(_settings.ReportType, _settings.ReportPath);
            await GenerateReport(testProjectReportModel);
        }

        /// <summary>
        ///     Manager method for the mutation test runs.
        /// </summary>
        /// <param name="mutationTestRuns"></param>
        /// <param name="timeout"></param>
        /// <param name="testProjectDuplicator"></param>
        private (int, TimeSpan) RunTestRuns(
            ICollection<Dictionary<int, (IMutation, HashSet<string>)>> mutationTestRuns,
            TimeSpan timeout,
            ITestProjectDuplicator testProjectDuplicator)
        {
            // Pre mutation test run initializations
            TimeSpan maxTestDuration = TimeSpan.FromSeconds((timeout * 2).Seconds);

            Stopwatch allRunsStopwatch = new Stopwatch();
            allRunsStopwatch.Start();

            _totalRunsCount = mutationTestRuns.Count;
            var mutationCount = mutationTestRuns.Sum(x => x.Count);

            _progressTracker.LogBeginTestSession(_totalRunsCount, mutationCount, maxTestDuration);

            // Start the mutation test runs
            var runId = 0;
            IEnumerable<Task> tasks = from mutationTestRun in mutationTestRuns
                select RunMutationTestRun(
                    mutationTestRun,
                    testProjectDuplicator,
                    timeout,
                    runId++);

            // Wait until all mutation test runs are finished
            Task.WaitAll(tasks.ToArray());
            allRunsStopwatch.Stop();

            return (mutationCount, allRunsStopwatch.Elapsed);
        }

        /// <summary>
        ///     Method that handles a single mutation test run
        /// </summary>
        /// <param name="testRunData"></param>
        /// <param name="testProjectDuplicator"></param>
        /// <param name="timeout"></param>
        /// <param name="runId"></param>
        private async Task RunMutationTestRun(
            Dictionary<int, (IMutation, HashSet<string>)> testRunData,
            ITestProjectDuplicator testProjectDuplicator,
            TimeSpan timeout,
            int runId)
        {
            // Create the project to work in
            ITestProjectDuplication testProjectDuplication
                = testProjectDuplicator.MakeCopy(runId + 2);

            // The old assemblies were bound to the first project;
            // Create assemblies from the new project bound to this copy
            Dictionary<string, AssemblyAnalyzer> dependencyAssemblies = LoadInMemory(
                testProjectDuplication);

            // Get mutations which are also bound to the new copy
            var mutationTestRun = GetMutationsForDuplication(
                testRunData,
                dependencyAssemblies);

            try
            {
                // Obtain a Dictionary of IMutations
                var mutations = mutationTestRun
                    .ToDictionary(pair => pair.Key, pair => pair.Value.Item1).Values.ToList();

                var originalSourceCode = SourceCollector.CollectSourceCode(
                    mutations, testProjectDuplication, dependencyAssemblies);

                var timedOutGroupsCopy = CopyTimedOutGroups();

                MutationApplier.MutationApplier.ApplyMutations(
                    mutationTestRun.ToDictionary(pair => pair.Key, pair => pair.Value.Item1),
                    timedOutGroupsCopy,
                    dependencyAssemblies,
                    testProjectDuplication);

                var mutatedSourceCode = SourceCollector.CollectSourceCode(
                    mutations, testProjectDuplication, dependencyAssemblies);

                Stopwatch singRunsStopwatch = new Stopwatch();
                singRunsStopwatch.Start();

                var (newTimedOutGroups, mutationTestRunResult) = await StartMutationSession(
                    mutationTestRun, timeout, timedOutGroupsCopy, testProjectDuplication);

                UpdateTimedOutGroups(newTimedOutGroups);

                var newReportData = CollectReportData(mutationTestRunResult,
                    mutationTestRun, mutations, originalSourceCode, mutatedSourceCode
                    , singRunsStopwatch.Elapsed);
                UpdateReportData(newReportData);

                singRunsStopwatch.Stop();
                singRunsStopwatch.Reset();
            }
            catch (Exception e)
            {
                // Test run failed
                HandleMutationTestRunException(e);
            }
            finally
            {
                // Successfully completed the test run
                FinishMutationTestRun();
            }
        }

        /// <summary>
        ///     Generate the mutations bound to a specific copy
        ///     of the test project.
        /// </summary>
        /// <param name="testRunData"></param>
        /// <param name="dependencyAssemblies"></param>
        private Dictionary<int, (IMutation, HashSet<string>)> GetMutationsForDuplication(
            Dictionary<int, (IMutation, HashSet<string>)> testRunData,
            Dictionary<string, AssemblyAnalyzer> dependencyAssemblies)
        {
            var mutationTestRun = testRunData.ToDictionary(
                pair => pair.Key,
                pair => (
                    // Lookup the assembly analyzer and get a new mutation 
                    dependencyAssemblies[pair.Value.Item1.AssemblyName]
                        .GetEquivalentMutation(pair.Value.Item1),
                    pair.Value.Item2
                )
            );
            return mutationTestRun;
        }

        private async Task<Tuple<HashSet<int>, Dictionary<string, TestOutcome>>>
            StartMutationSession(
                Dictionary<int, (IMutation, HashSet<string>)> mutationTestRun,
                TimeSpan timeout,
                HashSet<int> timedOutGroupsCopy,
                ITestProjectDuplication testProjectDuplication)
        {
            var testsPerGroup
                = mutationTestRun.ToDictionary(pair => pair.Key, pair => pair.Value.Item2);
            IMutationSessionRunner mutationSessionRunner
                = new MutationSessionRunner.MutationSessionRunner();
            return await mutationSessionRunner.StartMutationSession(
                timeout,
                testsPerGroup,
                timedOutGroupsCopy,
                _settings.TestHost,
                testProjectDuplication.TestProjectFile.FullFilePath());
        }

        private HashSet<int> CopyTimedOutGroups()
        {
            HashSet<int> timedOutGroupsCopy;
            lock (_timedOutGroupsLock)
            {
                timedOutGroupsCopy = _timedOutGroups.ToHashSet();
            }

            return timedOutGroupsCopy;
        }

        private void UpdateTimedOutGroups(IEnumerable<int> newTimedOutGroups)
        {
            lock (_timedOutGroupsLock)
            {
                _timedOutGroups.UnionWith(newTimedOutGroups);
            }
        }

        private void UpdateReportData(IEnumerable<ReportData> newReportData)
        {
            lock (_reportDataLock)
            {
                _reportData
                    = _reportData.Concat(newReportData).ToList();
            }
        }

        private void HandleMutationTestRunException(Exception e)
        {
            _progressTracker.Log(
                $"The test process encountered an unexpected error. Continuing without this test run. Please consider to submit an github issue. {e}\n"
                ,
                LogMessageType.Error);
            lock (_failedRunsLock)
            {
                _failedRuns += 1;
            }

            Logger.Error(e, "The test process encountered an unexpected error.");
        }

        private void FinishMutationTestRun()
        {
            lock (_completedRunsLock)
            {
                _completedRuns += 1;

                lock (_failedRunsLock)
                {
                    _progressTracker.LogTestRunUpdate(_completedRuns, _totalRunsCount
                        , _failedRuns);
                }
            }
        }

        /// <summary>
        ///     Converts the result of a mutation test run to test outcomes
        /// </summary>
        /// <param name="testOutcomes"></param>
        /// <param name="mutationTestRun"></param>
        /// <param name="mutations"></param>
        /// <param name="originalSourceCode"></param>
        /// <param name="mutatedSourceCode"></param>
        /// <param name="testRunDuration"></param>
        /// <returns></returns>
        private static IEnumerable<ReportData> CollectReportData(
            Dictionary<string, TestOutcome> testOutcomes,
            Dictionary<int, (IMutation, HashSet<string>)> mutationTestRun,
            List<IMutation> mutations,
            IEnumerable<string> originalSourceCode,
            IEnumerable<string> mutatedSourceCode,
            TimeSpan testRunDuration)
        {
            var reportDataList
                = new List<ReportData>();

            var filteredTestOutcomes = FilterDuplicateTestOutcomes(testOutcomes, mutationTestRun);

            var coupledSourceCode = mutations.Zip(originalSourceCode)
                .Zip(mutatedSourceCode, (first, second) => (first.First, first.Second, second));

            foreach (var tuple in coupledSourceCode)
            {
                var pair = mutationTestRun.First(pair => pair.Value.Item1 == tuple.First);
                foreach (var testName in pair.Value.Item2)
                {
                    // some test names are filtered out now, so check needed
                    if (!filteredTestOutcomes.ContainsKey(testName)) continue;
                    reportDataList.Add(
                        new ReportData(
                            testName,
                            filteredTestOutcomes[testName],
                            tuple.First,
                            tuple.Second,
                            tuple.second,
                            testRunDuration));
                }
            }

            return reportDataList;
        }

        /// <summary>
        ///     A single mutation can have multiple tests, and therefore multiple TestOutcomes.
        ///     However, the results should only show one TestOutcome. This method will make sure
        ///     only one TestOutcome per mutation ends up in the result. If at least one of the
        ///     tests for a mutation was killed, the TestOutcome will also be: killed.
        /// </summary>
        /// <param name="testOutcomes"></param>
        /// <param name="mutationTestRun"></param>
        /// <returns></returns>
        private static Dictionary<string, TestOutcome> FilterDuplicateTestOutcomes(
            Dictionary<string, TestOutcome> testOutcomes,
            Dictionary<int, (IMutation, HashSet<string>)> mutationTestRun)
        {
            Dictionary<string, TestOutcome> filteredTestOutcomes
                = new Dictionary<string, TestOutcome>();

            foreach (var (_, tests) in mutationTestRun.Values)
            {
                string? currentTest = null;
                TestOutcome currentTestOutcome = TestOutcome.Passed;

                foreach (var test in tests.Where(test =>
                    testOutcomes.ContainsKey(test) && currentTestOutcome == TestOutcome.Passed))
                {
                    currentTest = test;
                    currentTestOutcome = testOutcomes[test];
                }

                if (currentTest != null)
                {
                    filteredTestOutcomes[currentTest] = currentTestOutcome;
                }
            }

            return filteredTestOutcomes;
        }

        private static IEnumerable<IEnumerable<IMutation>> CollectMutations(
            Dictionary<string, AssemblyAnalyzer> dependencyAssemblies,
            MutationLevel mutationLevel,
            HashSet<string> excludeGroup,
            HashSet<string> excludeSingular)
        {
            var allMutations = new List<IEnumerable<IEnumerable<IMutation>>>();

            foreach (var assembly in dependencyAssemblies.Values)
            {
                var mutations =
                    assembly.AllMutations(mutationLevel, excludeGroup, excludeSingular);
                allMutations.Add(mutations);
            }

            // Flatten result
            return allMutations.SelectMany(x => x);
        }

        /// <summary>
        ///     This can be used to create a test project, which can be used to perform the
        ///     coverage analysis on.
        /// </summary>
        /// <param name="projectInfo"></param>
        /// <returns></returns>
        private static Tuple<ITestProjectDuplication, Dictionary<string, AssemblyAnalyzer>,
                ITestProjectDuplicator>
            DuplicateTestProject(
                IProjectInfo projectInfo)
        {
            // Copy project N times
            ITestProjectDuplicator testProjectCopier
                = new TestProjectDuplicator(Directory.GetParent(projectInfo.AssemblyPath).FullName);

            // This is for some reason necessary when running tests with Dotnet,
            // otherwise the coverage analysis breaks future clones.
            testProjectCopier.MakeInitialCopy(projectInfo);

            // Begin code coverage on first project.
            ITestProjectDuplication testProjectDuplication = testProjectCopier.MakeCopy(1);

            // Load each project in memory as an AssemblyAnalyzer
            Dictionary<string, AssemblyAnalyzer> dependencyAssemblies
                = LoadInMemory(testProjectDuplication);

            return new Tuple<ITestProjectDuplication, Dictionary<string, AssemblyAnalyzer>,
                ITestProjectDuplicator>(
                testProjectDuplication
                , dependencyAssemblies, testProjectCopier);
        }

        /// <summary>
        ///     Builds the project at the given project path.
        /// </summary>
        /// <param name="projectPath"></param>
        /// <returns></returns>
        private async Task<IProjectInfo> BuildProject(string projectPath)
        {
            _progressTracker.LogBeginPreBuilding();
            IProjectReader projectReader = new ProjectReader();
            _progressTracker.Report($"Building {Path.GetFileName(projectPath)}\n");
            IProjectInfo projectInfo
                = await projectReader.ReadAndBuildProjectAsync(projectPath);
            _progressTracker.LogEndPreBuilding();
            return projectInfo;
        }

        /// <summary>
        ///     Foreach project reference load it in memory as an <see cref="AssemblyAnalyzer"/>.
        /// </summary>
        /// <param name="duplication"></param>
        private static Dictionary<string, AssemblyAnalyzer> LoadInMemory(
            ITestProjectDuplication duplication)
        {
            Dictionary<string, AssemblyAnalyzer> dependencyAssemblies
                = new Dictionary<string, AssemblyAnalyzer>();

            foreach (FileDuplication projectReferencePath in duplication.TestProjectReferences)
            {
                try
                {
                    AssemblyAnalyzer loadProjectReferenceModel
                        = new AssemblyAnalyzer(projectReferencePath.FullFilePath());

                    if (loadProjectReferenceModel.Types.Count > 0)
                    {
                        dependencyAssemblies[loadProjectReferenceModel.Module.Assembly.Name.Name]
                            = loadProjectReferenceModel;
                    }
                }
                catch (FileNotFoundException e)
                {
                    Logger.Error(e
                        , $"Faultify was unable to read the file {projectReferencePath.FullFilePath()}.");
                }
            }

            return dependencyAssemblies;
        }

        /// <summary>
        /// Builds a report based on the test results and program settings
        /// </summary>
        /// <param name="testResult">Report model with the test results</param>
        private async Task GenerateReport(TestProjectReportModel testResult)
        {
            var model = new MutationProjectReportModel();
            model.TestProjects.Add(testResult);

            var reporter = ReporterFactory.CreateReporter(_settings.ReporterType);
            byte[] reportBytes = await reporter.CreateReportAsync(model);

            string reportFileName = DateTime.Now.ToString("yy-MM-dd-H-mm") + reporter.FileExtension;
            string reportFullPath = Path.Combine(_settings.OutputDirectory, reportFileName);
            await File.WriteAllBytesAsync(reportFullPath, reportBytes);
        }
    }
}