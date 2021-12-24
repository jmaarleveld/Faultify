using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using NLog;

namespace Faultify.TestHostRunner.Results
{
    public static class ResultsUtils
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        ///     Reads the mutation coverage from the <see cref="TestRunnerConstants.CoverageFileName" /> file.
        /// </summary>
        /// <returns></returns>
        public static MutationCoverage ReadMutationCoverageFile()
        {
            Logger.Info("Reading mutation coverage file");
            try
            {
                using MemoryMappedFile mmf = MemoryMappedFile.OpenExisting("CoverageFile");
                using MemoryMappedViewStream stream = mmf.CreateViewStream();
                using MemoryStream memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                memoryStream.Position = 0;

                return MutationCoverage.Deserialize(memoryStream.ToArray());
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "Fatal exception reading file");
            }

            return null;
        }

        /// <summary>
        ///     Write the mutation coverage to the <see cref="TestRunnerConstants.CoverageFileName" /> file.
        /// </summary>
        /// <returns></returns>
        public static void WriteMutationCoverageFile(MutationCoverage mutationCoverage)
        {
            using MemoryMappedFile mmf =
                MemoryMappedFile.OpenExisting("CoverageFile", MemoryMappedFileRights.ReadWrite);
            WriteMutationCoverageFile(mutationCoverage, mmf);
        }

        /// <summary>
        ///     Write the mutation coverage to the <see cref="TestRunnerConstants.CoverageFileName" /> file.
        /// </summary>
        /// <returns></returns>
        public static void WriteMutationCoverageFile(MutationCoverage mutationCoverage, MemoryMappedFile coverageFile)
        {
            using MemoryMappedViewStream stream = coverageFile.CreateViewStream();
            stream.Write(mutationCoverage.Serialize());
            stream.Flush();
        }

        public static MemoryMappedFile CreateCoverageMemoryMappedFile()
        {
            FileStream file = File.Create(TestRunnerConstants.CoverageFileName);
            file.Dispose();

            FileStream fileStream = new FileStream(TestRunnerConstants.CoverageFileName, FileMode.Open,
                FileAccess.ReadWrite, FileShare.ReadWrite);
            return MemoryMappedFile.CreateFromFile(fileStream, "CoverageFile", 20000, MemoryMappedFileAccess.ReadWrite,
                HandleInheritability.None, true);
        }
    }
}
