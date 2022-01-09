using Faultify.ProjectDuplicator.ProjectAnalyzing;

namespace Faultify.ProjectDuplicator
{
    public interface ITestProjectDuplicator
    {
        TestProjectDuplication MakeInitialCopy(IProjectInfo testProject);
        TestProjectDuplication MakeCopy(int i);
        void DeleteFolder();
    }
}