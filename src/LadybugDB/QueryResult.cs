using System;
using System.Collections.Generic;
using System.Threading;
using LadybugDB.Interop;

namespace LadybugDB;

/// <summary>
/// The result of executing a Cypher query: column metadata plus a forward-only stream of rows.
/// </summary>
public sealed class QueryResult : IDisposable
{
    private LbugQueryResult _handle;
    private int _disposed;
    private string[]? _columnNames;

    internal QueryResult(LbugQueryResult handle)
    {
        _handle = handle;
    }

    /// <summary>Whether the query executed successfully.</summary>
    public bool IsSuccess => Volatile.Read(ref _disposed) == 0 && Native.QueryResultIsSuccess(ref _handle);

    /// <summary>The error message for a failed query, or <see langword="null"/> on success.</summary>
    public string? GetErrorMessage()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return null;
        }

        return Native.TakeString(Native.QueryResultGetErrorMessage(ref _handle));
    }

    /// <summary>Number of columns in the result.</summary>
    public ulong ColumnCount
    {
        get
        {
            ThrowIfDisposed();
            return Native.QueryResultGetNumColumns(ref _handle);
        }
    }

    /// <summary>Number of tuples (rows) in the result.</summary>
    public ulong RowCount
    {
        get
        {
            ThrowIfDisposed();
            return Native.QueryResultGetNumTuples(ref _handle);
        }
    }

    /// <summary>The name of the column at the given zero-based index.</summary>
    public string GetColumnName(ulong index)
    {
        ThrowIfDisposed();
        LbugState state = Native.QueryResultGetColumnName(ref _handle, index, out IntPtr pointer);
        if (state != LbugState.Success)
        {
            throw new LadybugException($"Failed to read column name at index {index}.");
        }

        return Native.TakeString(pointer) ?? string.Empty;
    }

    /// <summary>All column names, cached after first access.</summary>
    public IReadOnlyList<string> ColumnNames
    {
        get
        {
            if (_columnNames is null)
            {
                ulong count = ColumnCount;
                var names = new string[count];
                for (ulong i = 0; i < count; i++)
                {
                    names[i] = GetColumnName(i);
                }

                _columnNames = names;
            }

            return _columnNames;
        }
    }

    /// <summary>Whether another tuple is available from the current iterator position.</summary>
    public bool HasNext()
    {
        ThrowIfDisposed();
        return Native.QueryResultHasNext(ref _handle);
    }

    /// <summary>
    /// Advances to and returns the next tuple. The returned <see cref="FlatTuple"/> shares the
    /// engine's reusable buffer; consume it before calling this again.
    /// </summary>
    public FlatTuple GetNext()
    {
        ThrowIfDisposed();
        LbugState state = Native.QueryResultGetNext(ref _handle, out LbugFlatTuple tuple);
        if (state != LbugState.Success)
        {
            throw new LadybugException("Failed to advance the query result.");
        }

        return new FlatTuple(tuple);
    }

    /// <summary>
    /// Enumerates the result as fully materialized rows. Each row is read into managed memory before
    /// the iterator advances, which makes it safe against the engine's reused tuple buffer.
    /// </summary>
    public IEnumerable<object?[]> Rows()
    {
        ThrowIfDisposed();
        int columns = checked((int)ColumnCount);

        while (HasNext())
        {
            using FlatTuple tuple = GetNext();
            var row = new object?[columns];
            for (int i = 0; i < columns; i++)
            {
                using Value value = tuple.GetValue((ulong)i);
                row[i] = value.GetValue();
            }

            yield return row;
        }
    }

    /// <inheritdoc />
    public override string? ToString()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return null;
        }

        return Native.TakeString(Native.QueryResultToString(ref _handle));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        Native.QueryResultDestroy(ref _handle);
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(QueryResult));
        }
    }
}
