using System.Reflection.Metadata;

namespace Faultify.CodeDecompiler
{
    /// <summary>
    /// A dummy decompiler to use when a proper one is not available.
    /// </summary>
    public class NullDecompiler : ICodeDecompiler
    {
        
        public static readonly string CONSTANT_NULL_CODE = "SOURCE CODE COULD NOT BE GENERATED";
        
        public string Decompile(EntityHandle entityHandle)
        {
            return CONSTANT_NULL_CODE;
        }
    }
}
