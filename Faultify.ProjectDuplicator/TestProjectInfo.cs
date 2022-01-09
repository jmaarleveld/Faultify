extern alias MC;
using System;
using System.Collections.Generic;
using Faultify.AssemblyDissection;
using Mono.Cecil;
using Faultify.TestHostRunner;

namespace Faultify.ProjectDuplicator
{
    public class TestProjectInfo : IDisposable
    {
        public TestProjectInfo(TestHost testFramework, ModuleDefinition 
        testModule)
        {
            TestFramework = testFramework;
            TestModule = testModule;
        }
        public TestHost TestFramework { get; set; }
        public ModuleDefinition TestModule { get; set; }
        public List<AssemblyMutator> DependencyAssemblies { get; set; } = new List<AssemblyMutator>();

        /// <summary>
        /// <inheritdoc cref="IDisposable"/>
        /// </summary>
        public void Dispose()
        {
            foreach (var assemblyMutator in DependencyAssemblies) assemblyMutator.Dispose();
            DependencyAssemblies.Clear();
            TestModule!.Dispose();
        }
    }
}
