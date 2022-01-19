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
        public string AnalyzerName { get; }
        public string AnalyzerDescription { get; }
        public string OriginalSourceCode { get; }
        public string MutatedSourceCode { get; }

        public ReportData(string testName, TestOutcome testOutcome, string analyzerName,
            string analyzerDescription, string originalSourceCode, string mutatedSourceCode)
        {
            TestName = testName;
            TestOutcome = testOutcome;
            AnalyzerName = analyzerName;
            AnalyzerDescription = analyzerDescription;
            OriginalSourceCode = originalSourceCode;
            MutatedSourceCode = mutatedSourceCode;
        }
    }
}
