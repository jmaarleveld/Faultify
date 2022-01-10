using NLog;

namespace Faultify.ProjectDuplicator
{
    /// <summary>
    ///     A test project duplication.
    /// </summary>
    public class TestProjectDuplication : IDisposable, ITestProjectDuplication
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public TestProjectDuplication(
            FileDuplication testProjectFile,
            IEnumerable<FileDuplication> testProjectReferences,
            int duplicationNumber
        )
        {
            TestProjectFile = testProjectFile;
            TestProjectReferences = testProjectReferences;
            DuplicationNumber = duplicationNumber;
        }

        /// <summary>
        ///     Test project references.
        /// </summary>
        public IEnumerable<FileDuplication> TestProjectReferences { get; set; }

        /// <summary>
        ///     Test project file handle.
        /// </summary>
        public FileDuplication TestProjectFile { get; set; }

        /// <summary>
        ///     Number indicating which duplication this test project is.
        /// </summary>
        public int DuplicationNumber { get; }

        /// <summary>
        ///     Indicates if the test project is currently used by any test runner.
        /// </summary>
        public bool IsInUse { get; set; }

        public void Dispose()
        {
            TestProjectFile.Dispose();
            foreach (var fileDuplication in TestProjectReferences) fileDuplication.Dispose();
        }

        /// <summary>
        ///     Event that notifies when ever this test project is given free by a given test runner.
        /// </summary>
        public event EventHandler<TestProjectDuplication>? TestProjectFreed;

        /// <summary>
        ///     Mark this project as free for any test runner.
        /// </summary>
        public void MarkAsFree()
        {
            IsInUse = false;
            TestProjectFreed?.Invoke(this, this);
        }


        /// <summary>
        ///     Delete the test project completely
        ///     Currently does not work, given that Nunit restricts access to the files awaits been given
        /// </summary>
        public void DeleteTestProject()
        {
            try {
                Directory.Delete(TestProjectFile.Directory, true);
            }
            catch (Exception e) {
                Logger.Error(e, $"Couldn't delete {TestProjectFile.Directory}." + e.Message);
            }
        }
    }
}
