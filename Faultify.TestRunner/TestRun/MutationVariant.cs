using System.Reflection.Metadata;
using Faultify.MutationCollector.AssemblyMutator;
using Faultify.MutationCollector.Mutation;

namespace Faultify.TestRunner.TestRun
{
    /// <summary>
    ///     Single mutation variant which contains the executable mutation.
    /// </summary>
    public class MutationVariant
    {
        public MutationVariant(
            bool causesTimeOut,
            AssemblyMutator assembly,
            MutationVariantIdentifier mutationIdentifier,
            MutationAnalyzerInfo mutationAnalyzerInfo,
            EntityHandle memberHandle,
            IMutation mutation,
            string mutatedSource,
            string originalSource
        )
        {
            CausesTimeOut = causesTimeOut;
            Assembly = assembly;
            MutationIdentifier = mutationIdentifier;
            MutationAnalyzerInfo = mutationAnalyzerInfo;
            MemberHandle = memberHandle;
            Mutation = mutation;
            MutatedSource = mutatedSource;
            OriginalSource = originalSource;
        }
        
        public bool CausesTimeOut { get; set; } = false;

        public AssemblyMutator Assembly { get; set; }
        public MutationVariantIdentifier MutationIdentifier { get; set; }
        public MutationAnalyzerInfo MutationAnalyzerInfo { get; set; }

        public EntityHandle MemberHandle { get; set; }
        public IMutation Mutation { get; set; }
        public string MutatedSource { get; set; }
        public string OriginalSource { get; set; }
    }

    public struct MutationAnalyzerInfo
    {
        public string AnalyzerName { get; set; }
        public string AnalyzerDescription { get; set; }
    }
}
