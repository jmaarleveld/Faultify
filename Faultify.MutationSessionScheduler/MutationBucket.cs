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
        public MutationBucket(IMutation initialMutation)
        {
            // TODO: How will coverage be handled in the new architecture?
            Tests = new HashSet<string>(initialMutation.TestCoverage);
            Mutations = new List<IMutation> { initialMutation };
        }

        /// <summary>
        ///     Set of tests that the contained mutations cover
        /// </summary>
        private HashSet<string> Tests { get; }

        /// <summary>
        ///     List of mutations in the bucket
        /// </summary>
        public List<IMutation> Mutations { get; }

        /// <summary>
        ///     Adds a new mutation to the bucket.
        /// </summary>
        /// <param name="mutation"></param>
        public void AddMutation(IMutation mutation)
        {
            // TODO: fix coverage 
            // TODO: is this a bug?
            Tests.Add(mutation.TestCoverage);
            Mutations.Add(mutation);
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