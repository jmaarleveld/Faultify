using System.Collections.Generic;
using System;

namespace Faultify.ProjectDuplicator
{
    public interface ITestProjectDuplication: IDisposable
    {
        IEnumerable<FileDuplication> TestProjectReferences { get; set; }
        FileDuplication TestProjectFile { get; set; }
        int DuplicationNumber { get; }
        bool IsInUse { get; set; }
        event EventHandler<TestProjectDuplication>? TestProjectFreed;

        void MarkAsFree();
        void DeleteTestProject();
    }
}