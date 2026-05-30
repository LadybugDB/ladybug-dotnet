using System.IO.Compression;
using System.Xml.Linq;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Frosting;

namespace LadybugDB.Build.Tasks;

/// <summary>
/// Guards against a silent packaging regression: asserts the managed assemblies, every per-RID native
/// payload, and the meta-package's dependency set are all present in the produced .nupkg files.
/// </summary>
[TaskName("VerifyPackages")]
[IsDependentOn(typeof(PackManagedTask))]
[IsDependentOn(typeof(PackRuntimesTask))]
[IsDependentOn(typeof(PackNativeMetaTask))]
public sealed class VerifyPackagesTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        IReadOnlyDictionary<string, Package> packages = ReadPackages(context.ArtifactsDir);
        List<string> errors = [];

        Require(packages, "LadybugDB", errors, p =>
        {
            RequireFile(p, "lib/net10.0/LadybugDB.dll", errors);
            RequireFile(p, "lib/netstandard2.0/LadybugDB.dll", errors);
        });

        foreach (string rid in context.AllRids)
        {
            string library = BuildContext.NativeAssets[rid].Library;
            Require(packages, $"LadybugDB.Native.{rid}", errors,
                p => RequireFile(p, $"runtimes/{rid}/native/{library}", errors));
        }

        Require(packages, "LadybugDB.Native", errors, p =>
        {
            foreach (string rid in context.AllRids)
            {
                var dep = $"LadybugDB.Native.{rid}";
                if (!p.Dependencies.Contains(dep, StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add($"meta package 'LadybugDB.Native' is missing dependency '{dep}'");
                }
            }
        });

        if (errors.Count > 0)
        {
            throw new CakeException("Package validation failed:" + Environment.NewLine +
                                    string.Join(Environment.NewLine, errors.Select(e => "  - " + e)));
        }

        context.Log.Information($"verified {packages.Count} package(s) in {context.ArtifactsDir}");
    }

    private static void Require(
        IReadOnlyDictionary<string, Package> packages, string id, List<string> errors, Action<Package> check)
    {
        if (packages.TryGetValue(id, out Package? package))
        {
            check(package);
        }
        else
        {
            errors.Add($"expected package '{id}' was not produced");
        }
    }

    private static void RequireFile(Package package, string path, List<string> errors)
    {
        if (!package.Files.Contains(path, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"package '{package.Id}' is missing entry '{path}'");
        }
    }

    private static IReadOnlyDictionary<string, Package> ReadPackages(string artifactsDir)
    {
        var result = new Dictionary<string, Package>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(artifactsDir))
        {
            return result;
        }

        foreach (string path in Directory.GetFiles(artifactsDir, "*.nupkg"))
        {
            if (path.EndsWith(".snupkg", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            using ZipArchive archive = ZipFile.OpenRead(path);
            HashSet<string> files = archive.Entries.Select(e => e.FullName).ToHashSet(StringComparer.OrdinalIgnoreCase);

            ZipArchiveEntry? nuspecEntry = archive.Entries.FirstOrDefault(
                e => e.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase) && !e.FullName.Contains('/'));
            if (nuspecEntry is null)
            {
                continue;
            }

            using Stream stream = nuspecEntry.Open();
            var doc = XDocument.Load(stream);
            string id = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "id")?.Value?.Trim() ?? string.Empty;
            HashSet<string> deps = doc.Descendants()
                .Where(e => e.Name.LocalName == "dependency")
                .Select(e => (string?)e.Attribute("id"))
                .Where(v => !string.IsNullOrEmpty(v))
                .Select(v => v!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (id.Length > 0)
            {
                result[id] = new Package(id, files, deps);
            }
        }

        return result;
    }

    private sealed record Package(string Id, HashSet<string> Files, HashSet<string> Dependencies);
}
