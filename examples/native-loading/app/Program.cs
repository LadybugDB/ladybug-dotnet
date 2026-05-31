using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using LadybugDB;

// Tiny consumer that shows WHERE the native engine is loaded from: it prints the path the OS actually
// mapped for liblbug (from /proc/self/maps) and runs a Cypher round-trip. Exit code 0 means it worked.

Console.WriteLine("== LadybugDB native-loading example ==");
Console.WriteLine($"RID        : {RuntimeInformation.RuntimeIdentifier}");
Console.WriteLine($"OS         : {RuntimeInformation.OSDescription}");
Console.WriteLine($"AppBaseDir : {AppContext.BaseDirectory}");

// What native bits, if any, ship inside the app's own deployment directory?
string[] appNatives = Directory.EnumerateFiles(AppContext.BaseDirectory)
    .Where(p => Path.GetFileName(p).Contains("lbug", StringComparison.OrdinalIgnoreCase))
    .ToArray();
if (appNatives.Length == 0)
{
    Console.WriteLine("AppDirNative: <none> (no liblbug shipped with the app)");
}
else
{
    foreach (string f in appNatives)
    {
        Console.WriteLine($"AppDirNative: {f}");
    }
}

// First touch of the native engine: this forces the DllImport resolver to load liblbug.
string version = LadybugVersion.Version;
ulong storage = LadybugVersion.StorageVersion;
Console.WriteLine($"NativeVersion: {version} (storage {storage})");

PrintProvenance();

// Full create / insert / query round-trip against the engine.
string dbPath = Path.Combine(Path.GetTempPath(), "lbug-exp-" + Guid.NewGuid().ToString("N"));
try
{
    using Database db = new(dbPath);
    using Connection conn = new(db);

    conn.Query("CREATE NODE TABLE Person(name STRING, age INT64, PRIMARY KEY(name))").Dispose();
    conn.Query("CREATE (:Person {name: 'Alice', age: 30})").Dispose();
    conn.Query("CREATE (:Person {name: 'Bob', age: 42})").Dispose();

    using QueryResult result = conn.Query("MATCH (p:Person) RETURN p.name, p.age ORDER BY p.age");
    foreach (object?[] row in result.Rows())
    {
        Console.WriteLine($"  row: {row[0]} -> {row[1]}");
    }
}
finally
{
    try
    {
        if (Directory.Exists(dbPath))
        {
            Directory.Delete(dbPath, recursive: true);
        }
        else if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }
    catch
    {
        // best-effort cleanup
    }
}

Console.WriteLine("RESULT: SUCCESS (native loaded + Cypher round-trip OK)");
return 0;

// Reads /proc/self/maps and prints the absolute path(s) of the mapped liblbug image, tagging
// each as SYSTEM (under /usr or /lib) or BUNDLED (anywhere else, i.e. the app directory).
static void PrintProvenance()
{
    if (!OperatingSystem.IsLinux())
    {
        return;
    }

    try
    {
        List<string> paths = File.ReadAllLines("/proc/self/maps")
            .Where(l => l.Contains("lbug", StringComparison.OrdinalIgnoreCase) && l.Contains('/'))
            .Select(l => l[l.IndexOf('/')..].Trim())
            .Distinct()
            .ToList();

        if (paths.Count == 0)
        {
            Console.WriteLine("LoadedFrom : <no liblbug mapping found in /proc/self/maps>");
            return;
        }

        foreach (string p in paths)
        {
            bool system = p.StartsWith("/usr", StringComparison.Ordinal) ||
                          p.StartsWith("/lib", StringComparison.Ordinal);
            Console.WriteLine($"LoadedFrom : {p}   [{(system ? "SYSTEM" : "BUNDLED/app")}]");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"LoadedFrom : <maps read failed: {ex.Message}>");
    }
}
