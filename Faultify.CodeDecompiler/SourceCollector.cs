using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using Faultify.AssemblyDissection;
using Faultify.MutationCollector.Mutation;
using Faultify.ProjectDuplicator;
using NLog;

namespace Faultify.CodeDecompiler
{
    public class SourceCollector
    {
        private static readonly Logger Logger =
            LogManager.GetCurrentClassLogger();


        public static IEnumerable<string> CollectSourceCode(
            IList<IMutation> mutations,
            ITestProjectDuplication projectDuplication,
            Dictionary<string, AssemblyAnalyzer> assemblyAnalyzers)
        {
            // Group mutations together based on their assembly name,
            // so that we do all mutations for a single assembly 
            // at the same time.
            var orderedMutations = 
                Enumerable.Range(0, mutations.Count()).
                GroupBy(index => mutations[index].AssemblyName);

            // List to store results in
            string[] totalSourceSnippets = new string[mutations.Count];

            foreach (var mutationGrouping in orderedMutations)
            {
                // Get snippets for this group of mutations 
                var sourceSnippets = CollectSourceForGrouping(
                    mutationGrouping.Select(index => mutations[index]).ToList(), 
                    projectDuplication, 
                    assemblyAnalyzers).ToList();
                
                // Add snippets into the result list 
                int innerIndex = 0;
                foreach (var index in mutationGrouping) {
                    totalSourceSnippets[index] = sourceSnippets[innerIndex];
                    innerIndex++;
                }
            }

            return totalSourceSnippets;
        }

        private static IEnumerable<string> CollectSourceForGrouping(
            IList<IMutation> mutationGroup,
            ITestProjectDuplication projectDuplication,
            Dictionary<string, AssemblyAnalyzer> assemblyAnalyzers)
        {
            // First, retrieve the file object from the duplication 
            AssemblyAnalyzer assembly =
                assemblyAnalyzers[mutationGroup[0].AssemblyName];
            IFileDuplication? reference =
                projectDuplication.TestProjectReferences.FirstOrDefault(
                    x => x.FullFilePath() == assembly.AssemblyPath);
            if (reference == null)
            {
                Logger.Error(
                    $@"Failed to find project info for {assembly.AssemblyPath}");
                yield break;
            }

            // initialize the stream 
            using Stream stream = reference.OpenReadStream();
            using MemoryStream decompilerStream = new MemoryStream();
            stream.CopyTo(decompilerStream);
            decompilerStream.Position = 0;

            // Get a decompiler 
            ICodeDecompiler decompiler;
            try
            {
                decompiler = new CodeDecompiler(assembly.AssemblyPath,
                    decompilerStream);
            }
            catch (Exception e)
            {
                Logger.Debug(e.StackTrace);
                Logger.Warn(
                    "Could not decompile project. Source code will not be displayed in the report.");
                decompiler = new NullDecompiler();
            }

            // Get the source code for the mutations 
            foreach (var mutation in mutationGroup)
            {
                var entityHandle = mutation.MemberEntityHandle;
                yield return decompiler.Decompile(
                    MetadataTokens.EntityHandle((int)entityHandle));
            }
        }
    }
}
