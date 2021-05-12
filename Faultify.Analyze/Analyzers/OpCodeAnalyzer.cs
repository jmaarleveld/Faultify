﻿using System;
using System.Collections.Generic;
using System.Linq;
using Faultify.Analyze.Mutation;
using Faultify.Analyze.MutationGroups;
using Mono.Cecil;
using Mono.Cecil.Cil;
using NLog;

namespace Faultify.Analyze.Analyzers
{
    /// <summary>
    ///     Analyzer that searches for possible opcode mutations inside a method definition.
    ///     A list with opcodes definitions can be found here: https://en.wikipedia.org/wiki/List_of_CIL_instructions
    /// </summary>
    public abstract class OpCodeAnalyzer : IAnalyzer<OpCodeMutation, MethodDefinition>
    {
<<<<<<< HEAD
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly Dictionary<OpCode, IEnumerable<(MutationLevel, OpCode, string)>> _mappedOpCodes;
=======
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly Dictionary<OpCode, IEnumerable<(MutationLevel, OpCode)>> _mappedOpCodes;
>>>>>>> parent of 2f950df... Revert "Removed DI Logging in favour of static logging as sole solution"

        protected OpCodeAnalyzer(Dictionary<OpCode, IEnumerable<(MutationLevel, OpCode, string)>> mappedOpCodes)
        {
            _mappedOpCodes = mappedOpCodes;
        }

        public abstract string Description { get; }
        public abstract string Name { get; }
        public abstract string Id { get; }

        public IMutationGroup<OpCodeMutation> GenerateMutations(
            MethodDefinition scope,
            MutationLevel mutationLevel,
            HashSet<string> exclusions,
            IDictionary<Instruction, SequencePoint> debug = null
        )
        {
            var mutationGroup = new List<IEnumerable<OpCodeMutation>>();

            foreach (Instruction instruction in scope.Body?.Instructions)
            {
                OpCode original = instruction.OpCode;
                IEnumerable<OpCodeMutation> mutations;

                try
                {
                    IEnumerable<(MutationLevel, OpCode, string)> targets = _mappedOpCodes[original];
                    mutations =
                        from target
                            in targets
                        where mutationLevel.HasFlag(target.Item1) && !(exclusions.Contains(target.Item3))
                        select new OpCodeMutation(
                            instruction.OpCode,
                            target.Item2,
                            instruction,
                            scope);
                }
                catch (Exception e)
                {
                    _logger.Debug(e, $"Could not find key \"{original}\" in Dictionary.");
                    mutations = Enumerable.Empty<OpCodeMutation>();
                }
<<<<<<< HEAD
                mutationGroup.Add(mutations);
=======
            }

            try
            {
                IEnumerable<(MutationLevel, OpCode)> targets = _mappedOpCodes[original];
                mutations =
                    from target
                        in targets
                    where mutationLevel.HasFlag(target.Item1)
                    select new OpCodeMutation(
                        scope.OpCode,
                        target.Item2,
                        scope,
                        lineNumber);
            }
            catch (Exception e)
            {
                Logger.Debug(e, $"Could not find key \"{original}\" in Dictionary.");
                mutations = Enumerable.Empty<OpCodeMutation>();
>>>>>>> parent of 2f950df... Revert "Removed DI Logging in favour of static logging as sole solution"
            }

            return new MutationGroup<OpCodeMutation>
            {
                Name = Name,
                Description = Description,
                Mutations = mutationGroup.SelectMany(x => x),
            };
        }
    }
}
