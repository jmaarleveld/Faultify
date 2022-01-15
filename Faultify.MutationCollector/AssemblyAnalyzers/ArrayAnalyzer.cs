using System;
using System.Collections.Generic;
using System.Linq;
using Faultify.MutationCollector.Extensions;
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
            string assemblyName,
            string typeName,
            string? methodName,
            MethodDefinition method,
            MutationLevel mutationLevel,
            HashSet<string> exclusions,
            IDictionary<Instruction, SequencePoint>? debug = null
        )
        {
            if (methodName == null) {
                throw new NullReferenceException(
                    "ArrayAnalyzer expects a non-null method name");
            }
            // Filter and map arrays
            var entityHandle = method.MetadataToken.ToInt32();
            IEnumerable<ArrayMutation> arrayMutations =
                from instruction
                    in method.Body.Instructions
                where instruction.IsDynamicArray() && isArrayType(instruction)
                select new ArrayMutation(
                    new DynamicArrayRandomizerStrategy(method), 
                    method, 
                    Name, 
                    Description,
                    assemblyName,
                    entityHandle,
                    typeName,
                    methodName);

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
