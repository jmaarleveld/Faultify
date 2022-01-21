using System;
using System.Collections.Generic;
using System.Linq;
using Faultify.MutationCollector.Mutation;
using Mono.Cecil;
using Mono.Cecil.Cil;
using NLog;

namespace Faultify.MutationCollector.AssemblyAnalyzers
{
    /// <summary>
    ///     Analyzer that searches for possible opcode mutations inside a method definition.
    ///     A list with opcodes definitions can be found here:
    ///     https://en.wikipedia.org/wiki/List_of_CIL_instructions
    /// </summary>
    public abstract class OpCodeAnalyzer : IAnalyzer<OpCodeMutation, MethodDefinition>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly Dictionary<OpCode, IEnumerable<(MutationLevel, OpCode, string)>> _mappedOpCodes;

        protected OpCodeAnalyzer(Dictionary<OpCode, IEnumerable<(MutationLevel, OpCode, string)>> mappedOpCodes)
        {
            _mappedOpCodes = mappedOpCodes;
        }

        public abstract string Description { get; }
        public abstract string Name { get; }
        public abstract string Id { get; }

        public IEnumerable<OpCodeMutation> GenerateMutations(
            string assemblyName,
            string typeName,
            string? methodName,
            MethodDefinition scope,
            MutationLevel mutationLevel,
            HashSet<string> exclusions,
            string memberName,
            int? parentMethodEntityHandle,
            IDictionary<Instruction, SequencePoint>? debug = null
        )
        {
            if (methodName == null) {
                throw new NullReferenceException(
                    "OpcodeAnalyzer expects a non-null method name");
            }
            var mutationGroup = new List<IEnumerable<OpCodeMutation>>();
            int index = 0;
            foreach (Instruction instruction in scope.Body.Instructions)
            {
                OpCode original = instruction.OpCode;
                IEnumerable<OpCodeMutation> mutations = Enumerable.Empty<OpCodeMutation>();

                if (_mappedOpCodes.ContainsKey(original)) {
                    int instructionIndex = index;
                    var entityHandle = scope.MetadataToken.ToInt32();
                    IEnumerable<(MutationLevel, OpCode, string)> targets = _mappedOpCodes[original];
                    mutations =
                        from target in targets
                        where mutationLevel.HasFlag(target.Item1) && !(exclusions.Contains(target.Item3))
                        select new OpCodeMutation(
                            instruction.OpCode,
                            target.Item2,
                            instruction,
                            instructionIndex,
                            scope,
                            Name,
                            Description,
                            assemblyName,
                            entityHandle,
                            typeName,
                            methodName,
                            memberName,
                            parentMethodEntityHandle);
                    
                    mutationGroup.Add(mutations);
                }

                index++;

            }

            return mutationGroup.SelectMany(x => x);
        }
    }
}
