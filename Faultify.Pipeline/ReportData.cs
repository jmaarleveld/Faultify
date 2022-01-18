using Faultify.AssemblyDissection;
using Faultify.MutationCollector.Mutation;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Faultify.Pipeline
{
    /// <summary>
    ///     Structure to store test outcomes with its corresponding information regarding source
    ///     code and mutation.
    /// </summary>
    public struct ReportData
    {
        public string TestName { get; }
        public TestOutcome TestOutcome { get; }
        public IMutation Mutation { get; }
        public string OriginalSourceCode { get; }
        public string MutatedSourceCode { get; }

        public ReportData(string testName, TestOutcome testOutcome, IMutation mutation,
            string originalSourceCode, string mutatedSourceCode)
        {
            TestName = testName;
            TestOutcome = testOutcome;
            Mutation = mutation;
            OriginalSourceCode = originalSourceCode;
            MutatedSourceCode = mutatedSourceCode;
        }
    }
}
