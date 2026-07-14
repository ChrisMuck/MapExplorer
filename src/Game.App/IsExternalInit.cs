#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    /// <summary>Compatibility shim for Unity's netstandard profile when compiling C# 9 records.</summary>
    internal static class IsExternalInit
    {
    }
}
#endif
