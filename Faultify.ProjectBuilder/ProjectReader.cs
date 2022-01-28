using System;
using System.Linq;
using System.Threading.Tasks;
using Buildalyzer;
using Buildalyzer.Environment;
using NLog;

namespace Faultify.ProjectBuilder
{
    public class ProjectReader : IProjectReader
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public Task<IProjectInfo> ReadAndBuildProjectAsync(string path)
        {
            return Task.Run(() =>  AnalyzeProject(path));
        }

        /// <summary>
        ///     Analyze the project and return the results
        /// </summary>
        /// <param name="path"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        private static IProjectInfo AnalyzeProject(string path)
        {
            AnalyzerManager analyzerManager = new AnalyzerManager();
            IProjectAnalyzer projectAnalyzer = analyzerManager.GetProject(path);

            ProjectInfo result = null;
            
            try
            {
                IAnalyzerResults analyzerResults = projectAnalyzer.Build(new EnvironmentOptions
                {
                    DesignTime = false,
                    Restore = true,
                });

                if (!analyzerResults.Any(x => x.Succeeded))
                {
                    throw new ProjectNotBuiltException();
                }

                result = new ProjectInfo(analyzerResults.First(r => r.Succeeded));
            }
            catch (ProjectNotBuiltException e)
            {
                Logger.Fatal(e, "Faultify was unable to build any targets for the provided project. Terminating program");
                Environment.Exit(-1);
            }
            catch (InvalidOperationException e)
            {
                Logger.Fatal(e, "Could not find any target frameworks to build the project for. Terminating program");
                Environment.Exit(-1);
            }

            return result;
        }
    }
}
