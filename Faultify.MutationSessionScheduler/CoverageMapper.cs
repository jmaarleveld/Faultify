using System;
using System.Collections.Generic;
using System.Linq;
using Faultify.MutationCollector.Mutation;

namespace Faultify.MutationSessionScheduler
{
    /// <summary>
    ///     Utilities for mapping code coverage information
    ///     to mutations.
    /// </summary>
    public class CoverageMapper
    {
        /// <summary>
        ///     Map test coverage to mutations.
        ///
        ///     This method takes in coverage information, mapping
        ///     tests to methods, and a list of mutations.
        ///
        ///     The method then returns triples of
        ///     the form (mutation, methods, group_id),
        ///     containing the information what mutations
        ///     are affect what tests, and what group said
        ///     mutations are from.
        /// </summary>
        /// <param name="testsPerMethod"></param>
        /// <param name="mutations"></param>
        /// <returns></returns>
        public static IEnumerable<(IMutation, HashSet<string>, int)> MapCoverageToMutations(
            Dictionary<Tuple<string, int>, HashSet<string>> testsPerMethod,
            IEnumerable<IEnumerable<IMutation>> mutations)
        {
            int currentMutationGroupId = 1;
            
            foreach (var mutationGroup in mutations) {
                HashSet<string>? methods = null;
                
                foreach (var mutation in mutationGroup) {
                    if (mutation.MemberEntityHandle == null) {
                        // TODO: log a warning?
                        // TODO: probably should be handled somehow. 
                        // TODO: However, this case was never handled (mutations ignored)
                        continue;
                    }
                    if (methods == null) {
                        KeyValuePair<Tuple<string, int>, HashSet<string>> pair = testsPerMethod.FirstOrDefault(pair => 
                            pair.Key.Item1 == mutation.AssemblyName && pair.Key
                            .Item2 == mutation.MemberEntityHandle);
                        methods = pair.Value;
                    }

                    yield return (mutation, methods, currentMutationGroupId);
                }
                
                currentMutationGroupId++;
            }
        }
    }
}