using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Faultify.TestHostRunner;

namespace Faultify.MutationSessionRunner
{
    /// <summary>
    ///     Defines an interface for a test run that executes mutations and returns the test results.
    /// </summary>
    public interface IMutationSessionRunner
    {
        /// <summary>
        ///     Runs the mutation test and returns the test run results.
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="mutationsPerGroup"></param>
        /// <param name="timedOutGroups"></param>
        /// <param name="testHost"></param>
        /// <param name="testProjectPath"></param>
        /// <returns></returns>
        Task<Tuple<HashSet<int>, Dictionary<string, TestOutcome>>> StartMutationSession(
            TimeSpan timeout,
            Dictionary<int, HashSet<string>> mutationsPerGroup,
            HashSet<int> timedOutGroups,
            TestHost testHost,
            string testProjectPath
        );
    }
}
