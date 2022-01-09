namespace Faultify.MutationCollector.Mutation
{
    /// <summary>
    ///     Mutation that can be performed or reverted.
    /// </summary>
    public interface IMutation {
        
        /// <summary>
        ///     Name of the analyzer this mutation was found by.
        /// </summary>
        string AnalyzerName { get; }
        
        /// <summary>
        ///     Description of the analyze this mutation was found by.
        /// </summary>
        string AnalyzerDescription { get; }

        /// <summary>
        ///     Name of the assembly containing this mutation.
        /// </summary>
        string AssemblyName { get; }

        /// <summary>
        ///     EntityHandle referencing the method containing
        ///     this mutation.
        ///
        ///     This field may be null, in case the mutation does
        ///     not occur in a method.
        ///     An example of this would be a mutation of a
        ///     class variable.
        /// </summary>
        int? ParentMethodEntityHandle { get; }

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
