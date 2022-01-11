using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Faultify.MemoryTest.TestInformation;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NLog;
using Faultify.TestHostRunner.Results;

namespace Faultify.TestHostRunner.TestHostRunners
{
    public class XUnitTestHostRunner : ITestHostRunner
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly HashSet<string> _coverageTests = new HashSet<string>();
        private readonly string _testProjectAssemblyPath;
        private readonly List<Tuple<string, TestOutcome>> _testResults = new();

        public XUnitTestHostRunner(string testProjectAssemblyPath)
        {
            _testProjectAssemblyPath = testProjectAssemblyPath;
        }

        public TestHost TestHost => TestHost.XUnit;

        public async Task<List<Tuple<string, TestOutcome>>> RunTests(TimeSpan timeout, IProgress<string> progress, IEnumerable<string> tests)
        {
            HashSet<string>? hashedTests = new HashSet<string>(tests);

            MemoryTest.XUnit.XUnitTestHostRunner? xunitHostRunner =
                new MemoryTest.XUnit.XUnitTestHostRunner(_testProjectAssemblyPath);
            xunitHostRunner.TestEnd += OnTestEnd;

            await xunitHostRunner.RunTestsAsync(CancellationToken.None, hashedTests);

            return _testResults;
        }

        public async Task<Dictionary<string, List<Tuple<string, int>>>> RunCodeCoverage(CancellationToken cancellationToken)
        {
            MemoryTest.XUnit.XUnitTestHostRunner? xunitHostRunner =
                new MemoryTest.XUnit.XUnitTestHostRunner(_testProjectAssemblyPath);
            xunitHostRunner.TestEnd += OnTestEndCoverage;

            await xunitHostRunner.RunTestsAsync(CancellationToken.None);

            return ReadCoverageFile();
        }

        private void OnTestEnd(object? sender, TestEnd e)
        {
            _testResults.Add(new Tuple<string, TestOutcome>(e.TestName, ParseTestOutcome(e.TestOutcome)));
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
            }
            catch (ArgumentOutOfRangeException ex)
            {
                _logger.Error(ex, ex.Message + "defautling to Skipped");
                return TestOutcome.Skipped;
            }
        }

        private Dictionary<string, List<Tuple<string, int>>> ReadCoverageFile()
        {
            Dictionary<string, List<Tuple<string, int>>>? mutationCoverage = ResultsUtils
            .ReadMutationCoverageFile();

            mutationCoverage = mutationCoverage
                .Where(pair => _coverageTests.Contains(pair.Key))
                .ToDictionary(pair => pair.Key, pair => pair.Value);

            return mutationCoverage;
        }
    }
}
