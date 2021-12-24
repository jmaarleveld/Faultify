using System.Reflection.Metadata;

namespace Faultify.CodeDecompiler
{
    public interface ICodeDecompiler
    {
        string Decompile(EntityHandle entityHandle);
    }
}
