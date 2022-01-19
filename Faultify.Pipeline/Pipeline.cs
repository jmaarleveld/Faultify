﻿using System;
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
using Faultify.TestHostRunner;

namespace Faultify.Pipeline
{
    public class Pipeline
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IMutationSessionProgressTracker _progressTracker;
        private readonly TestHost _testHost;

        private HashSet<int> _timedOutGroups = new HashSet<int>();
        private readonly object _timedOutGroupsLock = new object();
        private int _completedRuns;
        private readonly object _completedRunsLock = new object();
        private int _failedRuns;
        private readonly object _failedRunsLock = new object();
        private int _totalRunsCount;

        private List<ReportData> _coupledTestOutcomes
            = new List<ReportData>();

        private readonly object _coupledTestOutcomesLock = new object();

        public Pipeline(IMutationSessionProgressTracker progressTracker, TestHost testHost)
        {
            _progressTracker = progressTracker;
            _testHost = testHost;
        }

        /// <summary>
        ///     This should be called to start the pipeline.
        /// </summary>
        /// <param name="testProjectPath"></param>
        public async void Start(string testProjectPath)
        {
            // Build the project
            IProjectInfo projectInfo = await BuildProject(testProjectPath);

            // Duplicate the test project
            var (coverageProject, dependencyAssemblies, testProjectDuplicator)
                = DuplicateTestProject(projectInfo);

            // Obtain mapping of a method to the tests that cover that method
            _progressTracker.LogBeginCoverage();
            (Dictionary<Tuple<string, int>, HashSet<string>> testsPerMethod, TimeSpan timeout)
                = await coverageCollector.GetTestsPerMutation(
                    coverageProject,
                    dependencyAssemblies,
                    projectInfo,
                    TIMESPAN_PROGRAMSETTING_HERE!,
                    CancellationToken.None);

            // Collect mutations for each assembly
            var mutations = CollectMutations(dependencyAssemblies, MUTATIONLEVEL, EXCLUDEGROUP
                , EXCLUDESINGULAR);

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
            RunTestRuns(mutationTestRuns, timeout, dependencyAssemblies, testProjectDuplicator);

            // Do the reporting stuff

            // Cleanup
            testProjectDuplicator.DeleteFolder();
        }

        /// <summary>
        ///     Manager method for the mutation test runs.
        /// </summary>
        /// <param name="mutationTestRuns"></param>
        /// <param name="timeout"></param>
        /// <param name="dependencyAssemblies"></param>
        /// <param name="testProjectDuplicator"></param>
        private void RunTestRuns(
            ICollection<Dictionary<int, (IMutation, HashSet<string>)>> mutationTestRuns,
            TimeSpan timeout,
            Dictionary<string, AssemblyAnalyzer> dependencyAssemblies,
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
                    dependencyAssemblies,
                    testProjectDuplicator,
                    timeout,
                    runId++);

            // Wait until all mutation test runs are finished
            Task.WaitAll(tasks.ToArray());
            allRunsStopwatch.Stop();
        }

        /// <summary>
        ///     Method that handles a single mutation test run
        /// </summary>
        /// <param name="mutationTestRun"></param>
        /// <param name="dependencyAssemblies"></param>
        /// <param name="testProjectDuplicator"></param>
        /// <param name="timeout"></param>
        /// <param name="runId"></param>
        private async Task RunMutationTestRun(
            Dictionary<int, (IMutation, HashSet<string>)> mutationTestRun,
            Dictionary<string, AssemblyAnalyzer> dependencyAssemblies,
            ITestProjectDuplicator testProjectDuplicator,
            TimeSpan timeout,
            int runId)
        {
            // Create the project to work in
            ITestProjectDuplication testProjectDuplication
                = testProjectDuplicator.MakeCopy(runId + 2);

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
                    mutationTestRun, mutations, originalSourceCode, mutatedSourceCode);
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
                _progressTracker,
                testsPerGroup,
                timedOutGroupsCopy,
                _testHost,
                testProjectDuplication);
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
            lock (_coupledTestOutcomesLock)
            {
                _coupledTestOutcomes
                    = _coupledTestOutcomes.Concat(newReportData).ToList();
            }
        }

        private void HandleMutationTestRunException(Exception e)
        {
            _progressTracker.Log(
                $"The test process encountered an unexpected error. Continuing without this test run. Please consider to submit an github issue. {e}"
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
        /// <returns></returns>
        private static IEnumerable<ReportData> CollectReportData(
            Dictionary<string, TestOutcome> testOutcomes,
            Dictionary<int, (IMutation, HashSet<string>)> mutationTestRun,
            List<IMutation> mutations,
            IEnumerable<string> originalSourceCode,
            IEnumerable<string> mutatedSourceCode)
        {
            var coupledTestOutcomes
                = new List<ReportData>();

            var coupledSourceCode = mutations.Zip(originalSourceCode)
                .Zip(mutatedSourceCode, (first, second) => (first.First, first.Second, second));

            foreach (var tuple in coupledSourceCode)
            {
                var pair = mutationTestRun.First(pair => pair.Value.Item1 == tuple.First);
                foreach (var testName in pair.Value.Item2)
                {
                    coupledTestOutcomes.Add(
                        new ReportData(testName, testOutcomes[testName], tuple.First.AnalyzerName,
                            tuple.First.AnalyzerDescription, tuple.Second, tuple.second));
                }
            }

            return coupledTestOutcomes;
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
            IProjectInfo projectInfo
                = await projectReader.ReadAndBuildProjectAsync(projectPath, _progressTracker);
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
                        dependencyAssemblies[projectReferencePath.FullFilePath()]
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
    }
}