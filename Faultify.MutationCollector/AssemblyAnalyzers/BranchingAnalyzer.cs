using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace Faultify.MutationCollector.AssemblyAnalyzers
{
    /// <summary>
    ///     Analyzer that searches for possible boolean branching mutations inside a method definition.
    ///     Mutations such as 'if(condition)' to 'if(!condition)'.
    /// </summary>
    public class BranchingAnalyzer : OpCodeAnalyzer
    {
        private static readonly Dictionary<OpCode, IEnumerable<(MutationLevel, OpCode, string)>> Bitwise =
            new Dictionary<OpCode, IEnumerable<(MutationLevel, OpCode, string)>>
            {
                // Opcodes for mutating 'if(condition)' to 'if(!condition)' or unconditional conditions.
                { OpCodes.Brtrue, new[] { (MutationLevel.Simple, OpCodes.Brfalse, "trueToFalse") } },
                { OpCodes.Brtrue_S, new[] { (MutationLevel.Simple, OpCodes.Brfalse_S, "trueToFalseS") } },

                // Opcodes for mutating 'if(!condition)' to 'if(condition)' or unconditional conditions.
                { OpCodes.Brfalse, new[] { (MutationLevel.Simple, OpCodes.Brtrue, "falseToTrue") } },
                { OpCodes.Brfalse_S, new[] { (MutationLevel.Simple, OpCodes.Brtrue_S, "falseToTrueS") } },
            };

        public BranchingAnalyzer() : base(Bitwise) { }

        public override string Description =>
            "Analyzer that searches for possible boolean branch mutations such as such as 'if(condition)' to 'if(!condition).";

        public override string Name => "Boolean Branch Analyzer";

        public override string Id => "Branching";
    }
}
