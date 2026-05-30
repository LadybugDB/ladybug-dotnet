using System;
using System.IO;
using LadybugDB;

namespace LadybugDB.Tests;

/// <summary>Shared helpers for the test suite, including native-availability detection.</summary>
internal static class TestEnvironment
{
    /// <summary>
    /// True when the native Ladybug library can be loaded. When false, tests skip rather than fail.
    /// See <c>.agents/notes/HANDOFF.md</c> for how to provide the native library.
    /// </summary>
    public static readonly bool NativeAvailable = Probe();

    public static string NewTempDbPath()
        => Path.Combine(Path.GetTempPath(), "ladybug-csharp-" + Guid.NewGuid().ToString("N"));

    public static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort cleanup.
        }
    }

    private static bool Probe()
    {
        try
        {
            _ = LadybugVersion.StorageVersion;
            return true;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (TypeInitializationException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }
}
