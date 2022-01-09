using System.Collections.Generic;
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
        /// </summary>
        /// <param name="testPerMethod"></param>
        /// <param name="mutations"></param>
        /// <returns></returns>
        static Dictionary<CoverageResult, IEnumerable<IList<IMutation>>> MapCoverageToMutations(
            Dictionary<CoverageResult, HashSet<string>> testPerMethod,
            IEnumerable<IEnumerable<IMutation>> mutations)
        {
            
        }
    }
}