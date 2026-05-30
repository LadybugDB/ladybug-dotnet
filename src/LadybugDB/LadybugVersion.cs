using LadybugDB.Interop;

namespace LadybugDB;

/// <summary>Version information reported by the native Ladybug library.</summary>
public static class LadybugVersion
{
    /// <summary>The Ladybug release version string (for example, <c>0.x.y</c>).</summary>
    public static string Version => Native.GetVersion() ?? string.Empty;

    /// <summary>The on-disk storage format version understood by this build.</summary>
    public static ulong StorageVersion => Native.GetStorageVersion();
}
