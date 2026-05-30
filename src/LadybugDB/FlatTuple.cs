using System;
using LadybugDB.Interop;

namespace LadybugDB;

/// <summary>
/// A single row of a <see cref="QueryResult"/>. The underlying native tuple buffer is reused by the
/// engine across iterations, so read or copy values before advancing the result. The high-level
/// <see cref="QueryResult.Rows"/> helper handles this for you.
/// </summary>
public sealed class FlatTuple : IDisposable
{
    private LbugFlatTuple _handle;
    private bool _disposed;

    internal FlatTuple(LbugFlatTuple handle)
    {
        _handle = handle;
    }

    /// <summary>Returns the value at the given zero-based column index.</summary>
    public Value GetValue(ulong index)
    {
        ThrowIfDisposed();
        LbugState state = Native.FlatTupleGetValue(ref _handle, index, out LbugValue value);
        if (state != LbugState.Success)
        {
            throw new LadybugException($"Failed to read value at column {index}.");
        }

        return new Value(value);
    }

    /// <summary>Returns the value at the given zero-based column index.</summary>
    public Value GetValue(int index)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return GetValue((ulong)index);
    }

    /// <inheritdoc />
    public override string? ToString()
    {
        if (_disposed)
        {
            return null;
        }

        return Native.TakeString(Native.FlatTupleToString(ref _handle));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Native.FlatTupleDestroy(ref _handle);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(FlatTuple));
        }
    }
}
