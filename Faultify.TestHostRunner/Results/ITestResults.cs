using System.Collections.Generic;

namespace Faultify.TestHostRunner.Results
{
    public interface ITestResults
    {
        List<TestResult> Tests { get; set; }
        byte[] Serialize();
    }
}