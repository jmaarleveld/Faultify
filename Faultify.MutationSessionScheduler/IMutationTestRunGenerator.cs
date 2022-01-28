using System.Collections.Generic;
using Faultify.MutationCollector.Mutation;

namespace Faultify.MutationSessionScheduler
{
    /// <summary>
    ///     Interface for defining a mutation test run generator that
    ///     compute the grouping for the mutation test runs.
    /// </summary>
    public interface IMutationTestRunGenerator
    {
        /// <summary>
        ///     Generates mutation test runs for the mutation test session.
        /// </summary>
        public IEnumerable<Dictionary<int, (IMutation, HashSet<string>)>> GenerateMutationTestRuns(
            IEnumerable<(IMutation, HashSet<string>, int)> testsPerMutation);
    }
}
