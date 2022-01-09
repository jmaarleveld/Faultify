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
            MethodDefinition method,
            string analyzerName,
            string analyzerDescription,
            string assemblyName,
            int? parentMethodEntityHandle)
        {
            Original = original;
            Replacement = replacement;
            Scope = scope;
            MethodScope = method;
            LineNumber = FindLineNumber();
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
                Instruction prev = Scope;
                SequencePoint seqPoint = null;
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
