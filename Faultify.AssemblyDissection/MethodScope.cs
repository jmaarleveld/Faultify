using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Faultify.MutationCollector;
using Faultify.MutationCollector.Mutation;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using FieldDefinition = Mono.Cecil.FieldDefinition;
using MethodDefinition = Mono.Cecil.MethodDefinition;

namespace Faultify.AssemblyDissection
{
    /// <summary>
    ///     Contains all of the instructions and mutations within the scope of a method definition.
    /// </summary>
    public class MethodScope : IMutationProvider, IMemberScope
    {
       
        /// <summary>
        ///     Underlying Mono.Cecil TypeDefinition.
        /// </summary>
        public readonly MethodDefinition MethodDefinition;

        private string AssemblyName { get;  }    
        private string TypeName { get; }

        /// <summary>
        ///     Fields inside this method 
        /// </summary>
        private Dictionary<string, FieldDefinition> Fields { get; }

        public MethodScope(MethodDefinition methodDefinition, string assemblyName, string typeName)
        {
            MethodDefinition = methodDefinition;
            AssemblyName = assemblyName;
            TypeName = typeName;
            Fields = 
                MethodDefinition.
                Body.
                Instructions.
                OfType<FieldReference>().
                Select(x => x.Resolve())
                .ToDictionary(x => x.Name, x => x);
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
        ///     Every list of mutations contained in the main list
        ///     applies to the same opcode and was found by the same
        ///     analyzer.
        /// </summary>
        public IEnumerable<IEnumerable<IMutation>> AllMutations(
            MutationLevel mutationLevel, 
            HashSet<string> excludeGroup, 
            HashSet<string> excludeSingular)
        {
            if (MethodDefinition.Body == null)
            {
                return new List<IList<IMutation>>();
            }

            MethodDefinition.Body.SimplifyMacros();
            
            // Get all mutations in the method body 
            var methodMutations = GetMethodMutations(
                mutationLevel,
                excludeGroup,
                excludeSingular);
            // Get mutations in fields
            var fieldMutations = GetFieldMutations(
                mutationLevel, 
                excludeGroup, 
                excludeSingular);
            // Combine the results 
            return methodMutations.Concat(fieldMutations);
        }

        /// <summary>
        ///     Return all mutations which can be applied in the
        ///     method body.
        /// </summary>
        private IEnumerable<IEnumerable<IMutation>> GetMethodMutations(
            MutationLevel mutationLevel,
            HashSet<string> excludeGroup,
            HashSet<string> excludeSingular)
        {
            return MutationFactory.GetMethodMutations(
                    AssemblyName,
                    TypeName,
                    MethodDefinition, 
                    mutationLevel, 
                    excludeGroup, 
                    excludeSingular,
                    AssemblyQualifiedName);
        }

        /// <summary>
        ///     Return all mutations that can be applied to
        ///     fields in the method.
        /// </summary>
        private IEnumerable<IEnumerable<IMutation>> GetFieldMutations(
            MutationLevel mutationLevel, 
            HashSet<string> excludeGroup, 
            HashSet<string> excludeSingular)
        {
            List<IEnumerable<IEnumerable<IMutation>>> fieldMutationLists =
                new List<IEnumerable<IEnumerable<IMutation>>>();
            foreach (var field in Fields.Values) {
                var mutations = MutationFactory.GetFieldMutations(
                    AssemblyName,
                    TypeName,
                    MethodDefinition.Name,
                    field,
                    mutationLevel,
                    excludeGroup,
                    excludeSingular,
                    AssemblyQualifiedName);
                fieldMutationLists.Add(mutations);
            }
            // Flatten result 
            return fieldMutationLists.SelectMany(x => x);
        }

        public IMutation GetEquivalentMutation(IMutation original)
        {
            if (original.FieldName != null) {
                var field = Fields[original.FieldName];
                return original.GetEquivalentMutation(
                    field,
                    field.MetadataToken.ToInt32());
            }

            return original.GetEquivalentMutation(
                MethodDefinition,
                IntHandle);
        }
    }
}
