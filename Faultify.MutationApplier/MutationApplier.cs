using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Faultify.AssemblyDissection;
using Faultify.MutationCollector.Mutation;
using Faultify.ProjectDuplicator;
using NLog;


namespace Faultify.MutationApplier
{
    public class MutationApplier
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        public static void ApplyMutations(
            Dictionary<int, IMutation> mutations, 
            HashSet<int> timedOutMutations,
            Dictionary<string, AssemblyAnalyzer> assemblyAnalyzers,
            ITestProjectDuplication testProject)
        {
            // First, apply all mutations from groups that did 
            // not cause a time out.
            foreach (var pair in mutations) {
                var mutationGroupId = pair.Key;
                var mutation = pair.Value;

                if (!timedOutMutations.Contains(mutationGroupId)) {
                    mutation.Mutate();
                }
            }
            
            // Now, flush the mutations to file 
            foreach (var assembly in assemblyAnalyzers.Values) {
                IFileDuplication? fileDuplication = testProject.TestProjectReferences.FirstOrDefault(
                    x => assembly.Module.Name == x.Name);
                if (fileDuplication == null) {
                    Logger.Error(
                        $"Failed to find file duplication for: {assembly.Module.Name}");
                    continue;
                }

                try {
                    using Stream writeStream = fileDuplication.OpenReadWriteStream();
                    using MemoryStream stream = new MemoryStream();
                    assembly.Module.Write(stream);
                    writeStream.Write(stream.ToArray());
                }
                catch (Exception e) {
                    // TODO: What do we want to catch? This was copied from the old code
                    Logger.Error(e, e.Message);
                }
                finally {
                    // TODO: is this necessary?
                    fileDuplication.Dispose();
                }
            }
            
            // TODO: What does this do? Is it necessary?
            testProject.TestProjectFile.Dispose();
        }
    }
}