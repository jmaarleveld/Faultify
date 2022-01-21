using System.Collections.Generic;
using Faultify.MutationCollector.Mutation;
using Mono.Cecil.Cil;

namespace Faultify.MutationCollector.AssemblyAnalyzers
{
    /// <summary>
    ///     Interface for analyzers that search for possible source code mutations on byte code level.
    /// </summary>
    /// <typeparam name="TMutation">The type of the returned metadata.</typeparam>
    /// <typeparam name="TScope"></typeparam>
    public interface IAnalyzer<out TMutation, in TScope> where TMutation : IMutation 
    {
        /// <summary>
        ///     Name of the mutator.
        /// </summary>
        string Description { get; }

        /// <summary>
        ///     Name of the mutator.
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     Id of the Analyzer.
        /// </summary>
        string Id { get; }

        /// <summary>
        ///     Analyzes possible mutations in the given scope.
        ///     Returns the mutation that can be either executed or reverted.
        ///
        ///     The AssemblyName and TypeName are always given, and will
        ///     be stored on the mutation object for identification
        ///     purposes. The method name may also be present, and is
        ///     passed as an argument to this method if the mutation
        ///     scope (TScope scope) was found inside a method.
        /// </summary>
        IEnumerable<TMutation> GenerateMutations(
            string assemblyName,
            string typeName,
            string? methodName,
            TScope scope,
            MutationLevel mutationLevel,
            HashSet<string> exclusions,
            string memberName,
            int? parentMethodEntityHandle,
            IDictionary<Instruction, SequencePoint>? debug = null
        );
    }
}
