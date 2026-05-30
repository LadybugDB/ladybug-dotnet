using System;
using System.Threading;
using LadybugDB.Interop;

namespace LadybugDB;

/// <summary>
/// An embedded Ladybug database instance. A single <see cref="Database"/> can serve many
/// <see cref="Connection"/> objects. Dispose it after all connections are closed.
/// </summary>
public sealed class Database : IDisposable
{
    private LbugDatabase _handle;
    private int _disposed;

    /// <summary>Opens (or creates) a database at <paramref name="databasePath"/>.</summary>
    /// <param name="databasePath">
    /// Filesystem path to the database. Use an empty string for an in-memory database.
    /// </param>
    /// <param name="config">Optional runtime configuration; native defaults are used when omitted.</param>
    public Database(string databasePath = "", SystemConfig? config = null)
    {
        if (databasePath is null)
        {
            throw new ArgumentNullException(nameof(databasePath));
        }

        LbugSystemConfig nativeConfig = (config ?? new SystemConfig()).ToNative();
        LbugState state = Native.DatabaseInit(databasePath, nativeConfig, out _handle);
        if (state != LbugState.Success)
        {
            throw new LadybugException(
                $"Failed to open Ladybug database at '{databasePath}'.");
        }
    }

    internal LbugState InitConnection(out LbugConnection connection)
    {
        ThrowIfDisposed();
        return Native.ConnectionInit(ref _handle, out connection);
    }

    internal void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(Database));
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        Native.DatabaseDestroy(ref _handle);
    }
}
