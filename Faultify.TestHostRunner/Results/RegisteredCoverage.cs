namespace Faultify.TestHostRunner.Results
{
    public class RegisteredCoverage
    {
        public RegisteredCoverage(string assemblyName, int entityHandle)
        {
            AssemblyName = assemblyName;
            EntityHandle = entityHandle;
        }

        public string AssemblyName { get; }
        public int EntityHandle { get; }

        public override int GetHashCode()
        {
            return (AssemblyName + ":" + EntityHandle).GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is RegisteredCoverage objCast
                   && AssemblyName == objCast.AssemblyName
                   && EntityHandle == objCast.EntityHandle;
        }
    }
}
