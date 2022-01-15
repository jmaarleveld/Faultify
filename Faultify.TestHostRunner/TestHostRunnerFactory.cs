using System;
using NLog;
using Faultify.TestHostRunner.TestHostRunners;

namespace Faultify.TestHostRunner
{
    /// <summary>
    ///     Static factory class for creating testRunners
    /// </summary>
    public static class TestHostRunnerFactory
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
            return testHost switch
            {
                TestHost.NUnit => new NUnitTestHostRunner(testAssemblyPath, timeOut),
                TestHost.XUnit => new XUnitTestHostRunner(testAssemblyPath),
                TestHost.MsTest => new DotnetTestHostRunner(testAssemblyPath, timeOut),
                TestHost.DotnetTest => new DotnetTestHostRunner(testAssemblyPath, timeOut),
                _ => new DotnetTestHostRunner(testAssemblyPath, timeOut),
            };
        }
    }
}
