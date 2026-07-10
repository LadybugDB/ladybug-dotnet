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

/// <summary>
/// Cross-TFM guard: <c>Native.LibraryImport.cs</c> (net7+) and <c>Native.DllImport.cs</c> (ns2.0)
/// are hand-kept identical. On net10.0 only the LibraryImport partial compiles, so reflection over
/// the loaded assembly cannot see the ns2.0 declarations. Parse both source files and assert that
/// they declare the same set of native entry points. This does not prove signature equivalence.
/// </summary>
public sealed class InteropDeclarationParity
{
    private static readonly System.Text.RegularExpressions.Regex EntryPointPattern =
        new(@"EntryPoint\s*=\s*""(?<ep>[A-Za-z0-9_]+)""");

    private static string InteropDir([System.Runtime.CompilerServices.CallerFilePath] string? thisFile = null)
    {
        string testDir = System.IO.Path.GetDirectoryName(thisFile!)!;
        string root = System.IO.Path.GetFullPath(System.IO.Path.Combine(testDir, "..", ".."));
        return System.IO.Path.Combine(root, "src", "LadybugDB", "Interop");
    }

    private static System.Collections.Generic.HashSet<string> EntryPoints(string fileName)
    {
        string path = System.IO.Path.Combine(InteropDir(), fileName);
        string text = System.IO.File.ReadAllText(path);
        var set = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
        foreach (System.Text.RegularExpressions.Match match in EntryPointPattern.Matches(text))
        {
            set.Add(match.Groups["ep"].Value);
        }

        return set;
    }

    [Fact]
    public void LibraryImport_And_DllImport_DeclareTheSameEntryPoints()
    {
        var libraryImport = EntryPoints("Native.LibraryImport.cs");
        var dllImport = EntryPoints("Native.DllImport.cs");

        var onlyInLibraryImport = new System.Collections.Generic.SortedSet<string>(libraryImport);
        onlyInLibraryImport.ExceptWith(dllImport);
        var onlyInDllImport = new System.Collections.Generic.SortedSet<string>(dllImport);
        onlyInDllImport.ExceptWith(libraryImport);

        Assert.True(
            onlyInLibraryImport.Count == 0 && onlyInDllImport.Count == 0,
            $"Interop declaration drift.\n  Only in LibraryImport: {string.Join(", ", onlyInLibraryImport)}\n  Only in DllImport: {string.Join(", ", onlyInDllImport)}");
    }
}
