#if NETSTANDARD2_0
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Polyfill enabling C# <c>init</c> accessors and records on netstandard2.0, which does not ship
    /// this type. Not used on modern target frameworks.
    /// </summary>
    internal static class IsExternalInit
    {
    }
}
#endif
