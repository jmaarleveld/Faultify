﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Faultify.Analyze.Analyzers;
using Faultify.Analyze.Mutation;
using Faultify.Analyze.MutationGroups;
using FieldDefinition = Mono.Cecil.FieldDefinition;

namespace Faultify.Analyze.AssemblyMutator
{
    /// <summary>
    ///     Represents a raw field definition.
    /// </summary>
    public class FieldScope : IMutationProvider, IMemberScope
    {
        private readonly HashSet<IAnalyzer<ConstantMutation, FieldDefinition>> _fieldAnalyzers;

        /// <summary>
        ///     Underlying Mono.Cecil FieldDefinition.
        /// </summary>
        private readonly FieldDefinition _fieldDefinition;

        public FieldScope(FieldDefinition fieldDefinition)
        {
            _fieldDefinition = fieldDefinition;
            _fieldAnalyzers = new HashSet<IAnalyzer<ConstantMutation, FieldDefinition>>
            {
                new ConstantAnalyzer(),
            };
        }

        public string AssemblyQualifiedName => _fieldDefinition.FullName;
        public string Name => _fieldDefinition.Name;
        public EntityHandle Handle => MetadataTokens.EntityHandle(_fieldDefinition.MetadataToken.ToInt32());

        public IEnumerable<IMutationGroup<IMutation>> AllMutations(MutationLevel mutationLevel)
        {
            return ConstantFieldMutations(mutationLevel);
        }

        /// <summary>
        ///     Returns possible constant field mutations.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<IMutationGroup<ConstantMutation>> ConstantFieldMutations(MutationLevel mutationLevel)
        {
            foreach (IAnalyzer<ConstantMutation, FieldDefinition> analyzer in _fieldAnalyzers)
            {
                IMutationGroup<ConstantMutation>
                    mutations = analyzer.GenerateMutations(_fieldDefinition, mutationLevel);

                if (mutations.Any())
                {
                    yield return mutations;
                }
            }
        }
    }
}