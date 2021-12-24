namespace Faultify.MutationCollector.Mutation
{
    /// <summary>
    ///     Mutation that can be performed or reverted.
    /// </summary>
    public interface IMutation {
        /// <summary>
        ///     Name of the analyze this mutation was found by.
        /// </summary>
        string AnalyzerName { get; }
        
        /// <summary>
        ///     Description of the analyze this mutation was found by.
        /// </summary>
        string AnalyzerDescription { get; }

        string Report { get; }

        /// <summary>
        ///     Mutates the the bytecode to its mutated version.
        /// </summary>
        void Mutate();

        /// <summary>
        ///     Reverts the mutation to its original.
        /// </summary>
        void Reset();
    }
}
