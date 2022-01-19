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
        ///     Every enumerable of mutations contained in the main enumerable
        ///     applies to the same opcode and was found by the same
        ///     analyzer.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IEnumerable<IMutation>> AllMutations(
            MutationLevel mutationLevel, 
            HashSet<string> excludeGroup, 
            HashSet<string> excludeSingular);

        /// <summary>
        ///     Get a mutation equivalent to the given one.
        ///
        ///     This can be used when mutating a copy of an
        ///     existing project. The underlying Cecil
        ///     definitions of a mutation still map to the
        ///     original project; this method can be used
        ///     to obtain a new mutation with updated
        ///     fields which can be used on the copy.
        /// </summary>
        IMutation GetEquivalentMutation(IMutation original);
    }
}
