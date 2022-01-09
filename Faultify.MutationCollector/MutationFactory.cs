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
    public class MutationFactory {
        
        private static readonly IList<IAnalyzer<IMutation, FieldDefinition>> FieldAnalyzers =
            new List<IAnalyzer<IMutation, FieldDefinition>> {
                new ConstantAnalyzer()
            };

        private static readonly IList<IAnalyzer<IMutation, MethodDefinition>> MethodAnalyzers =
            new List<IAnalyzer<IMutation, MethodDefinition>> {
                new ArithmeticAnalyzer(),
                new ArrayAnalyzer(),
                new BitwiseAnalyzer(),
                new ComparisonAnalyzer(),
                new VariableAnalyzer()
            };

        public static IEnumerable<IEnumerable<IMutation>> GetFieldMutations(
            string assemblyName,
            FieldDefinition field,
            MutationLevel level,
            HashSet<string> excludedAnalyzers,
            HashSet<string> excludedCategories) {
            IEnumerable<IEnumerable<IMutation> > mutationGroups =
                from analyzer in FieldAnalyzers 
                where !excludedAnalyzers.Contains(analyzer.Id) 
                select analyzer.GenerateMutations(assemblyName, field, level, excludedCategories) 
                into mutations 
                where mutations.Any() 
                select mutations;
            return mutationGroups;
        }

        public static IEnumerable<IEnumerable<IMutation>> GetMethodMutations(
            string assemblyName,
            MethodDefinition method,
            MutationLevel level,
            HashSet<string> excludedAnalyzers,
            HashSet<string> excludedCategories) {
            IEnumerable<IEnumerable<IMutation> > mutationGroups =
                from 
                analyzer in MethodAnalyzers 
                where !excludedAnalyzers.Contains(analyzer.Id) 
                select analyzer.GenerateMutations(assemblyName, method, level, excludedCategories) 
                into mutations 
                where mutations.Any() 
                select mutations;
            return mutationGroups;
        }
    }
}