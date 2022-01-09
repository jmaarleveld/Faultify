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
            int? parentMethodEntityHandle)
        {
            Name = field.Name;
            Original = field.Constant;
            Replacement = RandomValueGenerator.GenerateValueForField(type, Original);
            ConstantField = field;
            AnalyzerName = analyzerName;
            AnalyzerDescription = analyzerDescription;
            AssemblyName = assemblyName;
            ParentMethodEntityHandle = parentMethodEntityHandle;
        }
        
        /// <summary>
        ///     Name of the analyze this mutation was found by.
        /// </summary>
        public string AnalyzerName { get; }
        
        /// <summary>
        ///     Description of the analyze this mutation was found by.
        /// </summary>
        public string AnalyzerDescription { get; }
        
        /// <summary>
        ///     Name of the assembly containing this mutation.
        /// </summary>
        public string AssemblyName { get; }

        /// <summary>
        ///     EntityHandle referencing the method containing
        ///     this mutation.
        ///
        ///     This field may be null, in case the mutation does
        ///     not occur in a method.
        ///     An example of this would be a mutation of a
        ///     class variable.
        /// </summary>
        public int? ParentMethodEntityHandle { get; }

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
