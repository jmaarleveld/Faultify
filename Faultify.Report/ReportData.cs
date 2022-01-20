using System;
using Faultify.MutationCollector.Mutation;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Faultify.Report
{
    /// <summary>
    ///     Structure to store test outcomes with its corresponding information regarding source
    ///     code and mutation.
    /// </summary>
    public struct ReportData
    {
        public string TestName { get; }
        public MutationStatus MutationStatus { get; }
        public IMutation Mutation { get; }
        public string OriginalSourceCode { get; }
        public string MutatedSourceCode { get; }
        public TimeSpan TestRunDuration { get; }

        public ReportData(
            string testName,
            TestOutcome testOutcome,
            IMutation mutation,
            string originalSourceCode,
            string mutatedSourceCode,
            TimeSpan testRunDuration)
        {
            TestName = testName;
            
            MutationStatus = testOutcome switch
            {
                TestOutcome.Failed => MutationStatus.Killed,
                TestOutcome.Passed => MutationStatus.Survived,
                _ => MutationStatus.Timeout,
            };
            
            Mutation = mutation;
            OriginalSourceCode = originalSourceCode;
            MutatedSourceCode = mutatedSourceCode;
            TestRunDuration = testRunDuration;
        }
    }
}
