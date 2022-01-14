using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Faultify.MutationCollector.Mutation
{
    public class VariableMutation : IMutation
    {
        public VariableMutation(
            Instruction instruction, 
            int instructionIndex,
            Type type, 
            MethodDefinition method,
            string analyzerName,
            string analyzerDescription,
            string assemblyName,
            int memberEntityHandle,
            string typeName,
            string methodName)
        {
            Instruction = instruction;
            instructionIndex = instructionIndex;
            Type = type;
            Original = instruction.Operand;
            Replacement = RandomValueGenerator.GenerateValueForField(type, Original);
            Variable = instruction;
            MethodScope = method;
            LineNumber = FindLineNumber();
            AnalyzerName = analyzerName;
            AnalyzerDescription = analyzerDescription;
            AssemblyName = assemblyName;
            MemberEntityHandle = memberEntityHandle;
            TypeName = typeName;
            MethodName = methodName;
            FieldName = null;
            ClassFieldName = null;
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
         * Mutation and Reporting Functionality 
         */
        
        private Instruction Instruction { get; }
        private int InstructionIndex { get;  }
        private Type Type { get;  }
        
        /// <summary>
        ///     The original variable value.
        /// </summary>
        private object Original { get; set; }

        /// <summary>
        ///     The replacement for the variable value.
        /// </summary>
        private object Replacement { get; set; }

        private MethodDefinition MethodScope { get; set; }
        private int LineNumber { get; set; }

        /// <summary>
        ///     Reference to the variable instruction that can be mutated.
        /// </summary>
        private Instruction Variable { get; set; }

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
            var instruction = methodDefinition.Body.Instructions[InstructionIndex];
            return new VariableMutation(
                instruction,
                InstructionIndex,
                Type,
                methodDefinition,
                AnalyzerName,
                AnalyzerDescription,
                AssemblyName,
                memberEntityHandle,
                TypeName,
                MethodName);
        }

        public void Mutate()
        {
            Variable.Operand = Replacement;
        }

        public void Reset()
        {
            Variable.Operand = Original;
        }

        private int FindLineNumber()
        {
            var debug = MethodScope.DebugInformation.GetSequencePointMapping();
            int lineNumber = -1;

            if (debug != null)
            {
                Instruction? prev = Variable;
                SequencePoint? seqPoint = null;
                // If prev is not null
                // and line number is not found
                // Try previous instruction.
                while (prev != null && !debug.TryGetValue(prev, out seqPoint))
                {
                    prev = prev.Previous;
                }

                if (seqPoint != null)
                {
                    lineNumber = seqPoint.StartLine;
                }
            }
            return lineNumber;
        }


        public string Report
        {
            get
            {
                if (LineNumber == -1)
                {
                    return $"{MethodScope}: Variable was changed from {Original} to {Replacement}.";
                }

                return $"{MethodScope} at {LineNumber}: Variable was changed from {Original} to {Replacement}.";
                
            }
        }
    }
}
