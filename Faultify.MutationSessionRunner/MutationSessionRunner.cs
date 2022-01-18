using System;
using System.Collections.Generic;
using System.Linq;
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
    public class MutationSessionRunner : IMutationSessionRunner
    {
        public async Task<Tuple<HashSet<int>, Dictionary<string, TestOutcome>>> StartMutationSession(
            TimeSpan timeout,
            IProgress<string> sessionProgressTracker,
            Dictionary<int, HashSet<string>> testsPerGroup,
            HashSet<int> timedOutGroups,
            TestHost testHost,
            ITestProjectDuplication testProject
        )
        {
            HashSet<string> runningTests = new HashSet<string>();
            foreach (var (groupId, tests) in testsPerGroup)
            {
                if (timedOutGroups.Contains(groupId)) continue;
                foreach (var test in tests)
                {
                    runningTests.Add(test);
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
                foreach (var (groupId, tests) in testsPerGroup)
                {
                    if (tests.Contains(test))
                    {
                        newTimedOutGroups.Add(groupId);
                    }
                }
            }

            // Convert the testOutcomes to a Dictionary<string, TestOutcome>
            var testOutcomesDict = testOutcomes.ToDictionary(x => x.Item1, x => x.Item2);
            
            return new Tuple<HashSet<int>, Dictionary<string, TestOutcome>>(newTimedOutGroups
                , testOutcomesDict);
        }
    }
}
