using System;
using System.IO;
using System.Linq;
using CommandLine;
using Faultify.MutationCollector;
using System.Text.Json;
using System.Collections.Generic;
using Faultify.Report;
using Faultify.TestHostRunner;
using NLog;

// Disable Non-nullable is uninitialized, this is handled by the CommandLine package
#pragma warning disable 8618

namespace Faultify.Cli
{
    
    internal class Settings
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The path pointing to the test project project file.
        /// </summary>
        [Option('i', "inputProject", Required = true,
            HelpText = "The path pointing to the test project project file.")]
        public string TestProjectPath { get; set; }

        /// <summary>
        /// The root directory were the reports will be saved.
        /// </summary>
        [Option('r', "reportPath", Required = false,
            HelpText = "The path were the report will be saved.")]
        public string ReportPath { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "FaultifyOutput");

        /// <summary>
        /// Type of report to be generated, options: 'Pdf', 'Html', 'Json'
        /// </summary>
        [Option('f', "reportFormat", Required = false, Default = "Json",
            HelpText = "Type of report to be generated, options: 'Pdf', 'Html', 'Json'")]
        public string ReportType { get; set; }

        /// <summary>
        /// The mutation level indicating the test depth.
        /// </summary>
        [Option('l', "mutationLevel", Required = false, Default = MutationLevel.Detailed,
            HelpText = "The mutation level indicating the test depth.")]
        public MutationLevel MutationLevel { get; set; }

        /// <summary>
        /// The name of the test host framework. options:  "NUnit", "XUnit", "MsTest", "DotnetTest".
        /// </summary>
        [Option('t', "testHost", Required = false, Default = nameof(TestHost.DotnetTest),
            HelpText = "The name of the test host framework.")]
        public string _testHostName { get; set; }

        /// <summary>
        /// Time out in seconds for each mutation.
        /// </summary>
        [Option('d', "timeOut", Required = false, Default = 0,
            HelpText = "Time out in seconds for the mutations")]
        public double _seconds { get; set; }

        /// <summary>
        /// Mutation groups to be excluded
        /// </summary>
        [Option('g', "excludeMutationGroups", Required = false, Default = null,
            HelpText = "Mutation groups to be excluded")]
        public IEnumerable<string> ExcludeMutationGroups { get; set;}

        /// <summary>
        /// Individual mutation ids to be excluded
        /// </summary>
        [Option('s', "excludeMutationSingles", Required = false, Default = null,
            HelpText = "Individual mutation ids to be excluded")]
        public IEnumerable<string> _excludeMutationSingles { get; set; }

        /// <summary>
        /// Path to a json file detailing individual mutations to be excluded
        /// </summary>
        [Option('e', "excludeMutationSinglesFile", Required = false, Default = "NoFile",
            HelpText = "Path to a json file detailing individual mutations to be excluded")]
        public string _excludeMutationSinglesFile { get; set; }

        /// <summary>
        /// Set of individual mutations that should be excluded
        /// </summary>
        public HashSet<string> ExcludeSingleMutations 
        { 
            get
            {
                if (_excludeMutationSinglesFile.Equals("NoFile"))
                {
                    return _excludeMutationSingles.ToHashSet<string>();
                }

                string[]? excludeMutations;
                try
                {
                    string jsonString = File.ReadAllText(_excludeMutationSinglesFile);
                    excludeMutations = JsonSerializer.Deserialize<string[]>(jsonString);
                }
                catch(Exception ex)
                {
                    Logger.Warn(ex, "Defaulting to -s exclusions" + ex.Message);
                    excludeMutations = null;
                }
                
                return excludeMutations == null
                    ? _excludeMutationSingles.ToHashSet()
                    : new HashSet<string>(excludeMutations);
            }
        }
        
        /// <summary>
        /// Time out in seconds for each mutation.
        /// </summary>
        public TimeSpan TimeOut => TimeSpan.FromSeconds(_seconds);

        /// <summary>
        /// The name of the test host framework. options:  "NUnit", "MsTest", "DotnetTest".
        /// </summary>
        public TestHost TestHost
        {
            get => Enum.Parse<TestHost>(_testHostName, true);
            set => _testHostName = value.ToString();
        }
        
        /// <summary>
        /// The file type of the report. options:  "pdf", "html", "json".
        /// </summary>
        public ReporterType ReporterType
        {
            get
            {
                try
                {
                    return Enum.Parse<ReporterType>(ReportType, true);
                }
                catch (ArgumentException ex)
                {
                    Logger.Error(ex, $"The argument \"{ReportType}\" is not a valid file output type. Defaulting to Json.");
                    return ReporterType.Json;
                }
            }
            set => ReportType = value.ToString();
        }

        /// <summary>
        /// The directory where the current report will be saved to
        /// </summary>
        public string OutputDirectory => Path.Combine(ReportPath, DateTime.Now.ToString("yy-MM-dd"));
    }
}
