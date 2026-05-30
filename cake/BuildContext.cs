using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Cake.Common;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Frosting;
using Path = System.IO.Path;

namespace LadybugDB.Build;

/// <summary>
/// Shared state and paths for the packaging pipeline. Resolves the binding root from the running
/// assembly (independent of the caller's working directory) and stages prebuilt engine libraries.
/// </summary>
public sealed class BuildContext : FrostingContext
{
    /// <summary>RID -> (release asset name, canonical native library file name).</summary>
    public static readonly IReadOnlyDictionary<string, (string Asset, string Library)> NativeAssets =
        new Dictionary<string, (string, string)>(StringComparer.Ordinal)
        {
            ["win-x64"] = ("liblbug-windows-x86_64.zip", "lbug_shared.dll"),
            ["linux-x64"] = ("liblbug-linux-x86_64.tar.gz", "liblbug.so"),
            ["linux-arm64"] = ("liblbug-linux-aarch64.tar.gz", "liblbug.so"),
            ["osx-x64"] = ("liblbug-osx-x86_64.tar.gz", "liblbug.dylib"),
            ["osx-arm64"] = ("liblbug-osx-arm64.tar.gz", "liblbug.dylib"),
        };

    public BuildContext(ICakeContext context) : base(context)
    {
        BuildConfiguration = context.Argument("configuration", "Release");
        Commit = context.Argument("commit", Environment.GetEnvironmentVariable("GITHUB_SHA") ?? string.Empty);

        Root = FindBindingRoot();

        // version.txt at the binding root is the single source of truth for the version (e.g.
        // "0.17.0-alpha.1"): its base drives both the engine release we pull natives from and the
        // package version's base, and its prerelease suffix (alpha.1, alpha.2, ...) is the dev suffix.
        // Bump it there - no code change. Overrides: --engine-version / ENGINE_VERSION pick a different
        // engine release; --prerelease "" cuts a stable build; --package-version sets the version
        // verbatim (the release workflow passes the git tag). 'version' is reserved by the Cake host.
        (string baseVersion, string filePrerelease) = ReadVersion(Root);
        EngineVersion = context.Argument("engine-version",
            Environment.GetEnvironmentVariable("ENGINE_VERSION") ?? $"v{baseVersion}");
        string prerelease = context.Argument("prerelease", filePrerelease);
        string engineBase = EngineVersion.TrimStart('v', 'V');
        Version = context.HasArgument("package-version")
            ? context.Argument<string>("package-version")
            : prerelease.Length == 0 ? engineBase : $"{engineBase}-{prerelease}";

        ManagedProject = Path.Combine(Root, "src", "LadybugDB", "LadybugDB.csproj");
        TestProject = Path.Combine(Root, "test", "LadybugDB.Tests", "LadybugDB.Tests.csproj");
        Solution = Path.Combine(Root, "LadybugDB.slnx");
        NativeDir = Path.Combine(Root, "cake", "native");
        RuntimeProject = Path.Combine(NativeDir, "LadybugDB.Native.Runtime.csproj");
        MetaProject = Path.Combine(NativeDir, "LadybugDB.Native.Meta.csproj");
        MetaNuspecTemplate = Path.Combine(NativeDir, "LadybugDB.Native.nuspec");
        RuntimesStageDir = Path.Combine(Root, "lib", "runtimes");
        ArtifactsDir = Path.Combine(Root, "artifacts");
        DownloadDir = Path.Combine(Root, "download");
    }

    public string BuildConfiguration { get; }
    public string Version { get; }
    public string Commit { get; }
    public string EngineVersion { get; }

    public string Root { get; }
    public string ManagedProject { get; }
    public string TestProject { get; }
    public string Solution { get; }
    public string NativeDir { get; }
    public string RuntimeProject { get; }
    public string MetaProject { get; }
    public string MetaNuspecTemplate { get; }
    public string RuntimesStageDir { get; }
    public string ArtifactsDir { get; }
    public string DownloadDir { get; }

    public IReadOnlyList<string> AllRids { get; } = [.. NativeAssets.Keys];

    public string HostRid { get; } =
        (OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "osx" : "linux")
        + "-"
        + (RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "arm64" : "x64");

    /// <summary>Absolute path the native library for <paramref name="rid"/> is staged to.</summary>
    public string StagedLibraryPath(string rid) =>
        Path.Combine(RuntimesStageDir, rid, "native", NativeAssets[rid].Library);

    /// <summary>
    /// Ensure the prebuilt engine library for <paramref name="rid"/> sits under
    /// lib/runtimes/{rid}/native. No-op when it is already present (e.g. built from source locally);
    /// otherwise downloads the pinned engine release asset and extracts the canonical library out of it.
    /// </summary>
    public void EnsureNativeStaged(string rid)
    {
        if (!NativeAssets.TryGetValue(rid, out (string Asset, string Library) info))
        {
            throw new CakeException($"Unknown RID '{rid}'. Known: {string.Join(", ", AllRids)}.");
        }

        string dest = StagedLibraryPath(rid);
        if (File.Exists(dest))
        {
            Log.Information($"native already staged for {rid}: {dest}");
            return;
        }

        string asset = Path.Combine(DownloadDir, info.Asset);
        if (!File.Exists(asset))
        {
            DownloadEngineAsset(info.Asset);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        if (info.Asset.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            ExtractFromZip(asset, info.Library, dest);
        }
        else
        {
            ExtractFromTarGz(asset, dest);
        }

        Log.Information($"staged {rid} -> {dest}");
    }

    private void DownloadEngineAsset(string asset)
    {
        Directory.CreateDirectory(DownloadDir);
        Log.Information($"downloading {asset} from LadybugDB/ladybug@{EngineVersion}");

        var args = new ProcessArgumentBuilder()
            .Append("release").Append("download").AppendQuoted(EngineVersion)
            .Append("--repo").Append("LadybugDB/ladybug")
            .Append("--dir").AppendQuoted(DownloadDir)
            .Append("--pattern").AppendQuoted(asset);

        int exit;
        try
        {
            exit = this.StartProcess("gh", new ProcessSettings { Arguments = args });
        }
        catch (Exception ex)
        {
            throw new CakeException(
                $"Could not run 'gh' to download '{asset}'. Install the GitHub CLI and authenticate, " +
                $"or stage the native library under lib/runtimes/<rid>/native/ manually. ({ex.Message})");
        }

        if (exit != 0 || !File.Exists(Path.Combine(DownloadDir, asset)))
        {
            throw new CakeException($"gh release download failed for '{asset}' (exit {exit}).");
        }
    }

    private static void ExtractFromZip(string archive, string library, string dest)
    {
        using ZipArchive zip = ZipFile.OpenRead(archive);
        ZipArchiveEntry entry = zip.Entries.FirstOrDefault(e =>
                                    string.Equals(Path.GetFileName(e.FullName), library,
                                        StringComparison.OrdinalIgnoreCase))
                                ?? throw new CakeException($"'{library}' not found in '{Path.GetFileName(archive)}'.");
        entry.ExtractToFile(dest, overwrite: true);
    }

    // The Linux/macOS tarballs ship a symlink chain (liblbug.so -> .so.0 -> .so.0.x.y). NuGet does not
    // preserve symlinks and the binding loads by path, so copy the real shared object out under the
    // canonical name: the largest regular file in the "liblbug" family with the platform extension.
    private static void ExtractFromTarGz(string archive, string dest)
    {
        string ext = dest.EndsWith(".dylib", StringComparison.OrdinalIgnoreCase) ? ".dylib" : ".so";
        long bestLength = -1;
        byte[]? best = null;

        using FileStream fs = File.OpenRead(archive);
        using GZipStream gz = new(fs, CompressionMode.Decompress);
        using TarReader tar = new(gz);
        while (tar.GetNextEntry() is { } entry)
        {
            if (entry.EntryType is not (TarEntryType.RegularFile or TarEntryType.V7RegularFile) ||
                entry.DataStream is null)
            {
                continue;
            }

            string name = Path.GetFileName(entry.Name);
            if (!name.StartsWith("liblbug", StringComparison.OrdinalIgnoreCase) ||
                !name.Contains(ext, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            using MemoryStream ms = new();
            entry.DataStream.CopyTo(ms);
            if (ms.Length > bestLength)
            {
                bestLength = ms.Length;
                best = ms.ToArray();
            }
        }

        if (best is null)
        {
            throw new CakeException($"No 'liblbug*{ext}' regular file found in '{Path.GetFileName(archive)}'.");
        }

        File.WriteAllBytes(dest, best);
    }

    /// <summary>
    /// Reads the binding's version from <c>version.txt</c> at the root - the single source of truth for
    /// the package version (e.g. "0.17.0-alpha.1"). Returns the base ("0.17.0") and the prerelease
    /// suffix ("alpha.1", or "" when stable). Bump it there to advance the alpha or the engine; no code change.
    /// </summary>
    private static (string Base, string Prerelease) ReadVersion(string root)
    {
        string path = Path.Combine(root, "version.txt");
        if (!File.Exists(path))
        {
            throw new CakeException(
                $"Version file not found at '{path}'. " +
                $"It is the single source of truth for the package version (e.g. 0.17.0-alpha.1).");
        }

        string raw = File.ReadAllText(path).Trim();
        int dash = raw.IndexOf('-');
        return dash < 0 ? (raw, string.Empty) : (raw[..dash], raw[(dash + 1)..]);
    }

    private static string FindBindingRoot()
    {
        for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "LadybugDB.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new CakeException("Could not locate the binding root (no LadybugDB.slnx found above the build output).");
    }

    /// <summary>Console entry point. Cake resolves the task graph and constructs this context via DI.</summary>
    public static int Main(string[] args) => new CakeHost()
        .UseContext<BuildContext>()
        .Run(args);
}
