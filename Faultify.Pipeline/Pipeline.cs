using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Faultify.CoverageCollector;
using Faultify.ProjectBuilder;
using Faultify.ProjectDuplicator;
using Faultify.MutationSessionProgressTracker;
using Faultify.TestHostRunner;

namespace Faultify.Pipeline
{
    public class Pipeline
    {
        private readonly IMutationSessionProgressTracker _progressTracker;

        public Pipeline(IMutationSessionProgressTracker progressTracker)
        {
            _progressTracker = progressTracker;
        }

        /// <summary>
        ///     This should be called to start the pipeline.
        /// </summary>
        /// <param name="testProjectPath"></param>
        public async void Start(string testProjectPath)
        {
            // Create and duplicate the test project
            Tuple<ITestProjectDuplication, TestHost> testProject
                = await CreateTestProject(testProjectPath);

            // Obtain the coverage result (tests per mutation)
            CoverageResult coverageResult = new CoverageResult();
            Dictionary<Tuple<string, int>, HashSet<string>> testsPerMutation
                = await coverageResult.GetTestsPerMutation(_progressTracker, testProject.Item1
                    , testProject.Item2, CancellationToken.None);
        }

        /// <summary>
        ///     This can be used to create a test project, which can be used to perform the
        ///     coverage analysis on.
        /// </summary>
        /// <param name="testProjectPath"></param>
        /// <returns></returns>
        private async Task<Tuple<ITestProjectDuplication, TestHost>> CreateTestProject(
            string testProjectPath)
        {
            // Build project
            _progressTracker.LogBeginPreBuilding();
            IProjectInfo projectInfo = await BuildProject(testProjectPath);
            _progressTracker.LogEndPreBuilding();

            // Copy project N times
            ITestProjectDuplicator testProjectCopier
                = new TestProjectDuplicator(Directory.GetParent(projectInfo.AssemblyPath).FullName);

            // This is for some reason necessary when running tests with Dotnet,
            // otherwise the coverage analysis breaks future clones.
            testProjectCopier.MakeInitialCopy(projectInfo);

            // Begin code coverage on first project.
            ITestProjectDuplication coverageProject = testProjectCopier.MakeCopy(1);
            TestHost testHost = GetTestHost(projectInfo);
            return new Tuple<ITestProjectDuplication, TestHost>(coverageProject, testHost);
        }

        /// <summary>
        ///     Builds the project at the given project path.
        /// </summary>
        /// <param name="projectPath"></param>
        /// <returns></returns>
        private async Task<IProjectInfo> BuildProject(string projectPath)
        {
            IProjectReader projectReader = new ProjectReader();
            return await projectReader.ReadProjectAsync(projectPath, _progressTracker);
        }

        /// <summary>
        ///     This method can be used to obtain the TestHost of the program that will be analyzed.
        /// </summary>
        /// <param name="projectInfo"></param>
        /// <returns></returns>
        private static TestHost GetTestHost(IProjectInfo projectInfo)
        {
            string projectFile = File.ReadAllText(projectInfo.ProjectFilePath);

            if (Regex.Match(projectFile, "xunit").Captures.Any())
            {
                return TestHost.XUnit;
            }

            if (Regex.Match(projectFile, "nunit").Captures.Any())
            {
                return TestHost.NUnit;
            }

            if (Regex.Match(projectFile, "mstest").Captures.Any())
            {
                return TestHost.MsTest;
            }

            return TestHost.DotnetTest;
        }
    }
}