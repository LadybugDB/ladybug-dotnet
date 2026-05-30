using System;
using System.Runtime.InteropServices;

namespace LadybugDB.Interop;

/// <summary>Mirror of the C <c>lbug_state</c> enum.</summary>
internal enum LbugState
{
    Success = 0,
    Error = 1,
}

/// <summary>
/// Mirror of the C <c>lbug_data_type_id</c> enum. Values must match
/// <c>src/include/c_api/lbug.h</c> exactly.
/// </summary>
internal enum LbugDataTypeId
{
    Any = 0,
    Node = 10,
    Rel = 11,
    RecursiveRel = 12,
    Serial = 13,
    Bool = 22,
    Int64 = 23,
    Int32 = 24,
    Int16 = 25,
    Int8 = 26,
    UInt64 = 27,
    UInt32 = 28,
    UInt16 = 29,
    UInt8 = 30,
    Int128 = 31,
    Double = 32,
    Float = 33,
    Date = 34,
    Timestamp = 35,
    TimestampSec = 36,
    TimestampMs = 37,
    TimestampNs = 38,
    TimestampTz = 39,
    Interval = 40,
    Decimal = 41,
    InternalId = 42,
    String = 50,
    Blob = 51,
    List = 52,
    Array = 53,
    Struct = 54,
    Map = 55,
    Union = 56,
    Pointer = 58,
    Uuid = 59,
}

/// <summary>
/// Mirror of the C <c>lbug_system_config</c> struct (passed/returned by value).
/// <para>
/// The trailing <see cref="ThreadQos"/> field is macOS-only in C; it is included on every
/// platform so the managed struct is a constant 56 bytes that matches the native ABI everywhere
/// (on non-Apple platforms it occupies what is otherwise tail padding and is ignored).
/// </para>
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct LbugSystemConfig
{
    public ulong BufferPoolSize;
    public ulong MaxNumThreads;
    public byte EnableCompression;
    public byte ReadOnly;
    public ulong MaxDbSize;
    public byte AutoCheckpoint;
    public ulong CheckpointThreshold;
    public byte ThrowOnWalReplayFailure;
    public byte EnableChecksums;
    public byte EnableMultiWrites;
    public byte EnableDefaultHashIndex;
    public uint ThreadQos;
}

[StructLayout(LayoutKind.Sequential)]
internal struct LbugDatabase
{
    public IntPtr Database;
}

[StructLayout(LayoutKind.Sequential)]
internal struct LbugConnection
{
    public IntPtr Connection;
}

[StructLayout(LayoutKind.Sequential)]
internal struct LbugPreparedStatement
{
    public IntPtr PreparedStatement;
    public IntPtr BoundValues;
}

[StructLayout(LayoutKind.Sequential)]
internal struct LbugQueryResult
{
    public IntPtr QueryResult;
    public byte IsOwnedByCpp;
}

[StructLayout(LayoutKind.Sequential)]
internal struct LbugFlatTuple
{
    public IntPtr FlatTuple;
    public byte IsOwnedByCpp;
}

[StructLayout(LayoutKind.Sequential)]
internal struct LbugValue
{
    public IntPtr Value;
    public byte IsOwnedByCpp;
}

[StructLayout(LayoutKind.Sequential)]
internal struct LbugLogicalType
{
    public IntPtr DataType;
}

[StructLayout(LayoutKind.Sequential)]
internal struct LbugInternalId
{
    public ulong TableId;
    public ulong Offset;
}

[StructLayout(LayoutKind.Sequential)]
internal struct LbugInt128
{
    public ulong Low;
    public long High;
}

[StructLayout(LayoutKind.Sequential)]
internal struct LbugDate
{
    public int Days;
}

/// <summary>Shared layout for all timestamp precisions (each stores a single 64-bit count).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct LbugTimestamp
{
    public long Value;
}

[StructLayout(LayoutKind.Sequential)]
internal struct LbugInterval
{
    public int Months;
    public int Days;
    public long Micros;
}
