using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using NLog;
using Faultify.TestHostRunner.Results;

namespace Faultify.CoverageCollector
{
    /// <summary>
    ///     Injects coverage code into an assembly.
    /// </summary>
    public class TestCoverageInjector
    {
        private static readonly Lazy<TestCoverageInjector> _instance = new Lazy<TestCoverageInjector>(() => new TestCoverageInjector());

        private readonly string _currentAssemblyPath = typeof(TestCoverageInjector).Assembly.Location;
        private readonly MethodDefinition _initializeMethodDefinition;
        private readonly MethodDefinition _registerTargetCoverage;
        private readonly MethodDefinition _registerTestCoverage;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        ///     Initialise the test coverage injector
        /// </summary>
        private TestCoverageInjector()
        {
            // Retrieve the method definition for the register functions. 
            string registerTargetCoverage = nameof(CoverageRegistry.RegisterTargetCoverage);
            string registerTestCoverage = nameof(CoverageRegistry.RegisterTestCoverage);
            string initializeMethodName = nameof(CoverageRegistry.Initialize);

            using ModuleDefinition injectionAssembly = ModuleDefinition.ReadModule(_currentAssemblyPath);
            _registerTargetCoverage = getType(registerTargetCoverage, injectionAssembly);
            _registerTestCoverage = getType(registerTestCoverage, injectionAssembly);
            _initializeMethodDefinition = getType(initializeMethodName, injectionAssembly);

            if (_initializeMethodDefinition == null || _registerTargetCoverage == null)
            {
                _logger.Fatal("Testcoverage Injector could not initialize injection methods");
                Environment.Exit(13);
            }
        }

        /// <summary>
        ///     Getting the method definition of a method with the name that is supplied
        /// </summary>
        /// <param name="name"></param>
        /// <param name="injectionAssembly"></param>
        /// <returns></returns>
        private MethodDefinition getType(string name, ModuleDefinition injectionAssembly)
        {
            return injectionAssembly.Types.SelectMany(x => x.Methods.Where(y => y.Name == name)).First();
        }

        public static TestCoverageInjector Instance => _instance.Value;

        /// <summary>
        ///     Injects a call to <see cref="CoverageRegistry" /> Initialize method into the
        ///     <Module>
        ///         initialize function.
        ///         For more info see: https://einaregilsson.com/module-initializers-in-csharp/
        /// </summary>
        /// <param name="toInjectModule"></param>
        public void InjectModuleInit(ModuleDefinition toInjectModule)
        {
            File.Copy(_currentAssemblyPath,
                Path.Combine(Path.GetDirectoryName(toInjectModule.FileName), Path.GetFileName(_currentAssemblyPath)),
                true);

            const MethodAttributes moduleInitAttributes = MethodAttributes.Static
                | MethodAttributes.Assembly
                | MethodAttributes.SpecialName
                | MethodAttributes.RTSpecialName;

            MethodReference method = toInjectModule.ImportReference(_initializeMethodDefinition);
            MethodDefinition cctor = GetOrMake(moduleInitAttributes, toInjectModule, method.ReturnType);

            if (!IsCallAlreadyDone(method, cctor))
            {
                AddMethod(method, cctor);
            }
        }

        /// <summary>
        ///     Get the method definition for the constructor. If it's empty, create a new method definition instead
        /// </summary>
        /// <param name="moduleInitAttributes"></param>
        /// <param name="moduleType"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        private MethodDefinition GetOrMake(MethodAttributes moduleInitAttributes, ModuleDefinition toInjectModule, TypeReference returnType)
        {
            AssemblyDefinition assembly = toInjectModule.Assembly;
            TypeDefinition moduleType = assembly.MainModule.GetType("<Module>");

            MethodDefinition cctor =
                moduleType.Methods.FirstOrDefault(moduleTypeMethod => moduleTypeMethod.Name == ".cctor");
            if (cctor == null)
            {
                cctor = new MethodDefinition(".cctor", moduleInitAttributes, returnType);
                moduleType.Methods.Add(cctor);
            }

            return cctor;
        }

        /// <summary>
        ///     Check if the call is already done
        /// </summary>
        /// <param name="method"></param>
        /// <param name="cctor"></param>
        /// <returns></returns>
        private bool IsCallAlreadyDone(MethodReference method, MethodDefinition cctor)
        {
            return cctor.Body.Instructions.Any(instruction =>
                instruction.OpCode == OpCodes.Call && instruction.Operand == method);
        }

        /// <summary>
        ///     Add the method, and add any ret instruction if necessary
        /// </summary>
        /// <param name="method"></param>
        /// <param name="cctor"></param>
        private void AddMethod(MethodReference method, MethodDefinition cctor)
        {
            ILProcessor ilProcessor = cctor.Body.GetILProcessor();
            Instruction retInstruction =
                cctor.Body.Instructions.FirstOrDefault(instruction => instruction.OpCode == OpCodes.Ret);
            Instruction callMethod = ilProcessor.Create(OpCodes.Call, method);

            if (retInstruction == null)
            {
                ilProcessor.Append(callMethod);
                ilProcessor.Emit(OpCodes.Ret);
            }
            else
            {
                ilProcessor.InsertBefore(retInstruction, callMethod);
            }
        }

        /// <summary>
        ///     Injects the required references for the `Faultify.Injection` <see cref="CoverageRegistry" /> code into the given
        ///     module.
        /// </summary>
        /// <param name="module"></param>
        public void InjectAssemblyReferences(ModuleDefinition module)
        {
            // Find the references for `Faultify.TestRunner.Shared` and copy it over to the module directory and add it as reference.
            Assembly assembly = typeof(Dictionary<string, List<Tuple<string, int>>>).Assembly;

            string dest = Path.Combine(Path.GetDirectoryName(module.FileName), Path.GetFileName(assembly.Location));
            File.Copy(assembly.Location, dest, true);

            AssemblyNameReference shared =
                _registerTargetCoverage.Module.AssemblyReferences.First(x => x.Name == assembly.GetName().Name);

            module.AssemblyReferences.Add(shared);
            module.AssemblyReferences.Add(_registerTargetCoverage.Module.Assembly.Name);
        }

        /// <summary>
        ///     Injects the coverage register function for each method in the given module.
        /// </summary>
        public void InjectTargetCoverage(ModuleDefinition module)
        {
            foreach (TypeDefinition typeDefinition in module.Types.Where(x => !x.Name.StartsWith("<")))
            {
                foreach (MethodDefinition method in typeDefinition.Methods)
                {
                    MethodReference registerMethodReference = method.Module.ImportReference(_registerTargetCoverage);
                    if (method.Body != null)
                    {
                        InsertInstructionsWithName(method, registerMethodReference);
                    }
                }
            }
        }

        /// <summary>
        ///     Get the opcode instructions, and insert them. Including the assembly name
        /// </summary>
        /// <param name="method"></param>
        /// <param name="registerMethodReference"></param>
        private void InsertInstructionsWithName(MethodDefinition method, MethodReference registerMethodReference)
        {
            ILProcessor processor = method.Body.GetILProcessor();

            Instruction callInstruction = processor.Create(OpCodes.Call, registerMethodReference);
            Instruction entityHandle = processor.Create(OpCodes.Ldc_I4, method.MetadataToken.ToInt32());
            Instruction assemblyName = processor.Create(OpCodes.Ldstr, method.Module.Assembly.Name.Name);

            method.Body.Instructions.Insert(0, callInstruction);
            method.Body.Instructions.Insert(0, entityHandle);
            method.Body.Instructions.Insert(0, assemblyName);
        }

        /// <summary>
        ///     Injects the test register function for each test method in the given module.
        /// </summary>
        public void InjectTestCoverage(ModuleDefinition module)
        {
            module.AssemblyReferences.Add(_registerTestCoverage.Module.Assembly.Name);
            module.AssemblyReferences.Add(
                _registerTargetCoverage.Module.AssemblyReferences.First(x => x.Name == "Faultify.TestRunner.Shared"));

            foreach (TypeDefinition typeDefinition in module.Types.Where(x => !x.Name.StartsWith("<")))
            foreach (MethodDefinition method in typeDefinition.Methods
                .Where(m => m.HasCustomAttributes
                    && m.CustomAttributes
                        .Any(x => x.AttributeType.Name == "TestAttribute"
                            || x.AttributeType.Name == "TestMethodAttribute"
                            || x.AttributeType.Name == "FactAttribute")))
                {
                    MethodReference registerMethodReference = method.Module.ImportReference(_registerTestCoverage);
                    if (method.Body != null)
                    {
                        InsertInstructions(method, registerMethodReference);
                    }
                }
        }

        /// <summary>
        ///     Insert a callback to the register function
        /// </summary>
        /// <param name="method"></param>
        /// <param name="registerMethodReference"></param>
        private void InsertInstructions(MethodDefinition method, MethodReference registerMethodReference)
        {
            ILProcessor processor = method.Body.GetILProcessor();
            // Insert instruction that loads the meta data token as parameter for the register method.
            Instruction entityHandle =
                processor.Create(OpCodes.Ldstr, method.DeclaringType.FullName + "." + method.Name);

            // Insert instruction that calls the register function.
            Instruction callInstruction = processor.Create(OpCodes.Call, registerMethodReference);

            method.Body.Instructions.Insert(0, callInstruction);
            method.Body.Instructions.Insert(0, entityHandle);
        }
    }
}
