using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Faultify.TestHostRunner.Results;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Faultify.TestHostRunner.TestHostRunners
{
    /// <summary>
    ///     Interface for running tests and code coverage on some test host.
    /// </summary>
    public interface ITestHostRunner
    {
        /// <summary>
        ///     Identifies what test framework is being used
        /// </summary>
        public TestHost TestHost { get; }

        /// <summary>
        ///     Runs the given tests and returns the results.
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="tests"></param>
        /// <returns>
        ///     A list of the test result from each test in the session.
        /// </returns>
        Task<List<Tuple<string, TestOutcome>>> RunTests(
            TimeSpan timeout,
            IEnumerable<string> tests
        );

        /// <summary>
        ///     Run the code coverage process.
        ///     This process finds out which tests cover which mutations.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>
        ///     Collection with test names as key and the covered method entity handles as value.
        /// </returns>
        Task<Dictionary<string, List<Tuple<string, int>>>> RunCodeCoverage(CancellationToken cancellationToken);
    }
}
