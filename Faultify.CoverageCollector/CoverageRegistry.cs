using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using Faultify.TestHostRunner.Results;

namespace Faultify.CoverageCollector
{
    /// <summary>
    ///     Registry that tracks which tests cover which entity handled.
    ///     This information is used by the test runner to know which mutations can be ran in parallel.
    /// </summary>
    public static class CoverageRegistry
    {
        private static readonly MutationCoverage MutationCoverage = new();
        private static string _currentTestCoverage = "NONE";
        private static readonly object RegisterMutex = new();
        private static MemoryMappedFile _mmf;

        /// <summary>
        ///     Is injected into <Module> by <see cref="TestCoverageInjector" /> and will be called on assembly load.
        /// </summary>
        public static void Initialize()
        {
            AppDomain.CurrentDomain.ProcessExit += OnCurrentDomain_ProcessExit;
            AppDomain.CurrentDomain.UnhandledException += OnCurrentDomain_ProcessExit;
            _mmf = MemoryMappedFile.OpenExisting("CoverageFile", MemoryMappedFileRights.ReadWrite);
        }

        /// <summary>
        ///     Try to write to the mutation coverage fail
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnCurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            try
            {
                ResultsUtils.WriteMutationCoverageFile(MutationCoverage, _mmf);
            }
            catch (Exception)
            {
                // This needs to be fully ignored or the test runner will fail
            }
        }

        /// <summary>
        ///     Registers the given method entity handle as 'covered' by the last registered 'test'
        /// </summary>
        /// <param name="entityHandle"></param>
        public static void RegisterTargetCoverage(string assemblyName, int entityHandle)
        {
            lock (RegisterMutex)
            {
                try
                {
                    if (!MutationCoverage.Coverage.TryGetValue(_currentTestCoverage,
                        out List<RegisteredCoverage> targetHandles))
                    {
                        targetHandles = new List<RegisteredCoverage>();
                        MutationCoverage.Coverage[_currentTestCoverage] = targetHandles;
                    }

                    targetHandles.Add(new RegisteredCoverage(assemblyName, entityHandle));

                    ResultsUtils.WriteMutationCoverageFile(MutationCoverage, _mmf);
                }
                catch (Exception ex)
                {
                    // This needs to be fully ignored or the test runner will fail
                }
            }
        }

        /// <summary>
        ///     Registers the given test case as current method under test.
        /// </summary>
        /// <param name="testName"></param>
        public static void RegisterTestCoverage(string testName)
        {
            _currentTestCoverage = testName;
        }
    }
}
