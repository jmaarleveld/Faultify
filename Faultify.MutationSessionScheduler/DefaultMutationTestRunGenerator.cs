using System.Collections.Generic;
using System.Linq;
using Faultify.MutationCollector;
using Faultify.TestRunner.Shared;
using NLog;

namespace Faultify.MutationSessionScheduler
{
    public class DefaultMutationTestRunGenerator : IMutationTestRunGenerator
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public IEnumerable<IMutationTestRun> GenerateMutationTestRuns(
            Dictionary<RegisteredCoverage, HashSet<string>> testsPerMethod,
            TestProjectInfo testProjectInfo,
            MutationLevel mutationLevel,
            HashSet<string> excludeGroup,
            HashSet<string> excludeSingular
        )
        {
            Logger.Info("Generating mutation test runs");

            IList<MutationVariantIdentifier>? allMutations =
                GetMutationsForCoverage(testsPerMethod, testProjectInfo, excludeGroup, excludeSingular, mutationLevel);
            IEnumerable<IList<MutationVariantIdentifier>>? mutationGroups = GetTestGroups(allMutations);

            var i = 0;
            return mutationGroups.Select(mutationGroup =>
            {
                return new DefaultMutationTestRun
                {
                    MutationIdentifiers = mutationGroup,
                    RunId = i++,
                    MutationLevel = mutationLevel,
                };
            });
        }

        /// <summary>
        ///     Greedy algorithm for the set cover problem of test coverage. It iterates through the list of mutations, adding them
        ///     to
        ///     the first bucket where it doesn't overlap with any tests. If there are no valid buckets, a new one is created.
        /// </summary>
        /// <param name="mutationVariants">List of mutation variants to group</param>
        /// <returns>A collection of collections containing the non-overlapping sets.</returns>
        private static IEnumerable<IList<MutationVariantIdentifier>> GreedyCoverageAlgorithm(
            IList<MutationVariantIdentifier> mutationVariants
        )
        {
            List<MutationBucket>? buckets = new List<MutationBucket>();
            IOrderedEnumerable<MutationVariantIdentifier>? mutations =
                mutationVariants.OrderByDescending(mutation => mutation.TestCoverage.Count);

            foreach (MutationVariantIdentifier mutation in mutations)
            {
                InsertOrMakeNew(buckets, mutation);
            }

            return buckets.Select(bucket => bucket.Mutations);
        }

        /// <summary>
        ///     Adding a mutation to the buckets, if there is no bucket for it yet, make a new bucket
        /// </summary>
        /// <param name="buckets"></param>
        /// <param name="mutation"></param>
        private static void InsertOrMakeNew(List<MutationBucket> buckets, MutationVariantIdentifier mutation)
        {
            // Attempt to add the mutation to a bucket
            var wasInserted = false;
            foreach (MutationBucket bucket in buckets)
            {
                if (!bucket.IntersectsWith(mutation.TestCoverage))
                {
                    bucket.AddMutation(mutation);
                    wasInserted = true;
                    break;
                }
            }

            // If it fails, make a new bucket
            if (!wasInserted)
            {
                buckets.Add(new MutationBucket(mutation));
            }
        }


        /// <summary>
        ///     Optimal algorithm for the set cover problem of test coverage. This is extgremely slow for large problem sizes, but
        ///     it is optimal
        ///     so it should be used for small test runs where it is not likely to slow things down.
        /// </summary>
        /// <param name="mutationVariants">List of mutation variants to group</param>
        /// <returns>A collection of collections containing the non-overlapping sets.</returns>
        private static IEnumerable<IList<MutationVariantIdentifier>> OptimalCoverageAlgorithm(
            IList<MutationVariantIdentifier> mutationVariants
        )
        {
            // Get all MutationsInfo
            List<MutationVariantIdentifier>? allMutations = new List<MutationVariantIdentifier>(mutationVariants);

            while (allMutations.Count > 0)
            {
                // Mark all tests as free slots
                HashSet<string>? freeTests = new HashSet<string>(mutationVariants.SelectMany(x => x.TestCoverage));

                List<MutationVariantIdentifier>? mutationsForThisRun = new List<MutationVariantIdentifier>();
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
        private static void RemoveFreeSlots(List<MutationVariantIdentifier> allMutations, HashSet<string> freeTests, List<MutationVariantIdentifier> mutationsForThisRun)
        {
            foreach (MutationVariantIdentifier mutation in allMutations.ToArray())
            {
                if (freeTests.IsSupersetOf(mutation.TestCoverage))
                {
                    foreach (string test in mutation.TestCoverage) freeTests.Remove(test);

                    mutationsForThisRun.Add(mutation);
                    allMutations.Remove(mutation);
                }

                if (freeTests.Count == 0) break;
            }
        }

        /// <summary>
        ///     Groups mutations into groups that can be run in parallel
        /// </summary>
        /// <param name="mutationVariants"></param>
        /// <returns></returns>
        private static IEnumerable<IList<MutationVariantIdentifier>> GetTestGroups(
            IList<MutationVariantIdentifier> mutationVariants
        )
        {
            Logger.Info("Building mutation groups for test groups");

            if (mutationVariants.Count > 500) // Magic number, optimal run size not yet clear
            {
                // Faster but non-optimal
                return GreedyCoverageAlgorithm(mutationVariants);
            }

            // Very poor time scaling
            return OptimalCoverageAlgorithm(mutationVariants);
        }

        private IList<MutationVariantIdentifier> GetMutationsForCoverage(
            Dictionary<RegisteredCoverage, HashSet<string>> coverage,
            TestProjectInfo testProjectInfo,
            HashSet<string> excludeGroup,
            HashSet<string> excludeSingular,
            MutationLevel mutationLevel
        )
        {
            Logger.Info("Generating dummy mutations for test coverage");
            List<MutationVariantIdentifier>? allMutations = new List<MutationVariantIdentifier>();

            foreach (var assembly in testProjectInfo.DependencyAssemblies)
            foreach (var type in assembly.Types)
            foreach (var method in type.Methods)
            {
                GetAllMutations(coverage, excludeGroup, excludeSingular, mutationLevel, allMutations, assembly, method);
            }
            return allMutations;
        }

        /// <summary>
        ///     Getting all the mutations, group by group
        /// </summary>
        /// <param name="coverage"></param>
        /// <param name="excludeGroup"></param>
        /// <param name="excludeSingular"></param>
        /// <param name="mutationLevel"></param>
        /// <param name="allMutations"></param>
        /// <param name="assembly"></param>
        /// <param name="method"></param>
        private void GetAllMutations(Dictionary<RegisteredCoverage, HashSet<string>> coverage, HashSet<string> excludeGroup, HashSet<string> excludeSingular, MutationLevel mutationLevel, List<MutationVariantIdentifier> allMutations, MutationCollector.AssemblyMutator.AssemblyMutator assembly, MutationCollector.AssemblyMutator.MethodScope method)
        {
            var methodMutationId = 0;
            KeyValuePair<RegisteredCoverage, HashSet<string>> registeredMutation = coverage.FirstOrDefault(x =>
                x.Key.AssemblyName == assembly.Module.Assembly.Name.Name && x.Key.EntityHandle == method.IntHandle);
            var mutationGroupId = 0;

            if (registeredMutation.Key != null)
            {
                foreach (var group in method.AllMutations(mutationLevel, excludeGroup, excludeSingular))
                {
                    foreach (var mutation in group)
                    {
                        allMutations.Add(new MutationVariantIdentifier(registeredMutation.Value,
                            method.AssemblyQualifiedName,
                            methodMutationId, mutationGroupId));

                        methodMutationId++;
                    }

                    mutationGroupId += 1;
                }
            }
        }

        /// <summary>
        ///     Helper class used for the test coverage analysis.
        /// </summary>
        private class MutationBucket
        {
            /// <summary>
            ///     Safely create a new bucket with an initial mutation in it
            /// </summary>
            /// <param name="initialMutation"></param>
            public MutationBucket(MutationVariantIdentifier initialMutation)
            {
                Tests = new HashSet<string>(initialMutation.TestCoverage);
                Mutations = new List<MutationVariantIdentifier> { initialMutation };
            }

            /// <summary>
            ///     Set of tests that the contained mutations cover
            /// </summary>
            private HashSet<string> Tests { get; }

            /// <summary>
            ///     List of mutations in the bucket
            /// </summary>
            public List<MutationVariantIdentifier> Mutations { get; }

            /// <summary>
            ///     Adds a new mutation to the bucket.
            /// </summary>
            /// <param name="mutation"></param>
            public void AddMutation(MutationVariantIdentifier mutation)
            {
                Tests.Union(mutation.TestCoverage);
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
}
