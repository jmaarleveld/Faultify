using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Faultify.AssemblyDissection;
using Faultify.MutationCollector;
using Faultify.MutationCollector.Mutation;
using coverageCollector = Faultify.CoverageCollector.CoverageCollector;
using Faultify.ProjectBuilder;
using Faultify.ProjectDuplicator;
using Faultify.MutationSessionProgressTracker;
using Faultify.MutationSessionScheduler;

namespace Faultify.Pipeline
{
    public class Pipeline
    {
        private readonly IMutationSessionProgressTracker _progressTracker;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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
            // Build the project
            IProjectInfo projectInfo = await BuildProject(testProjectPath);

            // Duplicate the test project
            var (testProjectDuplication, dependencyAssemblies) = DuplicateTestProject(projectInfo);

            // Obtain mapping of a method to the tests that cover that method
            _progressTracker.LogBeginCoverage();
            (Dictionary<Tuple<string, int>, HashSet<string>> testsPerMethod, TimeSpan timeout)
                = await coverageCollector.GetTestsPerMutation(testProjectDuplication
                    , dependencyAssemblies, projectInfo, TIMESPAN_PROGRAMSETTING_HERE!
                    , CancellationToken.None);

            // Collect mutations for each assembly
            var mutations = CollectMutations(dependencyAssemblies, MUTATIONLEVEL, EXCLUDEGROUP
                , EXCLUDESINGUAR);

            // Map the testsPerMethod with the mutations
            var coveragePerMutation
                = MutationSessionScheduler.CoverageMapper.MapCoverageToMutations(testsPerMethod
                    , mutations);

            // Generate the mutation test runs based on the mapped coverage and mutations
            IMutationTestRunGenerator mutationTestRunGenerator
                = new DefaultMutationTestRunGenerator();
            var mutationTestRuns
                = mutationTestRunGenerator.GenerateMutationTestRuns(coveragePerMutation);
        }

        private static IEnumerable<IEnumerable<IMutation>> CollectMutations(
            List<AssemblyAnalyzer> dependencyAssemblies,
            MutationLevel mutationLevel,
            HashSet<string> excludeGroup,
            HashSet<string> excludeSingular)
        {
            var allMutations = new List<IEnumerable<IEnumerable<IMutation>>>();
            
            foreach (var assembly in dependencyAssemblies)
            foreach (var type in assembly.Types)
            {
                var mutations
                    = type.Value.AllMutations(mutationLevel, excludeGroup, excludeSingular);
                allMutations.Add(mutations);
            }
            // Flatten result
            return allMutations.SelectMany(x => x);
        }

        /// <summary>
        ///     This can be used to create a test project, which can be used to perform the
        ///     coverage analysis on.
        /// </summary>
        /// <param name="projectInfo"></param>
        /// <returns></returns>
        private static Tuple<ITestProjectDuplication, List<AssemblyAnalyzer>> DuplicateTestProject(
            IProjectInfo projectInfo)
        {
            // Copy project N times
            ITestProjectDuplicator testProjectCopier
                = new TestProjectDuplicator(Directory.GetParent(projectInfo.AssemblyPath).FullName);

            // This is for some reason necessary when running tests with Dotnet,
            // otherwise the coverage analysis breaks future clones.
            testProjectCopier.MakeInitialCopy(projectInfo);

            // Begin code coverage on first project.
            ITestProjectDuplication testProjectDuplication = testProjectCopier.MakeCopy(1);

            // Load each project in memory as an AssemblyAnalyzer
            List<AssemblyAnalyzer> dependencyAssemblies = LoadInMemory(testProjectDuplication);

            return new Tuple<ITestProjectDuplication, List<AssemblyAnalyzer>>(testProjectDuplication
                , dependencyAssemblies);
        }

        /// <summary>
        ///     Builds the project at the given project path.
        /// </summary>
        /// <param name="projectPath"></param>
        /// <returns></returns>
        private async Task<IProjectInfo> BuildProject(string projectPath)
        {
            _progressTracker.LogBeginPreBuilding();
            IProjectReader projectReader = new ProjectReader();
            IProjectInfo projectInfo
                = await projectReader.ReadAndBuildProjectAsync(projectPath, _progressTracker);
            _progressTracker.LogEndPreBuilding();
            return projectInfo;
        }

        /// <summary>
        ///     Foreach project reference load it in memory as an <see cref="AssemblyAnalyzer"/>.
        /// </summary>
        /// <param name="duplication"></param>
        private static List<AssemblyAnalyzer> LoadInMemory(ITestProjectDuplication duplication)
        {
            List<AssemblyAnalyzer> dependencyAssemblies = new List<AssemblyAnalyzer>();

            foreach (FileDuplication projectReferencePath in duplication.TestProjectReferences)
            {
                try
                {
                    AssemblyAnalyzer loadProjectReferenceModel
                        = new AssemblyAnalyzer(projectReferencePath.FullFilePath());

                    if (loadProjectReferenceModel.Types.Count > 0)
                    {
                        dependencyAssemblies.Add(loadProjectReferenceModel);
                    }
                }
                catch (FileNotFoundException e)
                {
                    Logger.Error(e
                        , $"Faultify was unable to read the file {projectReferencePath.FullFilePath()}.");
                }
            }

            return dependencyAssemblies;
        }
    }
}