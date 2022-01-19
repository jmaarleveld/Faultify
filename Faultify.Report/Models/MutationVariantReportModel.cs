using System;
using System.Text.RegularExpressions;

namespace Faultify.Report.Models
{
    public class MutationVariantReportModel
    {
        public MutationVariantReportModel(
            string name,
            string description,
            MutationAnalyzerReportModel mutationAnalyzer,
            MutationStatus testStatus,
            TimeSpan testDuration,
            string originalSource,
            string mutatedSource,
            string memberName
        )
        {
            Name = name;
            Description = description;
            MutationAnalyzer = mutationAnalyzer;
            TestStatus = testStatus;
            TestDuration = testDuration;
            OriginalSource = originalSource;
            MutatedSource = mutatedSource;
            MemberName = memberName;
        }

        private string _name;
        public string Name {
            get {
                return Regex.Replace(_name, @"\S+ (\S+)\(\S*\) (.+)", "$1 $2");
            }
            set {
                _name = value;
            }
        }
        public string Description { get; set; }
        public MutationAnalyzerReportModel MutationAnalyzer { get; set; }
        public MutationStatus TestStatus { get; set; }
        public TimeSpan TestDuration { get; set; }
        public string OriginalSource { get; set; }
        public string MutatedSource { get; set; }
        public string MemberName { get; set; }
    }
}
