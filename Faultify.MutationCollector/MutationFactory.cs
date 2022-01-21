using System.Collections.Generic;
using System.Linq;
using Faultify.MutationCollector.AssemblyAnalyzers;
using Faultify.MutationCollector.Mutation;
using Mono.Cecil;

namespace Faultify.MutationCollector {
    
    /// <summary>
    ///     This class acts as an intermediary for generating
    ///     mutations using analyzers, by conveniently grouping
    ///     all analyzers together and calling them in order.
    /// </summary>
    public static class MutationFactory {
        
        private static readonly IList<IAnalyzer<IMutation, FieldDefinition>> FieldAnalyzers =
            new List<IAnalyzer<IMutation, FieldDefinition>> {
                new ConstantAnalyzer(),
            };

        private static readonly IList<IAnalyzer<IMutation, MethodDefinition>> MethodAnalyzers =
            new List<IAnalyzer<IMutation, MethodDefinition>> {
                new ArithmeticAnalyzer(),
                new ArrayAnalyzer(),
                new BitwiseAnalyzer(),
                new ComparisonAnalyzer(),
                new VariableAnalyzer(),
            };

        /// <summary>
        ///     Get all mutations which can be applied to a field definition
        /// </summary>
        /// <param name="assemblyName">Name of the containing assembly</param>
        /// <param name="typeName">Name of the containing class</param>
        /// <param name="methodName">If relevant, the name of the containing method</param>
        /// <param name="field">Field definition being mutated</param>
        /// <param name="level">the mutation level</param>
        /// <param name="excludedAnalyzers">excluded analyzers</param>
        /// <param name="excludedCategories">excluded categories of mutations</param>
        /// <param name="memberName"></param>
        /// <param name="parentMethodEntityHandle"></param>
        /// <returns>
        ///     enumerable of enumerables, where every inner enumerable contains a
        ///     group of mutations found by the same analyzer which will be
        ///     applied to the same method.
        /// </returns>
        public static IEnumerable<IEnumerable<IMutation>> GetFieldMutations(
            string assemblyName,
            string typeName,
            string? methodName,
            FieldDefinition field,
            MutationLevel level,
            HashSet<string> excludedAnalyzers,
            HashSet<string> excludedCategories,
            string memberName,
            int? parentMethodEntityHandle)
        {
            IEnumerable<IEnumerable<IMutation> > mutationGroups =
                from analyzer in FieldAnalyzers 
                where !excludedAnalyzers.Contains(analyzer.Id) 
                select analyzer.GenerateMutations(
                    assemblyName, 
                    typeName, 
                    methodName,
                    field,
                    level, 
                    excludedCategories,
                    memberName,
                    parentMethodEntityHandle) 
                into mutations 
                where mutations.Any() 
                select mutations;
            return mutationGroups;
        }

        /// <summary>
        ///     Get all mutations which can be applied to a given method.
        /// </summary>
        /// <param name="assemblyName">Name of the containing assembly</param>
        /// <param name="typeName">Name of the containing class</param>
        /// <param name="method">Method definition to be mutated</param>
        /// <param name="level">the mutation leven</param>
        /// <param name="excludedAnalyzers">excluded analyzers</param>
        /// <param name="excludedCategories">excluded categories of mutations</param>
        /// <param name="memberName"></param>
        /// <param name="parentMethodEntityHandle"></param>
        /// <returns>
        ///     enumerable of enumerables, where every inner enumerable contains a
        ///     group of mutations found by the same analyzer which will be
        ///     applied to the same method.
        /// </returns>
        public static IEnumerable<IEnumerable<IMutation>> GetMethodMutations(
            string assemblyName,
            string typeName,
            MethodDefinition method,
            MutationLevel level,
            HashSet<string> excludedAnalyzers,
            HashSet<string> excludedCategories,
            string memberName,
            int? parentMethodEntityHandle)
        {
            IEnumerable<IEnumerable<IMutation> > mutationGroups =
                from 
                analyzer in MethodAnalyzers 
                where !excludedAnalyzers.Contains(analyzer.Id) 
                select analyzer.GenerateMutations(
                    assemblyName, 
                    typeName, 
                    method.Name,
                    method, 
                    level, 
                    excludedCategories,
                    memberName,
                    parentMethodEntityHandle) 
                into mutations 
                where mutations.Any() 
                select mutations;
            return mutationGroups;
        }
    }
}