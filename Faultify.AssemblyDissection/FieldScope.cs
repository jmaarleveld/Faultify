using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        
        private string AssemblyName { get;  }

        public FieldScope(FieldDefinition fieldDefinition, string assemblyName)
        {
            _fieldDefinition = fieldDefinition;
            AssemblyName = assemblyName;
        }

        public string AssemblyQualifiedName => _fieldDefinition.FullName;
        public string Name => _fieldDefinition.Name;
        public EntityHandle Handle => MetadataTokens.EntityHandle(_fieldDefinition.MetadataToken.ToInt32());

        public IEnumerable<IEnumerable<IMutation>> AllMutations(
            MutationLevel mutationLevel,
            HashSet<string> excludeGroup,
            HashSet<string> excludeSingular)
        {
            return MutationFactory.GetFieldMutations(
                AssemblyName,
                _fieldDefinition,
                mutationLevel, 
                excludeGroup, 
                excludeSingular);
        }
    }
}
