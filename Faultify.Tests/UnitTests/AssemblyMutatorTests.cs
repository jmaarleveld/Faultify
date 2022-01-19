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

            using var typeScopeEnumerator = mutator.Types.Values.GetEnumerator();
            
            Assert.AreEqual(typeScopeEnumerator.Current.AssemblyQualifiedName, _nameSpaceTestAssemblyTarget1);
            typeScopeEnumerator.MoveNext();
            Assert.AreEqual(typeScopeEnumerator.Current.AssemblyQualifiedName, _nameSpaceTestAssemblyTarget2);
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

            List<IEnumerable<IMutation>> mutations = list.Where(x => x is IEnumerable<OpCodeMutation>).ToList();

            // The Mutator should detect Arithmetic and Comparison mutations, but no bitwise mutations.

            IEnumerable<IMutation> arithmeticMutations =
                mutations.FirstOrDefault(x => x.First().AnalyzerName == new ArithmeticAnalyzer().Name) ?? Array.Empty<IMutation>();
            IEnumerable<IMutation> comparisonMutations =
                mutations.FirstOrDefault(x => x.First().AnalyzerName == new ComparisonAnalyzer().Name) ?? Array.Empty<IMutation>();
            IEnumerable<IMutation> bitWiseMutations =
                mutations.FirstOrDefault(x => x.First().AnalyzerName == new BitwiseAnalyzer().Name) ?? Array.Empty<IMutation>();

            Assert.AreEqual(mutations.Count, 3);
            Assert.IsNotNull(arithmeticMutations, null);
            Assert.IsNotNull(comparisonMutations, null);

            Assert.IsNotEmpty(arithmeticMutations);
            Assert.IsNotEmpty(comparisonMutations);
            Assert.IsEmpty(bitWiseMutations);
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
