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
        private string TypeName { get;  }

        /// <summary>
        ///     Construct a new field scope 
        /// </summary>
        /// <param name="fieldDefinition">Underlying field definition</param>
        /// <param name="assemblyName">Name of the containing assembly</param>
        /// <param name="typeName">Name of the containing type</param>
        public FieldScope(
            FieldDefinition fieldDefinition,
            string assemblyName,
            string typeName)
        {
            _fieldDefinition = fieldDefinition;
            AssemblyName = assemblyName;
            TypeName = typeName;
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
                TypeName,
                null,
                _fieldDefinition,
                mutationLevel, 
                excludeGroup, 
                excludeSingular);
        }

        public IMutation GetEquivalentMutation(IMutation original)
        {
            return original.GetEquivalentMutation(
                _fieldDefinition, 
                _fieldDefinition.MetadataToken.ToInt32());
        }
    }
}
