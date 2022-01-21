using System;
using System.Collections.Generic;
using System.Linq;
using Faultify.MutationCollector.Mutation;
using NLog;

namespace Faultify.MutationSessionScheduler
{
    /// <summary>
    ///     Utilities for mapping code coverage information
    ///     to mutations.
    /// </summary>
    public class CoverageMapper
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
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
                
                foreach (var mutation in mutationGroup)
                {
                    if (methods == null) {
                        try
                        {
                            if (mutation.ParentMethodEntityHandle == null)
                            {
                                throw new NullReferenceException(
                                    "ParentMethodEntityHandle of the mutation was null");
                            }
                            KeyValuePair<Tuple<string, int>, HashSet<string>> pair
                                = testsPerMethod.FirstOrDefault(pair =>
                                    pair.Key.Item1 == mutation.AssemblyName && pair.Key
                                        .Item2 == mutation.ParentMethodEntityHandle);
                            methods = pair.Value;
                        }
                        catch (NullReferenceException e)
                        {
                            Logger.Error(e.Message);
                        }
                    }

                    if (methods != null)
                    {
                        yield return (mutation, methods, currentMutationGroupId);
                    }
                }
                
                currentMutationGroupId++;
            }
        }
    }
}