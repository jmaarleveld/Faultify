using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Faultify.MutationCollector.Mutation
{
    public class VariableMutation : IMutation
    {
        public VariableMutation(
            Instruction instruction, 
            Type type, 
            MethodDefinition method,
            string analyzerName,
            string analyzerDescription)
        {
            Original = instruction.Operand;
            Replacement = RandomValueGenerator.GenerateValueForField(type, Original);
            Variable = instruction;
            MethodScope = method;
            LineNumber = FindLineNumber();
            AnalyzerName = analyzerName;
            AnalyzerDescription = analyzerDescription;
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
                Instruction prev = Variable;
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
                    return $"{MethodScope}: Variable was changed from {Original} to {Replacement}.";
                }

                return $"{MethodScope} at {LineNumber}: Variable was changed from {Original} to {Replacement}.";
                
            }
        }
    }
}
