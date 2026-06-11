using System;
using System.Collections;
using System.Collections.Generic;
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
            case null: return BindValue(name, CreateNativeValue(null));
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
            case IEnumerable v: return BindValue(name, CreateNativeValue(v));
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

    private static IntPtr CreateNativeValue(object? value)
    {
        IntPtr handle = value switch
        {
            null => Native.ValueCreateNull(),
            bool v => Native.ValueCreateBool(v),
            sbyte v => Native.ValueCreateInt8(v),
            short v => Native.ValueCreateInt16(v),
            int v => Native.ValueCreateInt32(v),
            long v => Native.ValueCreateInt64(v),
            byte v => Native.ValueCreateUInt8(v),
            ushort v => Native.ValueCreateUInt16(v),
            uint v => Native.ValueCreateUInt32(v),
            ulong v => Native.ValueCreateUInt64(v),
            float v => Native.ValueCreateFloat(v),
            double v => Native.ValueCreateDouble(v),
            string v => Native.ValueCreateString(v),
            Guid v => Native.ValueCreateString(v.ToString()),
            DateTimeOffset v => Native.ValueCreateTimestampTz(new LbugTimestamp { Value = (v.UtcDateTime.Ticks - UnixEpochTicks) / 10L }),
            DateTime v => Native.ValueCreateTimestamp(new LbugTimestamp { Value = (ToUtcTicks(v) - UnixEpochTicks) / 10L }),
            Interval v => Native.ValueCreateInterval(new LbugInterval { Months = v.Months, Days = v.Days, Micros = v.Micros }),
#if NET7_0_OR_GREATER
            DateOnly v => Native.ValueCreateDate(new LbugDate { Days = v.DayNumber - new DateOnly(1970, 1, 1).DayNumber }),
#endif
            IEnumerable v => CreateNativeList(v),
            _ => throw new NotSupportedException($"Cannot bind a parameter of type {value.GetType()}.")
        };

        if (handle == IntPtr.Zero)
        {
            throw new LadybugException("Failed to create a native parameter value.");
        }

        return handle;
    }

    private static IntPtr CreateNativeList(IEnumerable values)
    {
        var elementHandles = new List<IntPtr>();
        try
        {
            foreach (object? value in values)
            {
                elementHandles.Add(CreateNativeValue(value));
            }

            if (elementHandles.Count == 0)
            {
                return CreateNativeEmptyList(values.GetType());
            }

            IntPtr[] elements = elementHandles.ToArray();
            LbugState state = Native.ValueCreateList((ulong)elements.Length, elements, out IntPtr listHandle);
            if (state != LbugState.Success || listHandle == IntPtr.Zero)
            {
                throw new LadybugException("Failed to create a LIST parameter value.");
            }

            return listHandle;
        }
        finally
        {
            foreach (IntPtr handle in elementHandles)
            {
                Native.ValueDestroy(handle);
            }
        }
    }

    private static IntPtr CreateNativeEmptyList(Type sequenceType)
    {
        if (!TryGetElementDataTypeId(sequenceType, out LbugDataTypeId childTypeId))
        {
            throw new NotSupportedException(
                $"Cannot bind empty enumerable parameter type {sequenceType} because its element type is unknown or unsupported.");
        }

        Native.DataTypeCreate(childTypeId, IntPtr.Zero, 0, out LbugLogicalType childType);
        try
        {
            Native.DataTypeCreateWithChild(LbugDataTypeId.List, ref childType, 0, out LbugLogicalType listType);
            try
            {
                IntPtr handle = Native.ValueCreateDefault(ref listType);
                if (handle == IntPtr.Zero)
                {
                    throw new LadybugException("Failed to create an empty LIST parameter value.");
                }

                return handle;
            }
            finally
            {
                Native.DataTypeDestroy(ref listType);
            }
        }
        finally
        {
            Native.DataTypeDestroy(ref childType);
        }
    }

    private static bool TryGetElementDataTypeId(Type sequenceType, out LbugDataTypeId dataTypeId)
    {
        Type? elementType = GetEnumerableElementType(sequenceType);
        if (elementType is not null && Nullable.GetUnderlyingType(elementType) is { } nullableType)
        {
            elementType = nullableType;
        }

        if (elementType == typeof(bool))
        {
            dataTypeId = LbugDataTypeId.Bool;
            return true;
        }

        if (elementType == typeof(sbyte))
        {
            dataTypeId = LbugDataTypeId.Int8;
            return true;
        }

        if (elementType == typeof(short))
        {
            dataTypeId = LbugDataTypeId.Int16;
            return true;
        }

        if (elementType == typeof(int))
        {
            dataTypeId = LbugDataTypeId.Int32;
            return true;
        }

        if (elementType == typeof(long))
        {
            dataTypeId = LbugDataTypeId.Int64;
            return true;
        }

        if (elementType == typeof(byte))
        {
            dataTypeId = LbugDataTypeId.UInt8;
            return true;
        }

        if (elementType == typeof(ushort))
        {
            dataTypeId = LbugDataTypeId.UInt16;
            return true;
        }

        if (elementType == typeof(uint))
        {
            dataTypeId = LbugDataTypeId.UInt32;
            return true;
        }

        if (elementType == typeof(ulong))
        {
            dataTypeId = LbugDataTypeId.UInt64;
            return true;
        }

        if (elementType == typeof(float))
        {
            dataTypeId = LbugDataTypeId.Float;
            return true;
        }

        if (elementType == typeof(double))
        {
            dataTypeId = LbugDataTypeId.Double;
            return true;
        }

        if (elementType == typeof(string) || elementType == typeof(Guid))
        {
            dataTypeId = LbugDataTypeId.String;
            return true;
        }

        if (elementType == typeof(DateTime))
        {
            dataTypeId = LbugDataTypeId.Timestamp;
            return true;
        }

        if (elementType == typeof(DateTimeOffset))
        {
            dataTypeId = LbugDataTypeId.TimestampTz;
            return true;
        }

        if (elementType == typeof(Interval))
        {
            dataTypeId = LbugDataTypeId.Interval;
            return true;
        }

#if NET7_0_OR_GREATER
        if (elementType == typeof(DateOnly))
        {
            dataTypeId = LbugDataTypeId.Date;
            return true;
        }
#endif

        dataTypeId = default;
        return false;
    }

    private static Type? GetEnumerableElementType(Type sequenceType)
    {
        if (sequenceType.IsArray)
        {
            return sequenceType.GetElementType();
        }

        if (sequenceType.IsGenericType && sequenceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return sequenceType.GetGenericArguments()[0];
        }

        if (sequenceType.IsGenericType)
        {
            Type[] genericArguments = sequenceType.GetGenericArguments();
            if (genericArguments.Length == 1)
            {
                return genericArguments[0];
            }
        }

        return null;
    }

    private PreparedStatement BindValue(string name, IntPtr value)
    {
        try
        {
            return Do(name, Native.PreparedStatementBindValue(ref _handle, Name(name), value));
        }
        finally
        {
            Native.ValueDestroy(value);
        }
    }

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
