using System;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using Faultify.MutationSessionProgressTracker;
using Faultify.Pipeline;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using MLog = Microsoft.Extensions.Logging.ILogger;

namespace Faultify.Cli
{
    /// <summary>
    /// The main instance of Faultify
    /// </summary>
    internal class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static readonly MLog ConsoleLogger
            = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<Program>();
        
        /// <summary>
        /// Settings of the current program run
        /// </summary>
        private Settings ProgramSettings { get; }

        /// <summary>
        /// Build a new instance of Faultify with the provided settings
        /// </summary>
        /// <param name="programSettings"></param>
        private Program(Settings programSettings)
        {
            ProgramSettings = programSettings;
        }

        /// <summary>
        /// Program entrypoint
        /// </summary>
        /// <param name="args">Commandline arguments</param>
        /// <exception cref="Exception">Faultify service cannot be accessed</exception>
        private static async Task Main(string[] args)
        {
            Program? program = CreateProgram(args);

            if (program == null)
            {
                Logger.Fatal("Couldn't get the Faultify service from the system");
                Environment.Exit(-1);
            }

            Directory.CreateDirectory(program.ProgramSettings.OutputDirectory);
            await program.Run();
        }

        /// <summary>
        ///     Perform the setup to create the Faultify program
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static Program? CreateProgram(string[] args)
        {
            Settings settings = ParseCommandlineArguments(args);


            IConfigurationRoot configurationRoot = BuildConfigurationRoot();

            ServiceCollection services = new ServiceCollection();
            services.Configure<Settings>(options => configurationRoot.GetSection("settings").Bind(options));

            LogManager.Configuration = new XmlLoggingConfiguration("NLog.config");
            services.AddLogging(builder => builder.AddNLog());
            services.AddSingleton(_ => new Program(settings));

            Program? program = services
                .BuildServiceProvider()
                .GetService<Program>();
            return program;
        }

        /// <summary>
        /// Validates and parses commandline arguments into a Settings object
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>A Settings object derived from the arguments</returns>
        private static Settings ParseCommandlineArguments(string[] args)
        {
            Settings settings = new Settings();

            ParserResult<Settings> result = Parser.Default.ParseArguments<Settings>(args)
                .WithParsed(o => { settings = o; });

            if (result.Tag == ParserResultType.NotParsed) Environment.Exit(0);

            return settings;
        }
        
        /// <summary>
        /// Prints the program progress to the console
        /// </summary>
        /// <param name="progress"></param>
        private void PrintProgress(MutationRunProgress progress)
        {
            ConsoleLogger.LogInformation($"> [{progress.Progress}%] {progress.Message}");
        }

        /// <summary>
        /// Executes the core program flow for Faultify
        /// </summary>
        private async Task Run()
        {
            ConsoleMessage.PrintLogo();
            ConsoleMessage.PrintSettings(ProgramSettings);

            if (!File.Exists(ProgramSettings.TestProjectPath))
            {
                Logger.Fatal($"The file {ProgramSettings.TestProjectPath} could not be found. Terminating Faultify.");
                Environment.Exit(2); // 0x2 ERROR_FILE_NOT_FOUND
            }

            Progress<MutationRunProgress> progress = new Progress<MutationRunProgress>();
            progress.ProgressChanged += (_, progress) => PrintProgress(progress);

            var progressTracker = new MutationSessionProgressTracker.MutationSessionProgressTracker(progress);

            var pipeline = new Pipeline.Pipeline(progressTracker, ProgramSettings);
            await pipeline.Start(ProgramSettings.TestProjectPath);
            
            progressTracker.LogEndFaultify(ProgramSettings.ReportPath);
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Helper method to generate a ConfigurationRoot
        /// </summary>
        /// <returns>The IConfigurationRoot object</returns>
        private static IConfigurationRoot BuildConfigurationRoot()
        {
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddUserSecrets<Program>();
            IConfigurationRoot configurationRoot = builder.Build();
            return configurationRoot;
        }
    }
}
