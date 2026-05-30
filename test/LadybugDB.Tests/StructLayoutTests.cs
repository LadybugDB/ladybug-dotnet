using System.Runtime.InteropServices;
using LadybugDB.Interop;
using Xunit;

namespace LadybugDB.Tests;

/// <summary>
/// ABI guard tests for the managed mirrors of the C structs in <c>src/include/c_api/lbug.h</c>.
/// They assert size and field offsets so accidental field reordering, type widening, or padding
/// mistakes are caught at build time rather than as <see cref="System.AccessViolationException"/>s
/// at the native boundary. These run without the native library because they only inspect the
/// managed layout.
/// </summary>
public sealed class StructLayoutTests
{
    // Structs whose size is independent of the pointer width.
    [Theory]
    [InlineData(typeof(LbugInternalId), 16)]   // uint64 table_id + uint64 offset
    [InlineData(typeof(LbugInt128), 16)]       // uint64 low + int64 high
    [InlineData(typeof(LbugDate), 4)]          // int32 days
    [InlineData(typeof(LbugTimestamp), 8)]     // int64 value (shared by all precisions)
    [InlineData(typeof(LbugInterval), 16)]     // int32 months + int32 days + int64 micros
    [InlineData(typeof(LbugSystemConfig), 56)] // matches sizeof(lbug_system_config) on 64-bit ABIs
    public void FixedSizeStruct_MatchesNativeSize(Type type, int expectedSize)
    {
        Assert.Equal(expectedSize, Marshal.SizeOf(type));
    }

    // Opaque single-pointer handles: { void* }.
    [Theory]
    [InlineData(typeof(LbugDatabase))]
    [InlineData(typeof(LbugConnection))]
    [InlineData(typeof(LbugLogicalType))]
    public void SinglePointerHandle_IsOnePointerWide(Type type)
    {
        Assert.Equal(IntPtr.Size, Marshal.SizeOf(type));
    }

    [Fact]
    public void PreparedStatement_IsTwoPointersWide()
    {
        // { void* _prepared_statement; void* _bound_values; }
        Assert.Equal(IntPtr.Size * 2, Marshal.SizeOf<LbugPreparedStatement>());
    }

    // { void*; bool _is_owned_by_cpp; } — the trailing byte is padded out to pointer alignment.
    [Theory]
    [InlineData(typeof(LbugQueryResult))]
    [InlineData(typeof(LbugFlatTuple))]
    [InlineData(typeof(LbugValue))]
    public void PointerPlusOwnershipFlag_HasPaddedLayout(Type type)
    {
        Assert.Equal(IntPtr.Size * 2, Marshal.SizeOf(type));
    }

    [Fact]
    public void Value_OwnershipFlag_FollowsThePointer()
    {
        Assert.Equal(0, (int)Marshal.OffsetOf<LbugValue>(nameof(LbugValue.Value)));
        Assert.Equal(IntPtr.Size, (int)Marshal.OffsetOf<LbugValue>(nameof(LbugValue.IsOwnedByCpp)));
    }

    [Fact]
    public void Int128_HighFollowsLow()
    {
        Assert.Equal(0, (int)Marshal.OffsetOf<LbugInt128>(nameof(LbugInt128.Low)));
        Assert.Equal(8, (int)Marshal.OffsetOf<LbugInt128>(nameof(LbugInt128.High)));
    }

    [Fact]
    public void Interval_FieldsAreContiguous()
    {
        Assert.Equal(0, (int)Marshal.OffsetOf<LbugInterval>(nameof(LbugInterval.Months)));
        Assert.Equal(4, (int)Marshal.OffsetOf<LbugInterval>(nameof(LbugInterval.Days)));
        Assert.Equal(8, (int)Marshal.OffsetOf<LbugInterval>(nameof(LbugInterval.Micros)));
    }

    // Every field offset must match lbug_system_config, including the 64-bit alignment padding that
    // the C compiler inserts after each bool that precedes a uint64 field.
    [Fact]
    public void SystemConfig_FieldOffsets_MatchNativeAbi()
    {
        Assert.Equal(0, Offset(nameof(LbugSystemConfig.BufferPoolSize)));
        Assert.Equal(8, Offset(nameof(LbugSystemConfig.MaxNumThreads)));
        Assert.Equal(16, Offset(nameof(LbugSystemConfig.EnableCompression)));
        Assert.Equal(17, Offset(nameof(LbugSystemConfig.ReadOnly)));
        Assert.Equal(24, Offset(nameof(LbugSystemConfig.MaxDbSize)));
        Assert.Equal(32, Offset(nameof(LbugSystemConfig.AutoCheckpoint)));
        Assert.Equal(40, Offset(nameof(LbugSystemConfig.CheckpointThreshold)));
        Assert.Equal(48, Offset(nameof(LbugSystemConfig.ThrowOnWalReplayFailure)));
        Assert.Equal(49, Offset(nameof(LbugSystemConfig.EnableChecksums)));
        Assert.Equal(50, Offset(nameof(LbugSystemConfig.EnableMultiWrites)));
        Assert.Equal(51, Offset(nameof(LbugSystemConfig.EnableDefaultHashIndex)));
        Assert.Equal(52, Offset(nameof(LbugSystemConfig.ThreadQos)));

        static int Offset(string field) => (int)Marshal.OffsetOf<LbugSystemConfig>(field);
    }
}
