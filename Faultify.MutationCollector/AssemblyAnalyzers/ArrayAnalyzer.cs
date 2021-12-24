using System;
using System.Collections.Generic;
using System.Linq;
using Faultify.Core.Extensions;
using Faultify.MutationCollector.ArrayMutationStrategy;
using Faultify.MutationCollector.Mutation;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Faultify.MutationCollector.AssemblyAnalyzers
{
    /// <summary>
    ///     Analyzer that searches for possible array mutations inside a method definition.
    ///     Limitations:
    ///     - Only one-dimensional arrays.
    ///     - Only arrays that are created dynamically with more than 2 values which are not default values.
    ///     - Only array of the following types: double, float, long, ulong, int, uint, byte, sbyte, short, ushort, char,
    ///     boolean.
    /// </summary>
    public class ArrayAnalyzer : IAnalyzer<ArrayMutation, MethodDefinition>
    {
        public string Description => "Analyzer that searches for possible array mutations.";

        public string Name => "Array Analyzer";

        public string Id => "Array";

        public IEnumerable<ArrayMutation> GenerateMutations(
            MethodDefinition method,
            MutationLevel mutationLevel,
            HashSet<string> exclusions,
            IDictionary<Instruction, SequencePoint> debug = null
        )
        {
            // Filter and map arrays
            IEnumerable<ArrayMutation> arrayMutations =
                from instruction
                    in method.Body.Instructions
                where instruction.IsDynamicArray() && isArrayType(instruction)
                select new ArrayMutation(
                    new DynamicArrayRandomizerStrategy(method), 
                    method, 
                    Name, 
                    Description);

            return arrayMutations;
        }

        private bool isArrayType(Instruction newarr)
        {
            // Cast generic operand into its system type
            Type type = ((TypeReference) newarr.Operand).ToSystemType();
            return TypeChecker.IsArrayType(type);
        }
    }
}
