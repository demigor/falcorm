// Compatibility shim for .NET Standard 2.0
// IsExternalInit is required for init-only setters (C# 9+) and record types
#if NETSTANDARD2_0 || NETSTANDARD2_1 || NETSTANDARD
namespace System.Runtime.CompilerServices
{
  // This type is required by the compiler for init-only setters and record types
  // It's a marker type that doesn't need any implementation
  public static class IsExternalInit
  {
  }
}
#endif
