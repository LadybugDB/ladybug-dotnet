using System;
using System.Threading;
using LadybugDB.Interop;

namespace LadybugDB;

/// <summary>
/// A parameterized, pre-compiled Cypher statement. Bind parameters with the fluent <c>Bind</c>
/// overloads, then run it with <see cref="Execute"/> (or <c>Connection.Execute</c>). Reusing a
/// prepared statement avoids re-planning the query on each execution.
/// </summary>
public sealed class PreparedStatement : IDisposable
{
    private static readonly long UnixEpochTicks = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;

    private readonly Connection _connection;
    private LbugPreparedStatement _handle;
    private int _disposed;

    internal PreparedStatement(Connection connection, LbugPreparedStatement handle)
    {
        _connection = connection;
        _handle = handle;
    }

    /// <summary>Whether the statement was prepared successfully.</summary>
    public bool IsSuccess => Volatile.Read(ref _disposed) == 0 && Native.PreparedStatementIsSuccess(ref _handle);

    /// <summary>Whether the statement performs only read operations.</summary>
    public bool IsReadOnly
    {
        get
        {
            ThrowIfDisposed();
            return Native.PreparedStatementIsReadOnly(ref _handle);
        }
    }

    /// <summary>The error message when preparation failed, otherwise <see langword="null"/>.</summary>
    public string? GetErrorMessage()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return null;
        }

        return Native.TakeString(Native.PreparedStatementGetErrorMessage(ref _handle));
    }

    public PreparedStatement Bind(string name, bool value) => Do(name, Native.PreparedStatementBindBool(ref _handle, Name(name), value));

    public PreparedStatement Bind(string name, sbyte value) => Do(name, Native.PreparedStatementBindInt8(ref _handle, Name(name), value));

    public PreparedStatement Bind(string name, short value) => Do(name, Native.PreparedStatementBindInt16(ref _handle, Name(name), value));

    public PreparedStatement Bind(string name, int value) => Do(name, Native.PreparedStatementBindInt32(ref _handle, Name(name), value));

    public PreparedStatement Bind(string name, long value) => Do(name, Native.PreparedStatementBindInt64(ref _handle, Name(name), value));

    public PreparedStatement Bind(string name, byte value) => Do(name, Native.PreparedStatementBindUInt8(ref _handle, Name(name), value));

    public PreparedStatement Bind(string name, ushort value) => Do(name, Native.PreparedStatementBindUInt16(ref _handle, Name(name), value));

    public PreparedStatement Bind(string name, uint value) => Do(name, Native.PreparedStatementBindUInt32(ref _handle, Name(name), value));

    public PreparedStatement Bind(string name, ulong value) => Do(name, Native.PreparedStatementBindUInt64(ref _handle, Name(name), value));

    public PreparedStatement Bind(string name, float value) => Do(name, Native.PreparedStatementBindFloat(ref _handle, Name(name), value));

    public PreparedStatement Bind(string name, double value) => Do(name, Native.PreparedStatementBindDouble(ref _handle, Name(name), value));

    public PreparedStatement Bind(string name, string value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return Do(name, Native.PreparedStatementBindString(ref _handle, Name(name), value));
    }

    public PreparedStatement Bind(string name, Guid value) => Bind(name, value.ToString());

    public PreparedStatement Bind(string name, DateTime value)
    {
        long micros = (ToUtcTicks(value) - UnixEpochTicks) / 10L;
        return Do(name, Native.PreparedStatementBindTimestamp(ref _handle, Name(name), new LbugTimestamp { Value = micros }));
    }

    public PreparedStatement Bind(string name, DateTimeOffset value)
    {
        long micros = (value.UtcDateTime.Ticks - UnixEpochTicks) / 10L;
        return Do(name, Native.PreparedStatementBindTimestampTz(ref _handle, Name(name), new LbugTimestamp { Value = micros }));
    }

    public PreparedStatement Bind(string name, Interval value)
        => Do(name, Native.PreparedStatementBindInterval(ref _handle, Name(name), new LbugInterval { Months = value.Months, Days = value.Days, Micros = value.Micros }));

#if NET7_0_OR_GREATER
    public PreparedStatement Bind(string name, DateOnly value)
    {
        int days = value.DayNumber - new DateOnly(1970, 1, 1).DayNumber;
        return Do(name, Native.PreparedStatementBindDate(ref _handle, Name(name), new LbugDate { Days = days }));
    }
#endif

    /// <summary>Binds a parameter whose CLR type is determined at runtime.</summary>
    public PreparedStatement Bind(string name, object? value)
    {
        switch (value)
        {
            case null:
                throw new ArgumentNullException(nameof(value), "Binding null parameters is not supported.");
            case bool v: return Bind(name, v);
            case sbyte v: return Bind(name, v);
            case short v: return Bind(name, v);
            case int v: return Bind(name, v);
            case long v: return Bind(name, v);
            case byte v: return Bind(name, v);
            case ushort v: return Bind(name, v);
            case uint v: return Bind(name, v);
            case ulong v: return Bind(name, v);
            case float v: return Bind(name, v);
            case double v: return Bind(name, v);
            case string v: return Bind(name, v);
            case Guid v: return Bind(name, v);
            case DateTimeOffset v: return Bind(name, v);
            case DateTime v: return Bind(name, v);
            case Interval v: return Bind(name, v);
#if NET7_0_OR_GREATER
            case DateOnly v: return Bind(name, v);
#endif
            default:
                throw new NotSupportedException($"Cannot bind a parameter of type {value.GetType()}.");
        }
    }

    /// <summary>Executes the statement on its owning connection.</summary>
    public QueryResult Execute() => _connection.Execute(this);

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        Native.PreparedStatementDestroy(ref _handle);
    }

    internal LbugState ExecuteOn(ref LbugConnection connection, out LbugQueryResult outQueryResult)
    {
        ThrowIfDisposed();
        return Native.ConnectionExecute(ref connection, ref _handle, out outQueryResult);
    }

    // Evaluated while building the native bind call's arguments, so it guards against use-after-dispose
    // before the native function actually runs.
    private string Name(string name)
    {
        ThrowIfDisposed();
        return name ?? throw new ArgumentNullException(nameof(name));
    }

    private static long ToUtcTicks(DateTime value)
        => value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc).Ticks
            : value.ToUniversalTime().Ticks;

    private PreparedStatement Do(string name, LbugState state)
    {
        if (state != LbugState.Success)
        {
            throw new LadybugException($"Failed to bind parameter '{name}'.");
        }

        return this;
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(PreparedStatement));
        }
    }
}
