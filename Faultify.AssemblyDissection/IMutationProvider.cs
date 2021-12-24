using System.Collections.Generic;
using Faultify.MutationCollector;
using Faultify.MutationCollector.Mutation;

namespace Faultify.AssemblyDissection
{
    /// <summary>
    ///     Defines an interface for mutation providers.
    /// </summary>
    internal interface IMutationProvider
    {
        /// <summary>
        ///     Returns all possible mutations.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IMutation> AllMutations(
            MutationLevel mutationLevel, 
            HashSet<string> excludeGroup, 
            HashSet<string> excludeSingular);
    }
}
