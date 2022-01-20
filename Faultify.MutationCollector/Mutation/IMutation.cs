using Mono.Cecil;

namespace Faultify.MutationCollector.Mutation
{
    /// <summary>
    ///     Mutation that can be performed or reverted.
    /// </summary>
    public interface IMutation {
        
        /********************************************************************************
         * Analyzer information
         */

        /// <summary>
        ///     Name of the analyzer this mutation was found by.
        /// </summary>
        string AnalyzerName { get; }
        
        /// <summary>
        ///     Description of the analyze this mutation was found by.
        /// </summary>
        string AnalyzerDescription { get; }
        
        /********************************************************************************
         * Assembly Information
         */

        /// <summary>
        ///     Name of the assembly containing this mutation.
        /// </summary>
        string AssemblyName { get; }

        /// <summary>
        ///     Name of the class containing this mutation 
        /// </summary>
        string TypeName { get; }
        
        /// <summary>
        ///     If this mutation occurs in a class variable,
        ///     the name of the variable is stored here.
        /// </summary>
        string? ClassFieldName { get; }

        /// <summary>
        ///     If the mutation occurs in a method,
        ///     the name of the method is stored here.
        /// </summary>
        string? MethodName { get; }
        
        /// <summary>
        ///     If the mutation occurs in a field inside a
        ///     method, the name of the field is stored here.
        ///
        ///     Note that the method name will also be stored.
        /// </summary>
        string? FieldName { get; }

        /// <summary>
        ///     EntityHandle referencing the member containing
        ///     this mutation.
        ///
        ///     This field may be null, in case the mutation does
        ///     not occur in a method.
        ///     An example of this would be a mutation of a
        ///     class variable.
        /// </summary>
        int MemberEntityHandle { get; }
        
        /********************************************************************************
         * Mutation and Reporting Functionality 
         */

        /// <summary>
        ///     Generate a mutation equivalent to the current one for a
        ///     class in a different project.
        ///
        ///     Mutations are originally analyzed on one copy of the project;
        ///     to avoid needing to generate them all again for other copies,
        ///     this method allows making a copy for a specific copy.
        /// </summary>
        /// <param name="original">original mutation</param>
        /// <param name="definition">field definition in the copy</param>
        /// <param name="memberEntityHandle">entity handle of parent method</param>
        /// <returns>new, equivalent mutation</returns>
        IMutation GetEquivalentMutation(
            IMemberDefinition definition,
            int memberEntityHandle);

        string Report { get; }
        public string MemberName { get; }

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
