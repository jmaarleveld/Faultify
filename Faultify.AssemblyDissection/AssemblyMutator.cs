using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Faultify.MutationCollector.Mutation;
using Mono.Cecil;

namespace Faultify.AssemblyDissection
{
    /// <summary>
    ///     The `AssemblyMutator` can be used to analyze all kinds of mutations in a target assembly.
    ///     It can be extended with custom analyzers.
    ///     Though an extension must correspond to one of the following collections in `AssemblyMutator`:
    ///     <br /><br />
    ///     - ArrayMutationAnalyzers(<see cref="ArrayAnalyzer" />)<br />
    ///     - ConstantAnalyzers(<see cref="VariableAnalyzer" />)<br />
    ///     - VariableMutationAnalyzer(<see cref="Analyzers.ConstantAnalyzer" />)<br />
    ///     - OpCodeMutationAnalyzer(<see cref="OpCodeAnalyzer" />)<br />
    ///     <br /><br />
    ///     If you add your analyzer to one of those collections then it will be used in the process of analyzing.
    ///     Unfortunately, if your analyzer does not fit the interfaces, it can not be used with the `AssemblyMutator`.
    /// </summary>
    public class AssemblyMutator : IDisposable
    {

        [Obsolete("Use AssemblyMutator(string assemblyPath)")]
        private AssemblyMutator(Stream stream)
        {
            Module = ModuleDefinition.ReadModule(
                stream,
                new ReaderParameters
                {
                    InMemory = true,
                    ReadSymbols = false,
                }
            );
            Types = LoadTypes();
        }

        public AssemblyMutator(string assemblyPath)
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
        }

        /// <summary>
        ///     Underlying Mono.Cecil ModuleDefinition.
        /// </summary>
        public ModuleDefinition Module { get; }

        /// <summary>
        ///     The types in the assembly.
        /// </summary>
        public List<TypeScope> Types { get; }

        public void Dispose()
        {
            Module?.Dispose();
        }

        /// <summary>
        ///     Loads all of the types within the raw module definition into the class
        /// </summary>
        /// <returns>A List<TypeScope> of types in the module</returns>
        private List<TypeScope> LoadTypes()
        {
            return (
                from type
                    in Module.Types
                where !type.FullName.StartsWith("<")
                select new TypeScope(type)
            ).ToList();
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
    }
}
