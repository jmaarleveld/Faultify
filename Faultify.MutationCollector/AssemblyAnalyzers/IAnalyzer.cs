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
        /// </summary>
        /// <param name="assemblyName">Name of the assembly containing this mutation</param> 
        /// <param name="scope">Scope in which to evaluate mutations</param>
        /// <param name="mutationLevel">Optimization and coverage level</param>
        /// <param name="exclusions"> List of excluded mutations</param>
        /// <returns>A <see cref="IMutationGroup{T}" /> containing the mutations</returns>
        IEnumerable<TMutation> GenerateMutations(
            string assemblyName,
            TScope scope,
            MutationLevel mutationLevel,
            HashSet<string> exclusions,
            IDictionary<Instruction, SequencePoint> debug = null
        );
    }
}
