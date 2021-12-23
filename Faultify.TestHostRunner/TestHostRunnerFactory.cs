using System;
using NLog;
using Faultify.TestHostRunner.TestHostRunners;
using Faultify.TestHostRunner.Enums;

namespace Faultify.TestHostRunner
{
    /// <summary>
    ///     Static factory class for creating testRunners
    /// </summary>
    internal static class TestHostRunnerFactory
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        ///     Create and return an ITestHostRunner
        /// </summary>
        /// <param name="testAssemblyPath">The path of the tested project</param>
        /// <param name="timeOut">The timeout for the test hosts</param>
        /// <param name="testHost">Type of testHost to instantiate</param>
        /// <param name="testHostLogger">Logger class</param>
        /// <returns></returns>
        public static ITestHostRunner CreateTestRunner(string testAssemblyPath, TimeSpan timeOut, TestHost testHost)
        {
            ITestHostRunner testRunner;
            Logger.Info("Creating test runner");
            try
            {
                testRunner = testHost switch
                {
                    TestHost.NUnit => new NUnitTestHostRunner(testAssemblyPath, timeOut),
                    TestHost.XUnit => new XUnitTestHostRunner(testAssemblyPath),
                    TestHost.MsTest => new DotnetTestHostRunner(testAssemblyPath, timeOut),
                    TestHost.DotnetTest => new DotnetTestHostRunner(testAssemblyPath, timeOut),
                    _ => throw new ArgumentOutOfRangeException(nameof(testHost), $"{testHost} not found."),
                };
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.Message + "Defaulting to DotNetTest.");
                testRunner = new DotnetTestHostRunner(testAssemblyPath, timeOut);
            }
            return testRunner;
        }
    }
}
