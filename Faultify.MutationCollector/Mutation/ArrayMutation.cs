using Faultify.MutationCollector.ArrayMutationStrategy;
using Mono.Cecil;
using MonoMod.Utils;

namespace Faultify.MutationCollector.Mutation
{
    /// <summary>
    ///     Array Mutation, receives specific Strategy and MethodDefinition. The logic for the methods depends on a given
    ///     Strategy.
    /// </summary>
    public class ArrayMutation : IMutation
    {
        private readonly IArrayMutationStrategy _arrayMutationStrategy;
        private MethodDefinition Replacement { get; }
        private MethodDefinition Original { get; }
        
        /********************************************************************************
         * Analyzer information
         */

        /// <summary>
        ///     Name of the analyze this mutation was found by.
        /// </summary>
        public string AnalyzerName { get; }
        
        /// <summary>
        ///     Description of the analyze this mutation was found by.
        /// </summary>
        public string AnalyzerDescription { get; }
        
        /********************************************************************************
         * Assembly Information
         */
        
        /// <summary>
        ///     Name of the assembly containing this mutation.
        /// </summary>
        public string AssemblyName { get; }
        
        /// <summary>
        ///     Name of the class containing this mutation 
        /// </summary>
        public string TypeName { get; }
        
        /// <summary>
        ///     If this mutation occurs in a class variable,
        ///     the name of the variable is stored here.
        /// </summary>
        public string? ClassFieldName { get; }

        /// <summary>
        ///     If the mutation occurs in a method,
        ///     the name of the method is stored here.
        /// </summary>
        public string MethodName { get; }
        
        /// <summary>
        ///     If the mutation occurs in a field inside a
        ///     method, the name of the field is stored here.
        ///
        ///     Note that the method name will also be stored.
        /// </summary>
        public string? FieldName { get; }

        /// <summary>
        ///     EntityHandle referencing the member containing
        ///     this mutation.
        /// </summary>
        public int MemberEntityHandle { get; }
        
        /********************************************************************************
         * Methods
         */

        public ArrayMutation(
            IArrayMutationStrategy mutationStrategy, 
            MethodDefinition methodDef,
            string analyzerName,
            string analyzerDescription,
            string assemblyName,
            int memberEntityHandle,
            string typeName,
            string methodName)
        {
            _arrayMutationStrategy = mutationStrategy;
            Original = methodDef;
            Replacement = Original.Clone();
            AnalyzerName = analyzerName;
            AnalyzerDescription = analyzerDescription;
            AssemblyName = assemblyName;
            MemberEntityHandle = memberEntityHandle;
            ClassFieldName = null;
            FieldName = null;
            MethodName = methodName;
            TypeName = typeName;
        }

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
        /// <param name="memberEntityHandle">entity handle of parent member</param>
        /// <returns>new, equivalent mutation</returns>
        public IMutation GetEquivalentMutation(
            IMutation original, 
            IMemberDefinition definition,
            int memberEntityHandle)
        {
            var methodDefinition = (MethodDefinition) definition;
            return new ArrayMutation(
                new DynamicArrayRandomizerStrategy(methodDefinition),
                methodDefinition,
                AnalyzerName,
                AnalyzerDescription,
                AssemblyName,
                memberEntityHandle,
                TypeName,
                MethodName);
        }


        /// <summary>
        ///     Mutates Array. Mutate logic depends on given Strategy.
        /// </summary>
        public void Mutate()
        {
            _arrayMutationStrategy.Mutate();
        }

        /// <summary>
        ///     Undo functionality for mutation array.
        /// </summary>
        public void Reset()
        {
            _arrayMutationStrategy.Reset(Original, Replacement);
        }

        public string Report => $"{Original} Change array contents.";
    }
}
