extern alias MC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Faultify.MutationCollector;
using Faultify.MutationCollector.AssemblyMutator;
using Faultify.Core.ProjectAnalyzing;
using Faultify.Injection;
using Faultify.Report;
using Faultify.Report.Models;
using Faultify.TestRunner.Logging;
using Faultify.TestRunner.ProjectDuplication;
using Faultify.TestRunner.Shared;
using Faultify.TestRunner.TestRun;
using Faultify.TestRunner.TestRun.TestHostRunners;
using MC::Mono.Cecil;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NLog;

namespace Faultify.TestRunner
{
    public class MutationTestProject
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly MutationLevel _mutationLevel;
        private readonly TestHost _testHost;
        private readonly string _testProjectPath;
        private readonly TimeSpan _timeOut;
        private readonly HashSet<string> _excludeGroup;
        private readonly HashSet<string> _excludeSingular;

        public MutationTestProject(
            string testProjectPath,
            MutationLevel mutationLevel,
            TestHost testHost,
            TimeSpan timeOut,
            HashSet<string> excludeGroup,
            HashSet<string> excludeSingular
        )
        {
            _testProjectPath = testProjectPath;
            _mutationLevel = mutationLevel;
            _testHost = testHost;
            _timeOut = timeOut;
            _excludeGroup = excludeGroup;
            _excludeSingular = excludeSingular;
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
        ///     Executes the mutation test session for the given test project.
        ///     Algorithm:
        ///     1. Build project
        ///     2. Calculate for each test which mutations they cover.
        ///     3. Rebuild to remove injected code.
        ///     4. Run test session.
        ///     4a. Generate optimized test runs.
        ///     5. Build report.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<TestProjectReportModel> Test(
            MutationSessionProgressTracker progressTracker,
            CancellationToken cancellationToken = default
        )
        {
            return StartMutationTestSession(coverageProjectInfo, testsPerMutation, progressTracker,
                timeout, testProjectCopier, _testHost);
        }

        /// <summary>
        ///     Starts the mutation test session and returns the report with results.
        /// </summary>
        /// <param name="testProjectInfo"></param>
        /// <param name="testsPerMutation"></param>
        /// <param name="sessionProgressTracker"></param>
        /// <param name="coverageTestRunTime"></param>
        /// <param name="testProjectDuplicationPool"></param>
        /// <returns></returns>
        private TestProjectReportModel StartMutationTestSession(
            TestProjectInfo testProjectInfo,
            Dictionary<Tuple<string, int>, HashSet<string>> testsPerMutation,
            MutationSessionProgressTracker sessionProgressTracker,
            TimeSpan coverageTestRunTime,
            TestProjectDuplicator testProjectDuplicator,
            TestHost testHost
        )
        {
            Logger.Info("Starting mutation session");
            // Generate the mutation test runs for the mutation session.
            DefaultMutationTestRunGenerator? defaultMutationTestRunGenerator = new DefaultMutationTestRunGenerator();
            IEnumerable<IMutationTestRun>? runs = defaultMutationTestRunGenerator.GenerateMutationTestRuns(
                testsPerMutation, testProjectInfo,
                _mutationLevel, _excludeGroup, _excludeSingular);
            // Double the time the code coverage took such that test runs have some time run their tests (needs to be in seconds).
            TimeSpan maxTestDuration = TimeSpan.FromSeconds((coverageTestRunTime * 2).Seconds);

            TestProjectReportModelBuilder reportBuilder =
                new TestProjectReportModelBuilder(testProjectInfo.TestModule.Name);


            Stopwatch? allRunsStopwatch = new Stopwatch();
            allRunsStopwatch.Start();

            List<IMutationTestRun>? mutationTestRuns = runs.ToList();
            int totalRunsCount = mutationTestRuns.Count();
            int mutationCount = mutationTestRuns.Sum(x => x.MutationCount);
            var completedRuns = 0;
            var failedRuns = 0;

            sessionProgressTracker.LogBeginTestSession(totalRunsCount, mutationCount, maxTestDuration);

            // Stores timed out mutations which will be excluded from test runs if they occur. 
            // Timed out mutations will be removed because they can cause serious test delays.
            List<MutationVariantIdentifier>? timedOutMutations = new List<MutationVariantIdentifier>();

            async Task RunTestRun(IMutationTestRun testRun)
            {
                TestProjectDuplication? testProject = testProjectDuplicator.MakeCopy(testRun.RunId + 2);

                try
                {
                    testRun.InitializeMutations(testProject, timedOutMutations, _excludeGroup, _excludeSingular);

                    Stopwatch? singRunsStopwatch = new Stopwatch();
                    singRunsStopwatch.Start();
                    IEnumerable<TestRunResult> results = await testRun.RunMutationTestAsync(
                        maxTestDuration,
                        sessionProgressTracker,
                        testHost,
                        testProject);
                    
                    foreach (var testResult in results)
                    {
                        // Store the timed out mutations such that they can be excluded.
                        timedOutMutations.AddRange(testResult.GetTimedOutTests());

                        // For each mutation add it to the report builder.
                        reportBuilder.AddTestResult(
                            testResult.TestResults,
                            testResult.Mutations,
                            singRunsStopwatch.Elapsed);
                    }

                    singRunsStopwatch.Stop();
                    singRunsStopwatch.Reset();
                    
                }
                catch (Exception e)
                {
                    sessionProgressTracker.Log(
                        $"The test process encountered an unexpected error. Continuing without this test run. Please consider to submit an github issue. {e}",
                        LogMessageType.Error);
                    failedRuns += 1;
                    Logger.Error(e, "The test process encountered an unexpected error.");
                }
                finally
                {
                    lock (this)
                    {
                        completedRuns += 1;

                        sessionProgressTracker.LogTestRunUpdate(completedRuns, totalRunsCount, failedRuns);
                    }

                    testProject.MarkAsFree();
                    testProject.DeleteTestProject();
                }
            }

            IEnumerable<Task>? tasks = from testRun in mutationTestRuns select RunTestRun(testRun);

            Task.WaitAll(tasks.ToArray());
            allRunsStopwatch.Stop();

            TestProjectReportModel? report = reportBuilder.Build(allRunsStopwatch.Elapsed, totalRunsCount);
            sessionProgressTracker.LogEndTestSession(allRunsStopwatch.Elapsed, completedRuns, mutationCount,
                report.ScorePercentage);

            testProjectDuplicator.DeleteFolder();

            return report;
        }
    }
}
