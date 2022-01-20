using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Faultify.MutationCollector;
using Faultify.MutationCollector.Mutation;
using Mono.Cecil;

namespace Faultify.AssemblyDissection
{
    /// <summary>
    ///     The `AssemblyMutator` can be used to analyze all kinds of mutations in a target assembly.
    /// </summary>
    public class AssemblyAnalyzer : IDisposable, IMutationProvider
    {
        public AssemblyAnalyzer(string assemblyPath)
        {
            var assemblyResolver = new DefaultAssemblyResolver();
            assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(assemblyPath));
            Module = ModuleDefinition.ReadModule(
                assemblyPath,
                new ReaderParameters
                {
                    InMemory = true,
                    ReadSymbols = true,
                    AssemblyResolver= assemblyResolver,
                }
            );
            Types = LoadTypes();
            AssemblyPath = assemblyPath;
        }

        /// <summary>
        ///     Path of the assembly 
        /// </summary>
        public string AssemblyPath { get; }

        /// <summary>
        ///     Underlying Mono.Cecil ModuleDefinition.
        /// </summary>
        public ModuleDefinition Module { get; }

        /// <summary>
        ///     The types in the assembly.
        /// </summary>
        public Dictionary<string, TypeScope> Types { get; }

        public void Dispose()
        {
            Module?.Dispose();
        }

        /// <summary>
        ///     Loads all of the types within the raw module definition into the class
        /// </summary>
        /// <returns>A List<TypeScope> of types in the module</returns>
        private Dictionary<string, TypeScope> LoadTypes()
        {
            return (
                from type
                    in Module.Types
                where !type.FullName.StartsWith("<")
                select new TypeScope(type, Module.Assembly.Name.Name)
            ).ToDictionary(x => x.Name, x => x);
        }

        /// <summary>
        ///     Flush the assembly changes to the given file.
        /// </summary>
        /// <param name="stream"></param>
        public void Flush(Stream stream)
        {
            Module.Write(stream);
        }

        public void Flush(string path)
        {
            Module.Write(path);
        }

        public void Flush()
        {
            Module.Write(Module.FileName);
        }

        /// <summary>
        ///     Returns all possible mutations.
        ///     Every enumerable of mutations contained in the main enumerable
        ///     applies to the same opcode and was found by the same
        ///     analyzer.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IEnumerable<IMutation>> AllMutations(
            MutationLevel mutationLevel,
            HashSet<string> excludeGroup,
            HashSet<string> excludeSingular)
        {
            var allMutations = new List<IEnumerable<IEnumerable<IMutation>>>();
            
            foreach (var type in Types)
            {
                var mutations
                    = type.Value.AllMutations(mutationLevel, excludeGroup, excludeSingular);
                allMutations.Add(mutations);
            }
            
            return allMutations.SelectMany(x => x);
        }

        /// <summary>
        ///     Get a mutation equivalent to the given one.
        ///
        ///     This can be used when mutating a copy of an
        ///     existing project. The underlying Cecil
        ///     definitions of a mutation still map to the
        ///     original project; this method can be used
        ///     to obtain a new mutation with updated
        ///     fields which can be used on the copy.
        /// </summary>
        public IMutation GetEquivalentMutation(IMutation original)
        {
            TypeScope scope = Types[original.TypeName];
            return scope.GetEquivalentMutation(original);
        }
    }
}
