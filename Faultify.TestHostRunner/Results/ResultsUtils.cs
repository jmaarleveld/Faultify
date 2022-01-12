﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
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
        public static Dictionary<string, List<Tuple<string, int>>> ReadMethodsPerTestFile()
        {
            Logger.Info("Reading mutation coverage file");
            try
            {
                using MemoryMappedFile mmf = MemoryMappedFile.OpenExisting("CoverageFile");
                using MemoryMappedViewStream stream = mmf.CreateViewStream();
                using MemoryStream memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                memoryStream.Position = 0;

                return DeserializeMethodsPerTest(memoryStream.ToArray());
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
        public static void WriteMethodsPerTestFile(Dictionary<string, List<Tuple<string, int>>> methodsPerTest)
        {
            using MemoryMappedFile mmf =
                MemoryMappedFile.OpenExisting("CoverageFile", MemoryMappedFileRights.ReadWrite);
            WriteMethodsPerTestFile(methodsPerTest, mmf);
        }

        /// <summary>
        ///     Write the mutation coverage to the <see cref="TestRunnerConstants.CoverageFileName" /> file.
        /// </summary>
        /// <returns></returns>
        public static void WriteMethodsPerTestFile(Dictionary<string, List<Tuple<string, int>>> methodsPerTest, MemoryMappedFile coverageFile)
        {
            using MemoryMappedViewStream stream = coverageFile.CreateViewStream();
            stream.Write(SerializeMethodsPerTest(methodsPerTest));
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
        
        public static byte[] SerializeMethodsPerTest(Dictionary<string, 
        List<Tuple<string, int>>> coverage)
        {
            MemoryStream memoryStream = new MemoryStream();
            BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
            binaryWriter.Write(coverage.Count);
            foreach ((string key, List<Tuple<string, int>> value) in coverage)
            {
                binaryWriter.Write(key);
                binaryWriter.Write(value.Count);
                foreach (Tuple<string, int> entityHandle in value)
                {
                    binaryWriter.Write(entityHandle.Item1);
                    binaryWriter.Write(entityHandle.Item2);
                }
            }

            return memoryStream.ToArray();
        }
        
        public static Dictionary<string, List<Tuple<string, int>>> DeserializeMethodsPerTest(byte[] data)
        {
            Dictionary<string, List<Tuple<string, int>>> methodsPerTest
                = new Dictionary<string, List<Tuple<string, int>>>();
            MemoryStream memoryStream = new MemoryStream(data);
            BinaryReader binaryReader = new BinaryReader(memoryStream);

            int count = binaryReader.ReadInt32();
            for (var i = 0; i < count; i++)
            {
                string key = binaryReader.ReadString();
                int listCount = binaryReader.ReadInt32();
                List<Tuple<string, int>> entityHandles = new List<Tuple<string, int>>(listCount);
                for (var j = 0; j < listCount; j++)
                {
                    string fullQualifiedName = binaryReader.ReadString();
                    int entityHandle = binaryReader.ReadInt32();
                    entityHandles.Add(new Tuple<string, int>(fullQualifiedName, entityHandle));
                }

                methodsPerTest.Add(key, entityHandles);
            }

            return methodsPerTest;
        }
        
        public static byte[] SerializeTestResults(List<Tuple<string, 
        TestOutcome>> tests)
        {
            MemoryStream memoryStream = new MemoryStream();
            BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
            binaryWriter.Write(tests.Count);
            foreach (Tuple<string, TestOutcome> testResult in tests)
            {
                binaryWriter.Write(testResult.Item1);
                binaryWriter.Write((int) testResult.Item2);
            }

            return memoryStream.ToArray();
        }
        
        public static List<Tuple<string, TestOutcome>> DeserializeTestResults(byte[] data)
        {
            List<Tuple<string, TestOutcome>> testResults
                = new List<Tuple<string, TestOutcome>>();
            MemoryStream memoryStream = new MemoryStream(data);
            BinaryReader binaryReader = new BinaryReader(memoryStream);
            int count = binaryReader.ReadInt32();
            for (var i = 0; i < count; i++)
            {
                string name = binaryReader.ReadString();
                TestOutcome testOutcome = (TestOutcome) binaryReader.ReadInt32();
                Tuple<string, TestOutcome> testResult
                    = new Tuple<string, TestOutcome>(name, testOutcome);
                testResults.Add(testResult);
            }

            return testResults;
        }
    }
}
