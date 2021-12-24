using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace Faultify.MutationCollector.AssemblyAnalyzers
{
    /// <summary>
    ///     Analyzer that searches for possible arithmetic mutations inside a method definition.
    ///     Mutations such as '+' to '-', '*', '/', and '%'.
    /// </summary>
    public class ArithmeticAnalyzer : OpCodeAnalyzer
    {
        private static readonly Dictionary<OpCode, IEnumerable<(MutationLevel, OpCode, string)>> Arithmetic =
            new Dictionary<OpCode, IEnumerable<(MutationLevel, OpCode, string)>>
            {
                // Opcodes for mutating arithmetic operation: '+' to '-' ,  '*',  '/' , and '%'.
                {
                    OpCodes.Add, new[]
                    {
                        (MutationLevel.Simple, OpCodes.Sub, "addToSub"),
                        (MutationLevel.Medium, OpCodes.Mul, "addToMul"),
                        (MutationLevel.Detailed, OpCodes.Div, "addToDiv"),
                        (MutationLevel.Detailed, OpCodes.Rem, "addToRem"),
                    }
                },

                // Opcodes for mutating arithmetic operation: '-' to '+' ,  '*',  '/' , and '%'.
                {
                    OpCodes.Sub, new[]
                    {
                        (MutationLevel.Simple, OpCodes.Add, "subToAdd"),
                        (MutationLevel.Medium, OpCodes.Mul, "subToMul"),
                        (MutationLevel.Detailed, OpCodes.Div, "subToDiv"),
                        (MutationLevel.Detailed, OpCodes.Rem, "subToRem"),
                    }
                },

                // Opcodes for mutating arithmetic operation: '*' to '+' ,  '-',  '/' , and '%'.
                {
                    OpCodes.Mul, new[]
                    {
                        (MutationLevel.Simple, OpCodes.Add, "mulToAdd"),
                        (MutationLevel.Medium, OpCodes.Sub, "mulToSub"),
                        (MutationLevel.Detailed, OpCodes.Div, "mulToDiv"),
                        (MutationLevel.Detailed, OpCodes.Rem, "mulToRem"),
                    }
                },

                // Opcodes for mutating arithmetic operation: '/' to '+' ,  '-',  '*' , and '%'.
                {
                    OpCodes.Div, new[]
                    {
                        (MutationLevel.Simple, OpCodes.Add, "divToAdd"),
                        (MutationLevel.Medium, OpCodes.Mul, "divToMul"),
                        (MutationLevel.Detailed, OpCodes.Sub, "divToSub"),
                        (MutationLevel.Detailed, OpCodes.Rem, "divToRem"),
                    }
                },

                // Opcodes for mutating arithmetic operation: '%' to '+' ,  '-',  '*' , and '/'.
                {
                    OpCodes.Rem, new[]
                    {
                        (MutationLevel.Simple, OpCodes.Add, "remToAdd"),
                        (MutationLevel.Medium, OpCodes.Mul, "remToMul"),
                        (MutationLevel.Detailed, OpCodes.Div, "remToDiv"),
                        (MutationLevel.Detailed, OpCodes.Sub, "remToSub"),
                    }
                },
            };

        public ArithmeticAnalyzer() : base(Arithmetic) { }

        public override string Description =>
            "Analyzer that searches for possible arithmetic mutations such as '+' to '-', '*', '/', and '%'.";

        public override string Name => "Arithmetic Analyzer";

        public override string Id => "Arithmetic";
    }
}
