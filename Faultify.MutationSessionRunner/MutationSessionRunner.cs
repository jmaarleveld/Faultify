using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Faultify.ProjectDuplicator;
using Faultify.TestHostRunner;
using Faultify.TestHostRunner.TestHostRunners;

namespace Faultify.MutationSessionRunner
{
    /// <summary>
    ///     Executes the mutation test run on a test project.
    /// </summary>
    internal class MutationSessionRunner : IMutationSessionRunner
    {
        public async Task<Tuple<HashSet<int>, Dictionary<string, TestOutcome>>> StartMutationSession(
            TimeSpan timeout,
            IProgress<string> sessionProgressTracker,
            Dictionary<int, HashSet<string>> mutationsPerGroup,
            HashSet<int> timedOutGroups,
            TestHost testHost,
            TestProjectDuplication testProject
        )
        {
            HashSet<string> runningTests = new HashSet<string>();
            foreach (var (groupId, mutations) in mutationsPerGroup)
            {
                if (timedOutGroups.Contains(groupId)) continue;
                foreach (var mutation in mutations)
                {
                    runningTests.Add(mutation);
                }
            }

            ITestHostRunner testRunner = TestHostRunnerFactory.CreateTestRunner(
                testProject.TestProjectFile.FullFilePath(), timeout, testHost);

            List<Tuple<string, TestOutcome>> testOutcomes =
                await testRunner.RunTests(timeout, sessionProgressTracker, runningTests);

            // Determine the timed out groups during this TestRun
            HashSet<int> newTimedOutGroups = new HashSet<int>();
            foreach ((string test, TestOutcome testOutcome) in testOutcomes)
            {
                if (testOutcome != TestOutcome.None) continue;
                foreach (var (groupId, mutations) in mutationsPerGroup)
                {
                    if (mutations.Contains(test))
                    {
                        newTimedOutGroups.Add(groupId);
                    }
                }
            }

            // Convert the testOutcomes to a Dictionary<string, TestOutcome>
            Dictionary<string, TestOutcome>
                testOutcomesDict = new Dictionary<string, TestOutcome>();
            foreach ((string mutation, TestOutcome testOutcome) in testOutcomes)
            {
                testOutcomesDict[mutation] = testOutcome;
            }

            return new Tuple<HashSet<int>, Dictionary<string, TestOutcome>>(newTimedOutGroups
                , testOutcomesDict);
        }
    }
}
