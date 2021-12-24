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
        public IEnumerable<IMutation> AllMutations(
            MutationLevel mutationLevel, 
            HashSet<string> excludeGroup, 
            HashSet<string> excludeSingular)
        {
            if (MethodDefinition.Body == null)
            {
                return Enumerable.Empty<IMutation>();
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
        private IEnumerable<IMutation> GetMethodMutations(
            MutationLevel mutationLevel,
            HashSet<string> excludeGroup,
            HashSet<string> excludeSingular)
        {
            return MutationFactory.GetMethodMutations(
                    MethodDefinition, 
                    mutationLevel, 
                    excludeGroup, 
                    excludeSingular);
        }

        /// <summary>
        ///     Return all mutations that can be applied to
        ///     fields in the method.
        /// </summary>
        private IEnumerable<IMutation> GetFieldMutations(
            MutationLevel mutationLevel, 
            HashSet<string> excludeGroup, 
            HashSet<string> excludeSingular)
        {
            IEnumerable<FieldReference> fieldReferences = MethodDefinition.Body.Instructions
                .OfType<FieldReference>();
            List<IEnumerable<IMutation>> fieldMutationLists = new List<IEnumerable<IMutation>>();
            foreach (FieldReference field in fieldReferences) {
                var mutations = MutationFactory.GetFieldMutations(
                    field.Resolve(),
                    mutationLevel,
                    excludeGroup,
                    excludeSingular);
                fieldMutationLists.Add(mutations);
            }
            // Flatten result 
            return fieldMutationLists.SelectMany(x => x);
        }
    }
}
