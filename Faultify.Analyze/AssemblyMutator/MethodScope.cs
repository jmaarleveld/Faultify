﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Faultify.Analyze.Analyzers;
using Faultify.Analyze.Mutation;
using Faultify.Analyze.MutationGroups;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using FieldDefinition = Mono.Cecil.FieldDefinition;
using MethodDefinition = Mono.Cecil.MethodDefinition;

namespace Faultify.Analyze.AssemblyMutator
{
    /// <summary>
    ///     Contains all of the instructions and mutations within the scope of a method definition.
    /// </summary>
    public class MethodScope : IMutationProvider, IMemberScope
    {
        private readonly HashSet<IAnalyzer<ArrayMutation, MethodDefinition>> _arrayMutationAnalyzers =
            new HashSet<IAnalyzer<ArrayMutation, MethodDefinition>>
            {
                new ArrayAnalyzer(),
            };

        private readonly HashSet<IAnalyzer<ConstantMutation, FieldDefinition>> _constantReferenceMutationAnalyers =
            new HashSet<IAnalyzer<ConstantMutation, FieldDefinition>>
            {
                new ConstantAnalyzer(),
            };

        private readonly HashSet<IAnalyzer<OpCodeMutation, Instruction>> _opCodeMethodAnalyzers =
            new HashSet<IAnalyzer<OpCodeMutation, Instruction>>
            {
                new ArithmeticAnalyzer(),
                new ComparisonAnalyzer(),
                new BitwiseAnalyzer(),
            };

        private readonly HashSet<IAnalyzer<VariableMutation, MethodDefinition>> _variableMutationAnalyzers =
            new HashSet<IAnalyzer<VariableMutation, MethodDefinition>>
            {
                new VariableAnalyzer(),
            };


        /// <summary>
        ///     Underlying Mono.Cecil TypeDefinition.
        /// </summary>
        public readonly MethodDefinition MethodDefinition;

        public MethodScope(MethodDefinition methodDefinition)
        {
            MethodDefinition = methodDefinition;
        }

        public int IntHandle => MethodDefinition.MetadataToken.ToInt32();

        /// <summary>
        ///     Full assembly name of this method.
        /// </summary>
        public string AssemblyQualifiedName => MethodDefinition.FullName;

        public string Name => MethodDefinition.Name;

        public EntityHandle Handle => MetadataTokens.EntityHandle(IntHandle);

        /// <summary>
        ///     Returns all available mutations within the scope of this method.
        /// </summary>
        public IEnumerable<IMutationGroup<IMutation>> AllMutations(MutationLevel mutationLevel, List<string> excludeGroup, List<string> excludeSingular)
        {
            if (MethodDefinition.Body == null)
            {
                return Enumerable.Empty<IMutationGroup<IMutation>>();
            }

            MethodDefinition.Body.SimplifyMacros();

            IEnumerable<IMutationGroup<IMutation>> opcodeMutations = OpCodeMutations(mutationLevel, excludeGroup, excludeSingular);
            IEnumerable<IMutationGroup<IMutation>> constantMutations = ConstantReferenceMutations(mutationLevel, excludeGroup);
            IEnumerable<IMutationGroup<IMutation>> variableMutations = VariableMutations(mutationLevel, excludeGroup);
            IEnumerable<IMutationGroup<IMutation>> arrayMutations = ArrayMutations(mutationLevel, excludeGroup);

            return opcodeMutations
                .Concat(constantMutations) // TODO: Why was this not used in the original?
                .Concat(variableMutations)
                .Concat(arrayMutations);
        }

        /// <summary>
        ///     Returns all operator mutations within the scope of this method.
        /// </summary>
        private IEnumerable<IMutationGroup<OpCodeMutation>> OpCodeMutations(MutationLevel mutationLevel, List<string> excludeGroup, List<string> excludeSingular)
        {
            foreach (IAnalyzer<OpCodeMutation, Instruction> analyzer in _opCodeMethodAnalyzers)
            {
                if (!excludeGroup.Contains(analyzer.Id))
                {
                    if (MethodDefinition.Body?.Instructions != null)
                    {
                        foreach (Instruction instruction in MethodDefinition.Body?.Instructions)
                        {
                            IMutationGroup<OpCodeMutation> mutations = analyzer.GenerateMutations(instruction,
                                mutationLevel, excludeSingular, MethodDefinition.DebugInformation.GetSequencePointMapping());

                            if (mutations.Any())
                            {
                                yield return mutations;
                            }
                        }
                    }
                } 
            }
        }

        /// <summary>
        ///     Returns all literal value mutations within the scope of this method.
        /// </summary>
        private IEnumerable<IMutationGroup<ConstantMutation>> ConstantReferenceMutations(MutationLevel mutationLevel, List<string> excludeGroup)
        {
            IEnumerable<FieldReference> fieldReferences = MethodDefinition.Body.Instructions
                .OfType<FieldReference>();

            foreach (FieldReference field in fieldReferences)
            foreach (IAnalyzer<ConstantMutation, FieldDefinition> analyzer in _constantReferenceMutationAnalyers)
            {
                if (excludeGroup.Contains(analyzer.Id))
                {
                    yield break;
                }
                IMutationGroup<ConstantMutation> mutations = analyzer.GenerateMutations(field.Resolve(), mutationLevel, new List<string>());

                if (mutations.Any())
                {
                    yield return mutations;
                }
            }
        }

        /// <summary>
        ///     Returns all variable mutations within the scope of this method.
        /// </summary>
        private IEnumerable<IMutationGroup<VariableMutation>> VariableMutations(MutationLevel mutationLevel, List<string> excludeGroup)
        {
            return
                from analyzer
                    in _variableMutationAnalyzers 
                    where!(excludeGroup.Contains(analyzer.Id))
                select  analyzer.GenerateMutations(MethodDefinition, mutationLevel, new List<string>());
        }

        /// <summary>
        ///     Returns all array mutations within the scope of this method.
        /// </summary>
        private IEnumerable<IMutationGroup<ArrayMutation>> ArrayMutations(MutationLevel mutationLevel, List<string> excludeGroup)
        {
            return
                from analyzer
                    in _arrayMutationAnalyzers
                where !(excludeGroup.Contains(analyzer.Id))
                select analyzer.GenerateMutations(MethodDefinition, mutationLevel, new List<string>());
        }
    }
}
