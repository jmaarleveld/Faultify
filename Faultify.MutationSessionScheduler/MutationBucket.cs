using System.Collections.Generic;
using System.Linq;
using Faultify.MutationCollector.Mutation;


namespace Faultify.MutationSessionScheduler
{
    /// <summary>
    ///     Helper class used for the test coverage analysis.
    /// </summary>
    internal class MutationBucket
    {
        /// <summary>
        ///     Safely create a new bucket with an initial mutation in it
        /// </summary>
        /// <param name="initialMutation"></param>
        public MutationBucket(
            IMutation initialMutation, 
            HashSet<string> testForMutation,
            int initialMutationGroupId)
        {
            // TODO: How will coverage be handled in the new architecture?
            Tests = new HashSet<string>(testForMutation);
            Mutations = new List<(IMutation, int)> { (initialMutation, initialMutationGroupId) };
        }

        /// <summary>
        ///     Set of tests that the contained mutations cover
        /// </summary>
        private HashSet<string> Tests { get; }

        /// <summary>
        ///     List of mutations in the bucket
        /// </summary>
        public List<(IMutation, int)> Mutations { get; }

        /// <summary>
        ///     Adds a new mutation to the bucket.
        /// </summary>
        public void AddMutation(
            IMutation mutation, 
            HashSet<string> testsForMutation,
            int groupId)
        {
            Tests.UnionWith(testsForMutation);
            Mutations.Add((mutation, groupId));
        }

        /// <summary>
        ///     Returns wether or not the bucket tests intersect with the provided set of tests
        /// </summary>
        /// <param name="tests">Tests to check for intersection with</param>
        /// <returns>True if the provided set of tests overlaps with the set of tests in the bucket</returns>
        public bool IntersectsWith(HashSet<string> tests)
        {
            return !tests.AsParallel().Any(test => Tests.Contains(test));
        }
    }
}