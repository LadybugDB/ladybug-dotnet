using System;
using System.Runtime.InteropServices;
using System.Text;
#if NET7_0_OR_GREATER
using System.Reflection;
using System.Runtime.CompilerServices;
#endif

namespace LadybugDB.Interop;

/// <summary>
/// Low-level P/Invoke surface for the Ladybug C API. The platform-specific declarations live in
/// <c>Native.LibraryImport.cs</c> (net7.0+) and <c>Native.DllImport.cs</c> (netstandard2.0); this
/// file holds shared constants, the native library resolver, and marshaling helpers.
/// </summary>
internal static partial class Native
{
    /// <summary>
    /// Canonical import name. On Windows the native file is <c>lbug_shared.dll</c>; on Linux/macOS the
    /// resolver (net7.0+) remaps to <c>liblbug.so</c> / <c>liblbug.dylib</c>.
    /// </summary>
    internal const string LibraryName = "lbug_shared";

#if NET7_0_OR_GREATER
    // The module initializer guarantees the resolver is registered before any P/Invoke into the
    // native library; CA2255's "app code only" guidance does not apply to this scenario.
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void RegisterResolver()
    {
        NativeLibrary.SetDllImportResolver(typeof(Native).Assembly, Resolve);
    }

    private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != LibraryName)
        {
            return IntPtr.Zero;
        }

        foreach (string candidate in GetCandidateNames())
        {
            if (NativeLibrary.TryLoad(candidate, assembly, searchPath, out IntPtr handle))
            {
                return handle;
            }
        }

        return IntPtr.Zero;
    }

    private static string[] GetCandidateNames()
    {
        if (OperatingSystem.IsWindows())
        {
            return new[] { "lbug_shared", "lbug_shared.dll", "liblbug" };
        }

        if (OperatingSystem.IsMacOS())
        {
            return new[] { "liblbug.dylib", "liblbug", "lbug_shared" };
        }

        return new[] { "liblbug.so", "liblbug", "lbug_shared" };
    }
#endif

    /// <summary>Convenience wrapper for <c>lbug_get_version</c> (owns and frees the returned string).</summary>
    internal static string? GetVersion() => TakeString(GetVersionPtr());

    /// <summary>Consumes the last native error and frees its returned string.</summary>
    internal static string? GetLastError() => TakeString(GetLastErrorPtr());

    /// <summary>Decodes a NUL-terminated UTF-8 C string into a managed string without taking ownership.</summary>
    internal static string? PtrToStringUtf8(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero)
        {
            return null;
        }

#if NET7_0_OR_GREATER
        return Marshal.PtrToStringUTF8(ptr);
#else
        int length = 0;
        while (Marshal.ReadByte(ptr, length) != 0)
        {
            length++;
        }

        if (length == 0)
        {
            return string.Empty;
        }

        byte[] bytes = new byte[length];
        Marshal.Copy(ptr, bytes, 0, length);
        return Encoding.UTF8.GetString(bytes);
#endif
    }

    /// <summary>Copies an owned C string into managed memory and frees it via <c>lbug_destroy_string</c>.</summary>
    internal static string? TakeString(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            return PtrToStringUtf8(ptr);
        }
        finally
        {
            DestroyString(ptr);
        }
    }

#if NETSTANDARD2_0
    /// <summary>Encodes a managed string as a NUL-terminated UTF-8 byte buffer for marshaling on netstandard2.0.</summary>
    internal static byte[] ToUtf8(string value)
    {
        int byteCount = Encoding.UTF8.GetByteCount(value);
        byte[] bytes = new byte[byteCount + 1];
        Encoding.UTF8.GetBytes(value, 0, value.Length, bytes, 0);
        bytes[byteCount] = 0;
        return bytes;
    }
#endif
}
