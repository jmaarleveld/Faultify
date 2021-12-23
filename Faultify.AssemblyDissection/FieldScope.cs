using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Faultify.MutationCollector;
using Faultify.MutationCollector.Mutation;
using FieldDefinition = Mono.Cecil.FieldDefinition;

namespace Faultify.AssemblyDissection
{
    /// <summary>
    ///     Represents a raw field definition.
    /// </summary>
    public class FieldScope : IMutationProvider, IMemberScope
    {

        /// <summary>
        ///     Underlying Mono.Cecil FieldDefinition.
        /// </summary>
        private readonly FieldDefinition _fieldDefinition;

        public FieldScope(FieldDefinition fieldDefinition)
        {
            _fieldDefinition = fieldDefinition;
        }

        public string AssemblyQualifiedName => _fieldDefinition.FullName;
        public string Name => _fieldDefinition.Name;
        public EntityHandle Handle => MetadataTokens.EntityHandle(_fieldDefinition.MetadataToken.ToInt32());

        public IEnumerable<IMutation> AllMutations(
            MutationLevel mutationLevel,
            HashSet<string> excludeGroup,
            HashSet<string> excludeSingular)
        {
            return MutationFactory.GetFieldMutations(
                _fieldDefinition,
                mutationLevel, 
                excludeGroup, 
                excludeSingular);
        }
    }
}
