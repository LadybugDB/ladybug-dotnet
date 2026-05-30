using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using LadybugDB.Interop;

namespace LadybugDB;

/// <summary>
/// A single value read from a query result, materializable into managed objects across Ladybug's
/// full type system: primitives, temporal types, decimal/int128, uuid/blob, nested collections
/// (list, array, struct, map), and graph types (node, rel, recursive rel).
/// </summary>
public sealed class Value : IDisposable
{
    private static readonly DateTime UnixEpochUtc = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private LbugValue _handle;
    private bool _disposed;

    internal Value(LbugValue handle)
    {
        _handle = handle;
    }

    /// <summary>Whether this value is SQL NULL.</summary>
    public bool IsNull
    {
        get
        {
            ThrowIfDisposed();
            return Native.ValueIsNull(ref _handle);
        }
    }

    /// <summary>The logical data type of this value.</summary>
    public DataTypeId DataTypeId
    {
        get
        {
            ThrowIfDisposed();
            return GetDataTypeId();
        }
    }

    /// <summary>Materializes this value as a managed object (or <see langword="null"/> for NULL).</summary>
    public object? GetValue()
    {
        ThrowIfDisposed();

        if (Native.ValueIsNull(ref _handle))
        {
            return null;
        }

        DataTypeId typeId = GetDataTypeId();
        switch (typeId)
        {
            case DataTypeId.Bool:
                EnsureSuccess(Native.ValueGetBool(ref _handle, out byte boolValue), typeId);
                return boolValue != 0;
            case DataTypeId.Int8:
                EnsureSuccess(Native.ValueGetInt8(ref _handle, out sbyte int8Value), typeId);
                return int8Value;
            case DataTypeId.Int16:
                EnsureSuccess(Native.ValueGetInt16(ref _handle, out short int16Value), typeId);
                return int16Value;
            case DataTypeId.Int32:
                EnsureSuccess(Native.ValueGetInt32(ref _handle, out int int32Value), typeId);
                return int32Value;
            case DataTypeId.Int64:
            case DataTypeId.Serial:
                EnsureSuccess(Native.ValueGetInt64(ref _handle, out long int64Value), typeId);
                return int64Value;
            case DataTypeId.UInt8:
                EnsureSuccess(Native.ValueGetUInt8(ref _handle, out byte uint8Value), typeId);
                return uint8Value;
            case DataTypeId.UInt16:
                EnsureSuccess(Native.ValueGetUInt16(ref _handle, out ushort uint16Value), typeId);
                return uint16Value;
            case DataTypeId.UInt32:
                EnsureSuccess(Native.ValueGetUInt32(ref _handle, out uint uint32Value), typeId);
                return uint32Value;
            case DataTypeId.UInt64:
                EnsureSuccess(Native.ValueGetUInt64(ref _handle, out ulong uint64Value), typeId);
                return uint64Value;
            case DataTypeId.Float:
                EnsureSuccess(Native.ValueGetFloat(ref _handle, out float floatValue), typeId);
                return floatValue;
            case DataTypeId.Double:
                EnsureSuccess(Native.ValueGetDouble(ref _handle, out double doubleValue), typeId);
                return doubleValue;
            case DataTypeId.Int128:
                EnsureSuccess(Native.ValueGetInt128(ref _handle, out LbugInt128 int128Value), typeId);
                return FromInt128(int128Value);
            case DataTypeId.InternalId:
                return ReadInternalId();
            case DataTypeId.Date:
                EnsureSuccess(Native.ValueGetDate(ref _handle, out LbugDate dateValue), typeId);
                return FromDays(dateValue.Days);
            case DataTypeId.Timestamp:
                EnsureSuccess(Native.ValueGetTimestamp(ref _handle, out LbugTimestamp ts), typeId);
                return FromTicks(ts.Value * 10L);
            case DataTypeId.TimestampMs:
                EnsureSuccess(Native.ValueGetTimestampMs(ref _handle, out LbugTimestamp tsMs), typeId);
                return FromTicks(tsMs.Value * TimeSpan.TicksPerMillisecond);
            case DataTypeId.TimestampSec:
                EnsureSuccess(Native.ValueGetTimestampSec(ref _handle, out LbugTimestamp tsSec), typeId);
                return FromTicks(tsSec.Value * TimeSpan.TicksPerSecond);
            case DataTypeId.TimestampNs:
                EnsureSuccess(Native.ValueGetTimestampNs(ref _handle, out LbugTimestamp tsNs), typeId);
                return FromTicks(tsNs.Value / 100L);
            case DataTypeId.TimestampTz:
                EnsureSuccess(Native.ValueGetTimestampTz(ref _handle, out LbugTimestamp tsTz), typeId);
                return new DateTimeOffset(FromTicks(tsTz.Value * 10L));
            case DataTypeId.Interval:
                EnsureSuccess(Native.ValueGetInterval(ref _handle, out LbugInterval interval), typeId);
                return new Interval(interval.Months, interval.Days, interval.Micros);
            case DataTypeId.Decimal:
                return ReadDecimal();
            case DataTypeId.Uuid:
                return ReadUuid();
            case DataTypeId.Blob:
                return ReadBlob();
            case DataTypeId.String:
                return GetString();
            case DataTypeId.List:
            case DataTypeId.Array:
                return ReadList();
            case DataTypeId.Struct:
            case DataTypeId.Union:
                return ReadStruct();
            case DataTypeId.Map:
                return ReadMap();
            case DataTypeId.Node:
                return ReadNode();
            case DataTypeId.Rel:
                return ReadRel();
            case DataTypeId.RecursiveRel:
                return ReadRecursiveRel();
            default:
                // ANY / POINTER and any future types: best-effort string form.
                return ToString();
        }
    }

    /// <summary>Reads a <see cref="string"/> value (the value must be of type STRING).</summary>
    public string? GetString()
    {
        ThrowIfDisposed();
        LbugState state = Native.ValueGetString(ref _handle, out IntPtr pointer);
        EnsureSuccess(state, DataTypeId.String);
        return Native.TakeString(pointer);
    }

    /// <inheritdoc />
    public override string? ToString()
    {
        if (_disposed)
        {
            return null;
        }

        return Native.TakeString(Native.ValueToString(ref _handle));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Values returned from a flat tuple are owned by the engine (IsOwnedByCpp != 0), in which
        // case the native destroy is a no-op; values we construct will actually be freed here.
        Native.ValueDestroy(ref _handle);
    }

    private DataTypeId GetDataTypeId()
    {
        Native.ValueGetDataType(ref _handle, out LbugLogicalType logicalType);
        try
        {
            return (DataTypeId)Native.DataTypeGetId(ref logicalType);
        }
        finally
        {
            Native.DataTypeDestroy(ref logicalType);
        }
    }

    private InternalId ReadInternalId()
    {
        EnsureSuccess(Native.ValueGetInternalId(ref _handle, out LbugInternalId raw), DataTypeId.InternalId);
        return new InternalId(raw.TableId, raw.Offset);
    }

    private object ReadDecimal()
    {
        EnsureSuccess(Native.ValueGetDecimalAsString(ref _handle, out IntPtr pointer), DataTypeId.Decimal);
        string text = Native.TakeString(pointer) ?? "0";
        return decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal value)
            ? value
            : text;
    }

    private object ReadUuid()
    {
        EnsureSuccess(Native.ValueGetUuid(ref _handle, out IntPtr pointer), DataTypeId.Uuid);
        string text = Native.TakeString(pointer) ?? string.Empty;
        return Guid.TryParse(text, out Guid guid) ? guid : text;
    }

    private byte[] ReadBlob()
    {
        EnsureSuccess(Native.ValueGetBlob(ref _handle, out IntPtr pointer, out ulong length), DataTypeId.Blob);
        if (pointer == IntPtr.Zero)
        {
            return Array.Empty<byte>();
        }

        try
        {
            if (length == 0)
            {
                return Array.Empty<byte>();
            }

            byte[] bytes = new byte[length];
            Marshal.Copy(pointer, bytes, 0, checked((int)length));
            return bytes;
        }
        finally
        {
            Native.DestroyBlob(pointer);
        }
    }

    private object?[] ReadList()
    {
        EnsureSuccess(Native.ValueGetListSize(ref _handle, out ulong size), DataTypeId.List);
        var items = new object?[size];
        for (ulong i = 0; i < size; i++)
        {
            EnsureSuccess(Native.ValueGetListElement(ref _handle, i, out LbugValue elementHandle), DataTypeId.List);
            using var element = new Value(elementHandle);
            items[i] = element.GetValue();
        }

        return items;
    }

    private Dictionary<string, object?> ReadStruct()
    {
        EnsureSuccess(Native.ValueGetStructNumFields(ref _handle, out ulong count), DataTypeId.Struct);
        var result = new Dictionary<string, object?>(checked((int)count));
        for (ulong i = 0; i < count; i++)
        {
            EnsureSuccess(Native.ValueGetStructFieldName(ref _handle, i, out IntPtr namePointer), DataTypeId.Struct);
            string name = Native.TakeString(namePointer) ?? string.Empty;

            EnsureSuccess(Native.ValueGetStructFieldValue(ref _handle, i, out LbugValue fieldHandle), DataTypeId.Struct);
            using var fieldValue = new Value(fieldHandle);
            result[name] = fieldValue.GetValue();
        }

        return result;
    }

    private Dictionary<object, object?> ReadMap()
    {
        EnsureSuccess(Native.ValueGetMapSize(ref _handle, out ulong size), DataTypeId.Map);
        var map = new Dictionary<object, object?>(checked((int)size));
        for (ulong i = 0; i < size; i++)
        {
            EnsureSuccess(Native.ValueGetMapKey(ref _handle, i, out LbugValue keyHandle), DataTypeId.Map);
            object? key;
            using (var keyValue = new Value(keyHandle))
            {
                key = keyValue.GetValue();
            }

            EnsureSuccess(Native.ValueGetMapValue(ref _handle, i, out LbugValue valueHandle), DataTypeId.Map);
            object? value;
            using (var mapValue = new Value(valueHandle))
            {
                value = mapValue.GetValue();
            }

            if (key is not null)
            {
                map[key] = value;
            }
        }

        return map;
    }

    private Node ReadNode()
    {
        EnsureSuccess(Native.NodeValGetIdVal(ref _handle, out LbugValue idHandle), DataTypeId.Node);
        InternalId id;
        using (var idValue = new Value(idHandle))
        {
            id = idValue.ReadInternalId();
        }

        EnsureSuccess(Native.NodeValGetLabelVal(ref _handle, out LbugValue labelHandle), DataTypeId.Node);
        string label;
        using (var labelValue = new Value(labelHandle))
        {
            label = labelValue.GetString() ?? string.Empty;
        }

        EnsureSuccess(Native.NodeValGetPropertySize(ref _handle, out ulong count), DataTypeId.Node);
        var properties = new Dictionary<string, object?>(checked((int)count));
        for (ulong i = 0; i < count; i++)
        {
            EnsureSuccess(Native.NodeValGetPropertyNameAt(ref _handle, i, out IntPtr namePointer), DataTypeId.Node);
            string name = Native.TakeString(namePointer) ?? string.Empty;

            EnsureSuccess(Native.NodeValGetPropertyValueAt(ref _handle, i, out LbugValue propertyHandle), DataTypeId.Node);
            using var propertyValue = new Value(propertyHandle);
            properties[name] = propertyValue.GetValue();
        }

        return new Node(id, label, properties);
    }

    private Rel ReadRel()
    {
        EnsureSuccess(Native.RelValGetIdVal(ref _handle, out LbugValue idHandle), DataTypeId.Rel);
        InternalId id;
        using (var idValue = new Value(idHandle))
        {
            id = idValue.ReadInternalId();
        }

        EnsureSuccess(Native.RelValGetSrcIdVal(ref _handle, out LbugValue srcHandle), DataTypeId.Rel);
        InternalId source;
        using (var srcValue = new Value(srcHandle))
        {
            source = srcValue.ReadInternalId();
        }

        EnsureSuccess(Native.RelValGetDstIdVal(ref _handle, out LbugValue dstHandle), DataTypeId.Rel);
        InternalId destination;
        using (var dstValue = new Value(dstHandle))
        {
            destination = dstValue.ReadInternalId();
        }

        EnsureSuccess(Native.RelValGetLabelVal(ref _handle, out LbugValue labelHandle), DataTypeId.Rel);
        string label;
        using (var labelValue = new Value(labelHandle))
        {
            label = labelValue.GetString() ?? string.Empty;
        }

        EnsureSuccess(Native.RelValGetPropertySize(ref _handle, out ulong count), DataTypeId.Rel);
        var properties = new Dictionary<string, object?>(checked((int)count));
        for (ulong i = 0; i < count; i++)
        {
            EnsureSuccess(Native.RelValGetPropertyNameAt(ref _handle, i, out IntPtr namePointer), DataTypeId.Rel);
            string name = Native.TakeString(namePointer) ?? string.Empty;

            EnsureSuccess(Native.RelValGetPropertyValueAt(ref _handle, i, out LbugValue propertyHandle), DataTypeId.Rel);
            using var propertyValue = new Value(propertyHandle);
            properties[name] = propertyValue.GetValue();
        }

        return new Rel(id, source, destination, label, properties);
    }

    private RecursiveRel ReadRecursiveRel()
    {
        EnsureSuccess(Native.ValueGetRecursiveRelNodeList(ref _handle, out LbugValue nodesHandle), DataTypeId.RecursiveRel);
        Node[] nodes;
        using (var nodesValue = new Value(nodesHandle))
        {
            object?[] rawNodes = nodesValue.ReadList();
            nodes = new Node[rawNodes.Length];
            for (int i = 0; i < rawNodes.Length; i++)
            {
                nodes[i] = (Node)rawNodes[i]!;
            }
        }

        EnsureSuccess(Native.ValueGetRecursiveRelRelList(ref _handle, out LbugValue relsHandle), DataTypeId.RecursiveRel);
        Rel[] rels;
        using (var relsValue = new Value(relsHandle))
        {
            object?[] rawRels = relsValue.ReadList();
            rels = new Rel[rawRels.Length];
            for (int i = 0; i < rawRels.Length; i++)
            {
                rels[i] = (Rel)rawRels[i]!;
            }
        }

        return new RecursiveRel(nodes, rels);
    }

    private static DateTime FromTicks(long ticksSinceEpoch) => UnixEpochUtc.AddTicks(ticksSinceEpoch);

#if NET7_0_OR_GREATER
    private static object FromDays(int days) => DateOnly.FromDateTime(UnixEpochUtc).AddDays(days);

    private static object FromInt128(LbugInt128 value) => new Int128((ulong)value.High, value.Low);
#else
    private static object FromDays(int days) => UnixEpochUtc.AddDays(days);

    private static object FromInt128(LbugInt128 value)
        => (new System.Numerics.BigInteger(value.High) << 64) + value.Low;
#endif

    private static void EnsureSuccess(LbugState state, DataTypeId typeId)
    {
        if (state != LbugState.Success)
        {
            throw new LadybugException($"Failed to read a {typeId} value.");
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(Value));
        }
    }
}
