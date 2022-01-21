using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Faultify.MutationCollector;
using Faultify.MutationCollector.AssemblyAnalyzers;
using Faultify.AssemblyDissection;
using Faultify.MutationCollector.Mutation;
using Faultify.Tests.UnitTests.Utils;
using NUnit.Framework;

namespace Faultify.Tests.UnitTests
{
    /// <summary>
    /// Test the assembly mutator on a dummy assembly.
    /// </summary>
    public class AssemblyMutatorTests
    {
        private readonly string _folder = Path.Combine("UnitTests", "TestSource", "TestAssemblyTarget.cs");

        private readonly string _nameSpaceTestAssemblyTarget1 =
            "Faultify.Tests.UnitTests.TestSource.TestAssemblyTarget1";

        private readonly string _nameSpaceTestAssemblyTarget2 =
            "Faultify.Tests.UnitTests.TestSource.TestAssemblyTarget2";

        [SetUp]
        public void LoadTestAssembly()
        {
            byte[] binary = DllTestHelper.CompileTestBinary(_folder);
            File.WriteAllBytes("test.dll", binary);
        }

        [TearDown]
        public void RemoveTestAssembly()
        {
            File.Delete("test.dll");
            File.Delete("test.pdb");
        }

        [Test]
        public void AssemblyMutator_Has_Right_Types()
        {
            var mutator = new AssemblyAnalyzer("test.dll");

            Assert.AreEqual(mutator.Types.Count, 2);

            var typeScopeEnumerator = mutator.Types.Values.ToList();
            
            Assert.AreEqual(typeScopeEnumerator[0].AssemblyQualifiedName, _nameSpaceTestAssemblyTarget1);
            Assert.AreEqual(typeScopeEnumerator[1].AssemblyQualifiedName, _nameSpaceTestAssemblyTarget2);
        }

        [Test]
        public void AssemblyMutator_Type_TestAssemblyTarget1_Has_Right_Methods()
        {
            var mutator = new AssemblyAnalyzer("test.dll");
            TypeScope target1 = mutator.Types.Values.First(x =>
                x.AssemblyQualifiedName == _nameSpaceTestAssemblyTarget1);

            Assert.AreEqual(target1.Methods.Count, 3);
            Assert.IsNotNull(target1.Methods.FirstOrDefault(x => x.Value.Name == "TestMethod1"), null);
            Assert.IsNotNull(target1.Methods.FirstOrDefault(x => x.Value.Name == "TestMethod2"), null);
        }

        [Test]
        public void AssemblyMutator_Type_TestAssemblyTarget2_Has_Right_Methods()
        {
            using AssemblyAnalyzer mutator = new AssemblyAnalyzer("test.dll");
            TypeScope target1 = mutator.Types.Values.First(x =>
                x.AssemblyQualifiedName == _nameSpaceTestAssemblyTarget2);

            Assert.AreEqual(target1.Methods.Count, 4);
            Assert.IsNotNull(target1.Methods.FirstOrDefault(x => x.Value.Name == "TestMethod1"), null);
            Assert.IsNotNull(target1.Methods.FirstOrDefault(x => x.Value.Name == "TestMethod2"), null);
        }

        [Test]
        public void AssemblyMutator_Type_TestAssemblyTarget1_Has_Right_Fields()
        {
            using AssemblyAnalyzer mutator = new AssemblyAnalyzer("test.dll");
            TypeScope target1 = mutator.Types.Values.First(x =>
                x.AssemblyQualifiedName == _nameSpaceTestAssemblyTarget1);

            Assert.AreEqual(target1.Fields.Count, 2); // ctor, cctor, two target methods.
            Assert.IsNotNull(target1.Fields.FirstOrDefault(x => x.Value.Name == "Constant"), null);
            Assert.IsNotNull(target1.Fields.FirstOrDefault(x => x.Value.Name == "Static"), null);
        }

        [Test]
        public void AssemblyMutator_Type_TestAssemblyTarget2_Has_Right_Fields()
        {
            using AssemblyAnalyzer mutator = new AssemblyAnalyzer("test.dll");
            TypeScope target1 = mutator.Types.Values.First(x =>
                x.AssemblyQualifiedName == _nameSpaceTestAssemblyTarget2);

            Assert.AreEqual(target1.Fields.Count, 2); // ctor, cctor, two target methods.
            Assert.IsNotNull(target1.Fields.FirstOrDefault(x => x.Value.Name == "Constant"), null);
            Assert.IsNotNull(target1.Fields.FirstOrDefault(x => x.Value.Name == "Static"), null);
        }

        [Test]
        public void AssemblyMutator_Type_TestAssemblyTarget1_TestMethod1_Has_Right_Mutations()
        {
            using AssemblyAnalyzer mutator = new AssemblyAnalyzer("test.dll");
            TypeScope target1 = mutator.Types.Values.First(x =>
                x.AssemblyQualifiedName == _nameSpaceTestAssemblyTarget1);
            MethodScope method1 = target1.Methods.FirstOrDefault(x => x.Value.Name == "TestMethod1").Value;

            var list = method1.AllMutations(MutationLevel.Detailed, new HashSet<string>(), new HashSet<string>());

            List<List<IMutation>> mutations = 
                list.Where(x => x is IEnumerable<OpCodeMutation>).
                    Select(x => x.ToList()).ToList();

            var arithmeticMutations = 
                mutations.Where(x => x.First().AnalyzerName == new ArithmeticAnalyzer().Name).ToList();
            var comparisonMutations = 
                mutations.Where(x => x.First().AnalyzerName == new ComparisonAnalyzer().Name).ToList();
            
            // The Mutator should detect exactly 4 Arithmetic and 1 Comparison mutations, but no others
            // Furthermore, those mutations should be in the same IEnumerable
            
            // We must have exactly two groups 
            Assert.AreEqual(2, mutations.Count);
            
            // 1 group of arithmetic mutations, 1 group of comparison mutations 
            Assert.AreEqual(1, arithmeticMutations.Count);
            Assert.AreEqual(1, comparisonMutations.Count);
            
            // Verify group amounts  
            Assert.AreEqual(4, arithmeticMutations.First().Count);
            Assert.AreEqual(1, comparisonMutations.First().Count);

            // Verify that all mutations in the group are correct
            foreach (var arithmeticMutation in arithmeticMutations.First())
            {
                Assert.AreEqual(new ArithmeticAnalyzer().Name, arithmeticMutation.AnalyzerName);
            }

            foreach (var comparisonMutation in comparisonMutations.First()) {
                Assert.AreEqual(new ComparisonAnalyzer().Name, comparisonMutation.AnalyzerName);
            }
        }

        [Test]
        public void AssemblyMutator_Type_TestAssemblyTarget1_Constant_Has_Right_Mutation()
        {
            using AssemblyAnalyzer mutator = new AssemblyAnalyzer("test.dll");
            TypeScope target1 = mutator.Types.Values.First(x =>
                x.AssemblyQualifiedName == _nameSpaceTestAssemblyTarget1);
            FieldScope field = target1.Fields.FirstOrDefault(x => x.Value.Name == "Constant").Value;


            var mutations = field.AllMutations(MutationLevel.Detailed, new HashSet<string>(), new HashSet<string>());

            IEnumerable<IEnumerable<IMutation>> enumerable = mutations as IEnumerable<IMutation>[] ?? mutations.ToArray();
            IEnumerable<IMutation> arithmeticMutations =
                enumerable.FirstOrDefault(x => x.First().AnalyzerName == new ConstantAnalyzer().Name) ?? Array.Empty<IMutation>();

            Assert.AreEqual(enumerable.Count(), 1);
            Assert.IsNotNull(arithmeticMutations, null);
        }
    }
}
