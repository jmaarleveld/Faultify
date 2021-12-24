using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Faultify.TestHostRunner.Results
{
    public interface ITestResult
    {
        string Name { get; set; }
        TestOutcome Outcome { get; set; }
    }
}
