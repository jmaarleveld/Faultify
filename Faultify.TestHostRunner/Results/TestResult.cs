using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Faultify.TestHostRunner.Results
{
    public class TestResult : ITestResult
    {
        public string Name { get; set; }
        public TestOutcome Outcome { get; set; }
    }
}
