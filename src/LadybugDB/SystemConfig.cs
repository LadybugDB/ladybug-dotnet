using LadybugDB.Interop;

namespace LadybugDB;

/// <summary>
/// Runtime configuration used when opening a <see cref="Database"/>. Any property left
/// <see langword="null"/> keeps the native default returned by <c>lbug_default_system_config</c>.
/// </summary>
public sealed class SystemConfig
{
    /// <summary>Buffer pool size in bytes.</summary>
    public ulong? BufferPoolSize { get; set; }

    /// <summary>Maximum number of threads used to execute queries.</summary>
    public ulong? MaxNumThreads { get; set; }

    /// <summary>Whether on-disk compression is enabled.</summary>
    public bool? EnableCompression { get; set; }

    /// <summary>Open the database in read-only mode.</summary>
    public bool? ReadOnly { get; set; }

    /// <summary>Maximum database size in bytes.</summary>
    public ulong? MaxDbSize { get; set; }

    /// <summary>Whether automatic checkpointing is enabled.</summary>
    public bool? AutoCheckpoint { get; set; }

    /// <summary>WAL size (bytes) that triggers an automatic checkpoint.</summary>
    public ulong? CheckpointThreshold { get; set; }

    /// <summary>Throw if replaying the write-ahead log fails on startup.</summary>
    public bool? ThrowOnWalReplayFailure { get; set; }

    /// <summary>Whether page checksums are enabled.</summary>
    public bool? EnableChecksums { get; set; }

    /// <summary>Whether concurrent writers are allowed.</summary>
    public bool? EnableMultiWrites { get; set; }

    /// <summary>Whether the default hash index is enabled.</summary>
    public bool? EnableDefaultHashIndex { get; set; }

    internal LbugSystemConfig ToNative()
    {
        LbugSystemConfig config = Native.DefaultSystemConfig();

        if (BufferPoolSize is { } bufferPoolSize)
        {
            config.BufferPoolSize = bufferPoolSize;
        }

        if (MaxNumThreads is { } maxNumThreads)
        {
            config.MaxNumThreads = maxNumThreads;
        }

        if (EnableCompression is { } enableCompression)
        {
            config.EnableCompression = ToByte(enableCompression);
        }

        if (ReadOnly is { } readOnly)
        {
            config.ReadOnly = ToByte(readOnly);
        }

        if (MaxDbSize is { } maxDbSize)
        {
            config.MaxDbSize = maxDbSize;
        }

        if (AutoCheckpoint is { } autoCheckpoint)
        {
            config.AutoCheckpoint = ToByte(autoCheckpoint);
        }

        if (CheckpointThreshold is { } checkpointThreshold)
        {
            config.CheckpointThreshold = checkpointThreshold;
        }

        if (ThrowOnWalReplayFailure is { } throwOnWalReplayFailure)
        {
            config.ThrowOnWalReplayFailure = ToByte(throwOnWalReplayFailure);
        }

        if (EnableChecksums is { } enableChecksums)
        {
            config.EnableChecksums = ToByte(enableChecksums);
        }

        if (EnableMultiWrites is { } enableMultiWrites)
        {
            config.EnableMultiWrites = ToByte(enableMultiWrites);
        }

        if (EnableDefaultHashIndex is { } enableDefaultHashIndex)
        {
            config.EnableDefaultHashIndex = ToByte(enableDefaultHashIndex);
        }

        return config;
    }

    private static byte ToByte(bool value) => value ? (byte)1 : (byte)0;
}
