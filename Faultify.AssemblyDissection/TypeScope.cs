using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Faultify.MutationCollector;
using Faultify.MutationCollector.Mutation;
using TypeDefinition = Mono.Cecil.TypeDefinition;

namespace Faultify.AssemblyDissection
{
    /// <summary>
    ///     Represents a raw type definition and provides access to its fields and methods..
    /// </summary>
    public class TypeScope : IMemberScope, IMutationProvider
    {

        public TypeScope(TypeDefinition typeDefinition, string assemblyName)
        {
            TypeDefinition = typeDefinition;
            AssemblyName = assemblyName;

            Fields = TypeDefinition.Fields.Select(x =>
                    new FieldScope(x, assemblyName)
                )
                .ToList();

            Methods = TypeDefinition.Methods.Select(x =>
                    new MethodScope(x, assemblyName)
                )
                .ToList();
        }
        
        private string AssemblyName { get;  }

        /// <summary>
        ///     The fields in this type.
        ///     For example: const, static, non-static fields.
        /// </summary>
        public List<FieldScope> Fields { get; }

        /// <summary>
        ///     The methods in this type.
        /// </summary>
        public List<MethodScope> Methods { get; }

        public TypeDefinition TypeDefinition { get; }
        public string Name => TypeDefinition.Name;
        public EntityHandle Handle => MetadataTokens.EntityHandle(TypeDefinition.MetadataToken.ToInt32());
        public string AssemblyQualifiedName => TypeDefinition.FullName;

        public IEnumerable<IEnumerable<IMutation>> AllMutations(
            MutationLevel mutationLevel, 
            HashSet<string> excludeGroup,
            HashSet<string> excludeSingular)
        {
            var mutations =
                from field in TypeDefinition.Fields
                select MutationFactory.GetFieldMutations(
                    AssemblyName,
                    field, 
                    mutationLevel, 
                    excludeGroup, 
                    excludeSingular);
            return mutations.SelectMany(x => x);
        }
    }
}
