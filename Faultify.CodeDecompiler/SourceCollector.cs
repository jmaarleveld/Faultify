using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Faultify.AssemblyDissection;
using Faultify.MutationCollector.Mutation;
using Faultify.ProjectDuplicator;
using NLog;

namespace Faultify.CodeDecompiler;

public class SourceCollector
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    
    public static IEnumerable<string> CollectSourceCode(
        IList<IMutation> mutations,
        ITestProjectDuplication projectDuplication,
        Dictionary<string, AssemblyAnalyzer> assemblyAnalyzers)
    {
        // First, group mutations by assembly name
        var orderedMutations = mutations.GroupBy(mutation => mutation.AssemblyName);
        List<string> sourceSnippets = new List<string>(mutations.Count);
        
        foreach (var mutationGrouping in orderedMutations) {
            sourceSnippets.AddRange(
                CollectSourceForGrouping(
                    mutationGrouping.ToList(), 
                    projectDuplication, 
                    assemblyAnalyzers));
        }

        return sourceSnippets;
    }

    private static IEnumerable<string> CollectSourceForGrouping(
        IList<IMutation> mutationGroup, 
        ITestProjectDuplication projectDuplication,
        Dictionary<string, AssemblyAnalyzer> assemblyAnalyzers)
    {
        // First, retrieve the file object from the duplication 
        AssemblyAnalyzer assembly = assemblyAnalyzers[mutationGroup[0].AssemblyName];
        IFileDuplication? reference = projectDuplication.TestProjectReferences.FirstOrDefault(
            x => x.FullFilePath() == assembly.AssemblyPath);
        if (reference == null) {
            Logger.Error($@"Failed to find project info for {assembly.AssemblyPath}");
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
            decompiler = new CodeDecompiler(assembly.AssemblyPath, decompilerStream);
        } catch (Exception e)
        {
            Logger.Debug(e.StackTrace);
            Logger.Warn("Could not decompile project. Source code will not be displayed in the report.");
            decompiler = new NullDecompiler();
        }
        
        // Get the source code for the mutations 
        foreach (var mutation in mutationGroup) {
            var entityHandle = mutation.ParentMethodEntityHandle;
            // Return a "no code" result in case the entity handle is null
            if (entityHandle == null) {
                yield return NullDecompiler.CONSTANT_NULL_CODE;
            }
            else {
                // Cast is now guaranteed not to fail
                yield return decompiler.Decompile(MetadataTokens.EntityHandle((int) entityHandle));
            }
        }
    }
}