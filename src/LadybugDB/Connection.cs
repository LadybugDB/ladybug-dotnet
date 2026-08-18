using System;
using System.Collections.Generic;
using System.Threading;
using LadybugDB.Interop;

namespace LadybugDB;

/// <summary>
/// A connection to a <see cref="Database"/> used to execute Cypher queries and prepared statements.
/// Operations on a single connection are serialized internally, and disposal is idempotent.
/// </summary>
public sealed class Connection : IDisposable
{
    private readonly Database _database;
    private readonly object _gate = new();
    private LbugConnection _handle;
    private int _disposed;

    /// <summary>Creates a new connection to <paramref name="database"/>.</summary>
    public Connection(Database database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));

        LbugState state = database.InitConnection(out _handle);
        if (state != LbugState.Success)
        {
            throw new LadybugException("Failed to create a Ladybug connection.");
        }
    }

    /// <summary>Executes a Cypher query and returns its result.</summary>
    /// <exception cref="LadybugQueryException">Thrown when the query fails to execute.</exception>
    public QueryResult Query(string cypher)
    {
        if (cypher is null)
        {
            throw new ArgumentNullException(nameof(cypher));
        }

        lock (_gate)
        {
            ThrowIfDisposed();
            LbugState state = Native.ConnectionQuery(ref _handle, cypher, out LbugQueryResult resultHandle);
            return Finish(state, resultHandle);
        }
    }

    /// <summary>Prepares a parameterized Cypher statement for repeated execution.</summary>
    /// <exception cref="LadybugQueryException">Thrown when the statement fails to prepare.</exception>
    public PreparedStatement Prepare(string cypher)
    {
        if (cypher is null)
        {
            throw new ArgumentNullException(nameof(cypher));
        }

        lock (_gate)
        {
            ThrowIfDisposed();
            LbugState state = Native.ConnectionPrepare(ref _handle, cypher, out LbugPreparedStatement handle);
            var statement = new PreparedStatement(this, handle);

            if (state != LbugState.Success || !statement.IsSuccess)
            {
                string message = statement.GetErrorMessage() ?? "Failed to prepare the Cypher statement.";
                statement.Dispose();
                throw new LadybugQueryException(message);
            }

            return statement;
        }
    }

    /// <summary>Executes a previously prepared statement (with its bound parameters).</summary>
    /// <exception cref="LadybugQueryException">Thrown when execution fails.</exception>
    public QueryResult Execute(PreparedStatement statement)
    {
        if (statement is null)
        {
            throw new ArgumentNullException(nameof(statement));
        }

        lock (_gate)
        {
            ThrowIfDisposed();
            LbugState state = statement.ExecuteOn(ref _handle, out LbugQueryResult resultHandle);
            return Finish(state, resultHandle);
        }
    }

    /// <summary>Prepares, binds, and executes a parameterized query in one call.</summary>
    public QueryResult Execute(string cypher, IReadOnlyDictionary<string, object?> parameters)
    {
        if (parameters is null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        using PreparedStatement statement = Prepare(cypher);
        foreach (KeyValuePair<string, object?> parameter in parameters)
        {
            statement.Bind(parameter.Key, parameter.Value);
        }

        return Execute(statement);
    }

    /// <summary>
    /// Returns SQL pushed down by a table function in the optimized plan for <paramref name="cypher"/>,
    /// or <see langword="null"/> when the plan contains no pushdown-capable table function.
    /// </summary>
    /// <exception cref="LadybugQueryException">Thrown when the query cannot be prepared or analyzed.</exception>
    public string? GetPushedSql(string cypher)
    {
        if (cypher is null)
        {
            throw new ArgumentNullException(nameof(cypher));
        }

        lock (_gate)
        {
            ThrowIfDisposed();
            LbugState state = Native.ConnectionGetPushedSql(ref _handle, cypher, out IntPtr sql);
            if (state != LbugState.Success)
            {
                string message = Native.GetLastError() ?? "Failed to get pushed-down SQL.";
                throw new LadybugQueryException(message);
            }

            return Native.TakeString(sql);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        lock (_gate)
        {
            Native.ConnectionDestroy(ref _handle);
        }
    }

    private QueryResult Finish(LbugState state, LbugQueryResult resultHandle)
    {
        var result = new QueryResult(resultHandle);
        if (state != LbugState.Success || !result.IsSuccess)
        {
            string message = result.GetErrorMessage() ?? "Ladybug query failed.";
            result.Dispose();
            throw new LadybugQueryException(message);
        }

        return result;
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(Connection));
        }

        _database.ThrowIfDisposed();
    }
}
