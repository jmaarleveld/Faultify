using System.Collections.Generic;

namespace Faultify.TestHostRunner.Results
{
    public interface IMutationCoverage
    {
        Dictionary<string, List<RegisteredCoverage>> Coverage { get; set; }

        byte[] Serialize();
        
    }
}