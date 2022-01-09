using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Faultify.MutationCollector.Mutation;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Faultify.MutationCollector.AssemblyAnalyzers
{
    /// <summary>
    ///     Analyzer that searches for possible constant mutations inside a type definition, this class is the parent class to
    ///     all constant analyzers.
    /// </summary>
    public class ConstantAnalyzer : IAnalyzer<ConstantMutation, FieldDefinition>
    {

        public string Description =>
            "Analyzer that searches for possible literaal constant mutations such as 'true' to 'false', or '7' to '42'.";

        public string Name => "Boolean ConstantMutation Analyzer";

        public string Id => "Constant";

        public IEnumerable<ConstantMutation> GenerateMutations(
            string assemblyName,
            FieldDefinition field,
            MutationLevel mutationLevel,
            HashSet<string> exclusions,
            IDictionary<Instruction, SequencePoint> debug = null
        )
        {
            // Make a new mutation list
            List<ConstantMutation> mutations = new List<ConstantMutation>();

            // If the type is valid, create a mutation and add it to the list
            Type type = field.Constant.GetType();

            if (TypeChecker.IsConstantType(type)) 
            {
                mutations.Add(new ConstantMutation(
                    field, 
                    type, 
                    Name, 
                    Description, 
                    assemblyName, 
                    null));
            }

            return mutations;
        }
    }
}
