using System.Collections.Generic;

namespace Faultify.ProjectDuplicator.ProjectAnalyzing
{
    public interface IProjectInfo
    {
        string ProjectFilePath { get; }
        IEnumerable<string> ProjectReferences { get; }
        string AssemblyPath { get; }

        string ProjectName { get; }
        string TargetDirectory { get; }
        string TargetFileName { get; }
    }
}
