using System;
using Mono.Cecil;

namespace Faultify.MutationCollector.Mutation
{
    /// <summary>
    ///     Constant mutation that can be performed or reverted.
    /// </summary>
    public class ConstantMutation : IMutation
    {
        public ConstantMutation(
            FieldDefinition field, 
            Type type,
            string analyzerName,
            string analyzerDescription,
            string assemblyName,
            int memberEntityHandle,
            string typeName,
            string? classFieldName,
            string? methodName,
            string? fieldName,
            string memberName,
            int? parentMethodEntityHandle)
        {
            Name = field.Name;
            Original = field.Constant;
            Type = type;
            Replacement = RandomValueGenerator.GenerateValueForField(type, Original);
            ConstantField = field;
            AnalyzerName = analyzerName;
            AnalyzerDescription = analyzerDescription;
            AssemblyName = assemblyName;
            MemberEntityHandle = memberEntityHandle;
            TypeName = typeName;
            ClassFieldName = classFieldName;
            MethodName = methodName;
            FieldName = fieldName;
            MemberName = memberName;
            ParentMethodEntityHandle = parentMethodEntityHandle;
        }
        
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
        public string? MethodName { get; }
        
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
        
        public int? ParentMethodEntityHandle { get; }
        
        /********************************************************************************
         * Mutation and Reporting Functionality 
         */

        private Type Type { get; }

        /// <summary>
        ///     The name of the constant.
        /// </summary>
        private string Name { get; set; }

        /// <summary>
        ///     The original constant value.
        /// </summary>
        private object Original { get; set; }

        /// <summary>
        ///     The replacement for the ConstantMutation value.
        /// </summary>
        private object Replacement { get; set; }

        /// <summary>
        ///     Reference to the constant field that can be mutated.
        /// </summary>
        private FieldDefinition ConstantField { get; set; }

        /// <summary>
        ///     Generate a mutation equivalent to the current one for a
        ///     class in a different project.
        ///
        ///     Mutations are originally analyzed on one copy of the project;
        ///     to avoid needing to generate them all again for other copies,
        ///     this method allows making a copy for a specific copy.
        /// </summary>
        /// <param name="definition">field definition in the copy</param>
        /// <param name="memberEntityHandle">entity handle of parent member</param>
        /// <param name="parentMethodEntityHandle"></param>
        /// <returns>new, equivalent mutation</returns>
        public IMutation GetEquivalentMutation(
            IMemberDefinition definition,
            int memberEntityHandle,
            int? parentMethodEntityHandle)
        {
            var fieldDefinition = (FieldDefinition) definition;
            return new ConstantMutation(
                fieldDefinition,
                Type,
                AnalyzerName,
                AnalyzerDescription,
                AssemblyName,
                memberEntityHandle,
                TypeName,
                ClassFieldName,
                MethodName,
                FieldName,
                MemberName,
                parentMethodEntityHandle);
        }
        
        public string MemberName { get; }

        public void Mutate()
        {
            ConstantField.Constant = Replacement;
        }

        public void Reset()
        {
            ConstantField.Constant = Original;
        }

        public bool HasConstant(object constant)
        {
            return ConstantField.Constant.Equals(constant);
        }

        public string Report => $"{ConstantField} was changed from {Original} to {Replacement}.";
    }
}
