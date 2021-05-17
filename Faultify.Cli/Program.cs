﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Faultify.Report;
using Faultify.Report.Models;
using Faultify.Report.Reporters;
using Faultify.TestRunner;
using Faultify.TestRunner.Logging;
using Karambolo.Extensions.Logging.File;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Faultify.Cli
{
    /// <summary>
    /// The main instance of Faultify
    /// </summary>
    internal class Program
    {
        private static string? _outputDirectory;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Program entrypoint
        /// </summary>
        /// <param name="args">Commandline arguments</param>
        /// <exception cref="Exception">Faultify service cannot be accessed</exception>
        private static async Task Main(string[] args)
        {
            Settings settings = ParseCommandlineArguments(args);

            var currentDate = DateTime.Now.ToString("yy-MM-dd");
            _outputDirectory = Path.Combine(settings.ReportPath, currentDate);

            Directory.CreateDirectory(_outputDirectory);

            IConfigurationRoot configurationRoot = BuildConfigurationRoot();

            ServiceCollection services = new ServiceCollection();
            services.Configure<Settings>(options => configurationRoot.GetSection("settings").Bind(options));
            
            LogManager.Configuration = new XmlLoggingConfiguration("NLog.config");
            services.AddLogging(builder => builder.AddNLog());
            
            services.AddSingleton<Program>();

            ServiceProvider serviceProvider = services.BuildServiceProvider();
            Program program = serviceProvider.GetService<Program>()
                ?? throw new Exception("Couldn't get the Faultify service from the system");


            await program.Run(settings);
        }
        
        /// <summary>
        ///     Sets up NLog configuration programmatically
        /// </summary>
        private static void ConfigureNLog()
        {
            var logPath = $"{DateTime.Now.ToString("yy.MM.dd-HH.mm.ss")}.log";
            var logFormat = "[${level:uppercase=true}] ${longdate} | ${logger} :: ${message}";

            // Clear existing log
            if (File.Exists(logPath))
            {
                File.Delete(logPath);
            }

            // Initialize configuration
            LoggingConfiguration config = new LoggingConfiguration();

            // File target configuration
            FileTarget logfile = new FileTarget("logfile")
            {
                FileName = logPath,
                Layout = logFormat,
            };

            config.AddRule(
                NLog.LogLevel.Trace,
                NLog.LogLevel.Fatal,
                logfile
            );

            // console target configuration
            ColoredConsoleTarget logconsole = new ColoredConsoleTarget("logconsole")
            {
                Layout = logFormat,
            };

            config.AddRule(
                NLog.LogLevel.Info,
                NLog.LogLevel.Fatal,
                logconsole
            );

            // Apply configuration
            LogManager.Configuration = config;
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
            if (progress.LogMessageType == LogMessageType.TestRunUpdate
                || progress.LogMessageType != LogMessageType.Other)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"\n> [{progress.Progress}%] {progress.Message}");
                Console.ForegroundColor = ConsoleColor.White;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"> [{progress.Progress}%] {progress.Message}");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        /// <summary>
        /// Executes the core program flow for faultify
        /// </summary>
        /// <param name="settings">A settings object containing the parameters for this program run</param>
        private async Task Run()
        {
            ConsoleMessage.PrintLogo();
            ConsoleMessage.PrintSettings(settings);

            if (!File.Exists(settings.TestProjectPath))
            {
                Logger.Fatal($"The file {settings.TestProjectPath} could not be found. Terminating Faultify.");
                Environment.Exit(2); // 0x2 ERROR_FILE_NOT_FOUND
            }

            Progress<MutationRunProgress> progress = new Progress<MutationRunProgress>();
            progress.ProgressChanged += (sender, progress) => PrintProgress(progress);

            MutationSessionProgressTracker progressTracker =
                new MutationSessionProgressTracker(progress);
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            TestProjectReportModel testResult = await RunMutationTest(settings, progressTracker);
            stopWatch.Stop();
            Console.WriteLine("runtime of RunMutationTest(program.cs line 185): " + stopWatch.Elapsed);

            progressTracker.LogBeginReportBuilding(settings.ReportType, settings.ReportPath);
            await GenerateReport(testResult, settings);
            progressTracker.LogEndFaultify(settings.ReportPath);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Runs coverage, analysis, mutations and tests
        /// </summary>
        /// <param name="settings">Program settings</param>
        /// <param name="progressTracker">Progress tracker</param>
        /// <returns>A report model with the results of the tests</returns>
        private async Task<TestProjectReportModel> RunMutationTest(
            Settings settings,
            MutationSessionProgressTracker progressTracker
        )
        {
            MutationTestProject mutationTestProject = new MutationTestProject(
                settings.TestProjectPath,
                settings.MutationLevel,
                settings.Parallel,
                settings.TestHost,
                settings.TimeOut,
                settings.ExcludeMutationGroups.ToHashSet<string>(),
                settings.ExcludeSingleMutations
            );

            return await mutationTestProject.Test(progressTracker, CancellationToken.None);
        }

        /// <summary>
        /// Builds a report based on the test results and program settings
        /// </summary>
        /// <param name="testResult">Report model with the test results</param>
        private async Task GenerateReport(TestProjectReportModel testResult)
        {
            if (string.IsNullOrEmpty(settings.ReportPath))
            {
                settings.ReportPath = Directory.GetCurrentDirectory();
            }

            MutationProjectReportModel mprm = new MutationProjectReportModel();
            mprm.TestProjects.Add(testResult);

            IReporter reporter = ReportFactory(settings.ReportType);
            byte[] reportBytes = await reporter.CreateReportAsync(mprm);

            string reportFileName = DateTime.Now.ToString("yy-MM-dd-H-mm") + reporter.FileExtension;

            await File.WriteAllBytesAsync(Path.Combine(_outputDirectory ?? string.Empty, reportFileName), reportBytes);
        }
        
        /// <summary>
        /// Selects the appropriate Report Builder
        /// </summary>
        /// <param name="type">Report type, must be one of "PDF", "HTML", or "JSON"</param>
        /// <returns>The reporter instance</returns>
        /// <exception cref="ArgumentOutOfRangeException">Wrong report type</exception>
        private IReporter ReportFactory(string type)
        {
            try
            {
                return type?.ToUpper() switch
                {
                    "PDF" => new PdfReporter(),
                    "HTML" => new HtmlReporter(),
                    "JSON" => new JsonReporter(),
                    _ => throw new ArgumentOutOfRangeException(type, $"The argument \"{type}\" is not a valid file output type." +
                        "Defaulting to JSON."),
                };
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Logger.Error(ex, ex.Message);
                return new JsonReporter();
            }
            
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
