using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Faultify.MutationCollector.Mutation
{
    /// <summary>
    ///     Opcode mutation that can be performed or reverted.
    /// </summary>
    public class OpCodeMutation : IMutation
    {
        public OpCodeMutation(
            OpCode original, 
            OpCode replacement, 
            Instruction scope, 
            int instructionIndex,
            MethodDefinition method,
            string analyzerName,
            string analyzerDescription,
            string assemblyName,
            int memberEntityHandle,
            string typeName,
            string methodName,
            string memberName,
            int? parentMethodEntityHandle)
        {
            Original = original;
            Replacement = replacement;
            Scope = scope;
            InstructionIndex = instructionIndex;
            MethodScope = method;
            LineNumber = FindLineNumber();
            AnalyzerName = analyzerName;
            AnalyzerDescription = analyzerDescription;
            AssemblyName = assemblyName;
            MemberEntityHandle = memberEntityHandle;
            ClassFieldName = null;
            FieldName = null;
            TypeName = typeName;
            MethodName = methodName;
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
        
        public int? ParentMethodEntityHandle { get; }
        
        /********************************************************************************
         * Mutation and Reporting Functionality 
         */

        /// <summary>
        ///     The original opcode.
        /// </summary>
        private OpCode Original { get; set; }

        /// <summary>
        ///     The replacement for the original opcode.
        /// </summary>
        private OpCode Replacement { get; set; }
        private MethodDefinition MethodScope { get; set; }

        private int LineNumber { get; set; }

        /// <summary>
        ///     Reference to the instruction line in witch the opcode can be mutated.
        /// </summary>
        private Instruction Scope { get; set; }

        /// <summary>
        ///     Index of the instruction in the method body
        /// </summary>
        private int InstructionIndex { get; }

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
            var methodDefinition = (MethodDefinition) definition;
            var instruction = methodDefinition.Body.Instructions[InstructionIndex];
            return new OpCodeMutation(
                Original,
                Replacement,
                instruction,
                InstructionIndex,
                methodDefinition,
                AnalyzerName,
                AnalyzerDescription,
                AssemblyName,
                memberEntityHandle,
                TypeName,
                MethodName,
                MemberName,
                parentMethodEntityHandle);
        }
        
        public string MemberName { get; }

        public void Mutate()
        {
            Scope.OpCode = Replacement;
        }

        public void Reset()
        {
            Scope.OpCode = Original;
        }

        private int FindLineNumber()
        {
            var debug = MethodScope.DebugInformation.GetSequencePointMapping();
            int lineNumber = -1;

            if (debug != null)
            {
                Instruction? prev = Scope;
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
                    return $"{MethodScope}: Operator was changed from {Original} to {Replacement}.";
                }

                return $"{MethodScope} at {LineNumber}: Operator was changed from {Original} to {Replacement}.";

            }
        }

        public bool HasOpcode(OpCode opCode)
        {
            return Scope.OpCode == opCode;
        }
    }
}
