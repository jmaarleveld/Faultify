using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faultify.TestRunner.Logging;
using Faultify.TestRunner.ProjectDuplication;

namespace Faultify.MutationSessionScheduler
{
    /// <summary>
    ///     Defines an interface for a test run group.
    /// </summary>
    public interface IMutationTestRunGroup
    {
        public int RunId { get; set; }

        public int MutationCount { get; }

        /// <summary>
        ///     Runs the mutation test and returns the test run results.
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="sessionProgressTracker"></param>
        /// <param name="testHostRunnerFactory"></param>
        /// <param name="projectDuplication"></param>
        /// <returns></returns>
        Task<IEnumerable<TestRunResult>> RunMutationTestAsync(
            TimeSpan timeout,
            MutationSessionProgressTracker sessionProgressTracker,
            TestHost testHost,
            TestProjectDuplication projectDuplication
        );


        /// <summary>
        ///     Initializes the mutation and filter out those who have had a time out.
        /// </summary>
        /// <param name="testProject"></param>
        /// <param name="timedOutMutationVariants"></param>
        /// <param name="excludeGroup"></param>
        /// <param name="excludeSingular"></param>
        void InitializeMutations(
            TestProjectDuplication testProject,
            IEnumerable<MutationVariantIdentifier> timedOutMutationVariants,
            HashSet<string> excludeGroup,
            HashSet<string> excludeSingular
        );
    }
}
