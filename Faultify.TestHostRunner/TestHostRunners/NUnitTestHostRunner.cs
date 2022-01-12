﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Faultify.MemoryTest.TestInformation;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NLog;
using Faultify.TestHostRunner.Results;

namespace Faultify.TestHostRunner.TestHostRunners
{
    public class NUnitTestHostRunner : ITestHostRunner
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly HashSet<string> _coverageTests = new HashSet<string>();
        private readonly string _testProjectAssemblyPath;
        private readonly List<Tuple<string, TestOutcome>> _testResults
            = new List<Tuple<string, TestOutcome>>();
        private readonly TimeSpan _timeout;

        public NUnitTestHostRunner(string testProjectAssemblyPath, TimeSpan timeout)
        {
            _logger.Info("Creating test runner");
            _testProjectAssemblyPath = testProjectAssemblyPath;
            _timeout = timeout;
        }

        public TestHost TestHost => TestHost.NUnit;

        public async Task<List<Tuple<string, TestOutcome>>> RunTests(TimeSpan timeout, IProgress<string> progress, IEnumerable<string> tests)
        {
            _logger.Info("Running tests");
            HashSet<string>? hashedTests = new HashSet<string>(tests);

            MemoryTest.NUnit.NUnitTestHostRunner? nunitHostRunner =
                new MemoryTest.NUnit.NUnitTestHostRunner(_testProjectAssemblyPath);
            nunitHostRunner.Settings.Add("DefaultTimeout", 3000);
            nunitHostRunner.Settings.Add("StopOnError", false);
            nunitHostRunner.Settings.Add("BaseDirectory", new FileInfo(_testProjectAssemblyPath).DirectoryName);

            nunitHostRunner.TestEnd += OnTestEnd;

            await nunitHostRunner.RunTestsAsync(CancellationToken.None, hashedTests);

            return _testResults;
        }

        public async Task<Dictionary<string, List<Tuple<string, int>>>> RunCodeCoverage(CancellationToken cancellationToken)
        {
            _logger.Info("Running code coverage");
            MemoryTest.NUnit.NUnitTestHostRunner? nunitHostRunner =
                new MemoryTest.NUnit.NUnitTestHostRunner(_testProjectAssemblyPath);
            nunitHostRunner.Settings.Add("DefaultTimeout", 1000);
            nunitHostRunner.Settings.Add("StopOnError", false);
            nunitHostRunner.Settings.Add("BaseDirectory", new FileInfo(_testProjectAssemblyPath).DirectoryName);

            nunitHostRunner.TestEnd += OnTestEndCoverage;

            await nunitHostRunner.RunTestsAsync(CancellationToken.None);

            return ReadCoverageFile();
        }

        private void OnTestEnd(object? sender, TestEnd e)
        {
            _testResults.Add(new Tuple<string, TestOutcome>(e.FullTestName, ParseTestOutcome(e.TestOutcome)));
        }

        private void OnTestEndCoverage(object? sender, TestEnd e)
        {
            _coverageTests.Add(e.FullTestName);
        }

        private TestOutcome ParseTestOutcome(MemoryTest.TestOutcome outcome)
        {
            try
            {
                return outcome switch
                {
                    MemoryTest.TestOutcome.Passed => TestOutcome.Passed,
                    MemoryTest.TestOutcome.Failed => TestOutcome.Failed,
                    MemoryTest.TestOutcome.Skipped => TestOutcome.Skipped,
                    _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null),
                };
            } catch(ArgumentOutOfRangeException ex)
            {
                _logger.Error(ex, ex.Message + "defautling to Skipped");
                return TestOutcome.Skipped;
            }
        }

        private Dictionary<string, List<Tuple<string, int>>> ReadCoverageFile()
        {
            _logger.Info("Reading coverage file");
            Dictionary<string, List<Tuple<string, int>>>? methodsPerTest = ResultsUtils
            .ReadMethodsPerTestFile();

            methodsPerTest = methodsPerTest
                .Where(pair => _coverageTests.Contains(pair.Key))
                .ToDictionary(pair => pair.Key, pair => pair.Value);

            return methodsPerTest;
        }
    }
}
