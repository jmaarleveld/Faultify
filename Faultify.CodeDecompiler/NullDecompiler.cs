using System.Reflection.Metadata;

namespace Faultify.CodeDecompiler
{
    /// <summary>
    /// A dummy decompiler to use when a proper one is not available.
    /// </summary>
    public class NullDecompiler : ICodeDecompiler
    {
        public string Decompile(EntityHandle entityHandle)
        {
            return "SOURCE CODE COULD NOT BE GENERATED";
        }
    }
}
