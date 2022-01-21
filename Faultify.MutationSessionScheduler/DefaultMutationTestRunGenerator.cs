using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Faultify.MutationCollector.Mutation;
using NLog;

namespace Faultify.MutationSessionScheduler
{
    public class DefaultMutationTestRunGenerator : IMutationTestRunGenerator
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // Magic number, optimal run size not yet clear
        private const int CONSTANT_OPTIMAL_DIVISION_ALGORITHM_THRESHOLD = 500;

        public IEnumerable<Dictionary<int, (IMutation, HashSet<string>)>> GenerateMutationTestRuns(
            IEnumerable<(IMutation, HashSet<string>, int)> testsPerMutation)
        {
            Logger.Info("Generating mutation test runs");

            var testsPerMutationList = testsPerMutation.ToList();

            IEnumerable<IList<(IMutation, int)>> testRunGroups
                = GetTestGroups(testsPerMutationList.ToList());

            var testsPerGroup = new Dictionary<int, HashSet<string>>();
            foreach (var (_, tests, groupId) in testsPerMutationList)
            {
                if (testsPerGroup.ContainsKey(groupId)) continue;
                testsPerGroup[groupId] = tests;
            }

            foreach (var testRunGroup in testRunGroups)
            {
                yield return testRunGroup.ToDictionary(
                    pair => pair.Item2, pair => (pair.Item1, testsPerGroup[pair.Item2]));
            }
        }

        /// <summary>
        ///     Groups mutations into groups that can be run in parallel
        /// </summary>
        /// <param name="mutationVariants"></param>
        /// <returns></returns>
        private static IEnumerable<IList<(IMutation, int)>> GetTestGroups(
            IList<(IMutation, HashSet<string>, int)> testsPerMutation
        )
        {
            Logger.Info("Building mutation groups for test groups");

            if (testsPerMutation.Count > CONSTANT_OPTIMAL_DIVISION_ALGORITHM_THRESHOLD)
            {
                // Faster but non-optimal
                return GreedyCoverageAlgorithm(testsPerMutation);
            }

            // Very poor time scaling
            return OptimalCoverageAlgorithm(testsPerMutation);
        }

        /*****************************************************************************************
         * Greedy Coverage Algorithm
         */

        /// <summary>
        ///     Greedy algorithm for the set cover problem of test coverage.
        ///     It iterates through the list of mutations, adding them to
        ///     the first bucket where it doesn't overlap with any tests.
        ///     If there are no valid buckets, a new one is created.
        /// </summary>
        /// <param name="mutationVariants">List of mutation variants to group</param>
        /// <returns>A collection of collections containing the non-overlapping sets.</returns>
        private static IEnumerable<IList<(IMutation, int)>> GreedyCoverageAlgorithm(
            IList<(IMutation, HashSet<string>, int)> testsPerMutation
        )
        {
            List<MutationBucket> buckets = new List<MutationBucket>();
            IOrderedEnumerable<(IMutation, HashSet<string>, int)> orderedTestsPerMutation =
                testsPerMutation.OrderByDescending(triple => triple.Item2.Count);

            foreach (var triple in orderedTestsPerMutation)
            {
                InsertOrMakeNew(buckets, triple);
            }

            return buckets.Select(bucket => bucket.Mutations);
        }

        /// <summary>
        ///     Adding a mutation to the buckets, if there is no bucket for it yet, make a new bucket
        /// </summary>
        private static void InsertOrMakeNew(
            List<MutationBucket> buckets,
            (IMutation, HashSet<string>, int) triple)
        {
            var mutation = triple.Item1;
            var testsForMutation = triple.Item2;
            var groupId = triple.Item3;

            // Attempt to add the mutation to a bucket
            var wasInserted = false;

            foreach (MutationBucket bucket in buckets)
            {
                if (bucket.IntersectsWith(testsForMutation))
                {
                    continue;
                }

                bucket.AddMutation(mutation, testsForMutation, groupId);
                wasInserted = true;
                break;
            }

            // If it fails, make a new bucket
            if (!wasInserted)
            {
                buckets.Add(new MutationBucket(mutation, testsForMutation, groupId));
            }
        }

        /*****************************************************************************************
         * Optimal Coverage Algorithm
         */

        /// <summary>
        ///     Optimal algorithm for the set cover problem of test coverage.
        ///     This is extremely slow for large problem sizes, but it is optimal
        ///     so it should be used for small test runs where it is not likely to slow things down.
        /// </summary>
        /// <returns>A collection of collections containing the non-overlapping sets.</returns>
        private static IEnumerable<IList<(IMutation, int)>> OptimalCoverageAlgorithm(
            IList<(IMutation, HashSet<string>, int)> testsPerMutation
        )
        {
            // Get all MutationsInfo
            List<(IMutation, HashSet<string>, int)> allMutations =
                new List<(IMutation, HashSet<string>, int)>(testsPerMutation);

            while (allMutations.Count > 0)
            {
                // Mark all tests as free slots
                HashSet<string> freeTests = new HashSet<string>(
                    allMutations.SelectMany(triple => triple.Item2));

                List<(IMutation, int)> mutationsForThisRun = new List<(IMutation, int)>();
                RemoveFreeSlots(allMutations, freeTests, mutationsForThisRun);

                yield return mutationsForThisRun;
            }
        }

        /// <summary>
        ///     Remove all the free test slots
        /// </summary>
        /// <param name="allMutations"></param>
        /// <param name="freeTests"></param>
        /// <param name="mutationsForThisRun"></param>
        private static void RemoveFreeSlots(
            List<(IMutation, HashSet<string>, int)> allMutations,
            HashSet<string> freeTests,
            List<(IMutation, int)> mutationsForThisRun)
        {
            foreach (var triple in allMutations.ToArray())
            {
                var mutation = triple.Item1;
                var testsForMutation = triple.Item2;
                var groupId = triple.Item3;

                if (freeTests.IsSupersetOf(testsForMutation))
                {
                    foreach (var test in testsForMutation)
                    {
                        freeTests.Remove(test);
                    }

                    mutationsForThisRun.Add((mutation, groupId));
                    allMutations.Remove(triple);
                }

                if (freeTests.Count == 0)
                {
                    break;
                }
            }
        }
    }
}