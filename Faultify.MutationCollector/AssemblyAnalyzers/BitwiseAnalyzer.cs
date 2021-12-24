using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace Faultify.MutationCollector.AssemblyAnalyzers
{
    /// <summary>
    ///     Analyzer that searches for possible bitwise mutations inside a method definition.
    ///     Mutations such as such as 'and' and 'xor'.
    ///
    ///     Note that this class also mutates logical operators such as || and &&.
    ///     This is because the logical operators are compiled to bitwise operations
    ///     in IL-code.
    /// </summary>
    public class BitwiseAnalyzer : OpCodeAnalyzer
    {
        private static readonly Dictionary<OpCode, IEnumerable<(MutationLevel, OpCode, string)>> Bitwise =
            new Dictionary<OpCode, IEnumerable<(MutationLevel, OpCode, string)>>
            {
                // Opcodes for mutation bitwise operator: '|' to '&' , and '^'. 
                {
                    OpCodes.Or, new[]
                    {
                        (MutationLevel.Simple, OpCodes.And, "orToAnd"),
                        (MutationLevel.Medium, OpCodes.Xor, "orToXor"),
                    }
                },

                // Opcodes for mutation bitwise operator: '&' to '|' , and '^'. 
                {
                    OpCodes.And, new[]
                    {
                        (MutationLevel.Simple, OpCodes.Or, "andToOr"),
                        (MutationLevel.Medium, OpCodes.Xor, "andToXor"),
                    }
                },

                // Opcodes for mutation bitwise operator: '^' to '|' , and '&'. 
                {
                    OpCodes.Xor, new[]
                    {
                        (MutationLevel.Simple, OpCodes.Or, "xorToOr"),
                        (MutationLevel.Medium, OpCodes.And, "xorToAnd"),
                    }
                },
            };

        public BitwiseAnalyzer() : base(Bitwise) { }

        public override string Description =>
            "Analyzer that searches for possible bitwise mutations such as 'or' to 'and' and 'xor'.";

        public override string Name => "Bitwise Analyzer";

        public override string Id => "Bitwise";
    }
}
