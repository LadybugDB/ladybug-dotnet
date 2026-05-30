using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using Cake.Common.Tools.DotNet.MSBuild;
using Cake.Common.Tools.DotNet.Pack;
using Cake.Common.Tools.DotNet.Test;
using Cake.Core.Diagnostics;
using Cake.Frosting;

namespace LadybugDB.Build.Tasks;

// The packaging pipeline, in run order. Cake Frosting discovers tasks by attribute, so these small
// tasks live together here; the only one big enough to warrant its own file is VerifyPackages.

[TaskName("Clean")]
public sealed class CleanTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        // The build project's own bin/obj are intentionally left alone (it is running from there).
        string[] areas = ["src", "test", Path.Combine("cake", "native")];
        foreach (string area in areas)
        {
            context.CleanDirectories($"{context.Root}/{area.Replace('\\', '/')}/**/bin");
            context.CleanDirectories($"{context.Root}/{area.Replace('\\', '/')}/**/obj");
        }

        context.EnsureDirectoryExists(context.ArtifactsDir);
        context.CleanDirectory(context.ArtifactsDir);
    }
}

[TaskName("Restore")]
public sealed class RestoreTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) => context.DotNetRestore(context.Solution);
}

[TaskName("BuildManaged")]
[IsDependentOn(typeof(RestoreTask))]
public sealed class BuildManagedTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) =>
        context.DotNetBuild(context.ManagedProject, new DotNetBuildSettings
        {
            Configuration = context.BuildConfiguration,
            NoRestore = true,
        });
}

/// <summary>
/// Stages the prebuilt engine library for the host RID so the suite can exercise the native
/// round-trips. Skips quietly when the host platform has no shipped RID.
/// </summary>
[TaskName("FetchNatives")]
public sealed class FetchNativesTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        if (BuildContext.NativeAssets.ContainsKey(context.HostRid))
        {
            context.EnsureNativeStaged(context.HostRid);
        }
        else
        {
            context.Log.Warning($"no prebuilt native shipped for host RID '{context.HostRid}'; native round-trips will skip");
        }
    }
}

[TaskName("Test")]
[IsDependentOn(typeof(BuildManagedTask))]
[IsDependentOn(typeof(FetchNativesTask))]
public sealed class TestTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        DotNetTestSettings settings = new() { Configuration = context.BuildConfiguration };

        // When the host native is present, fail (don't skip) if it doesn't actually load.
        if (BuildContext.NativeAssets.ContainsKey(context.HostRid) &&
            File.Exists(context.StagedLibraryPath(context.HostRid)))
        {
            settings.EnvironmentVariables = new Dictionary<string, string> { ["LADYBUG_REQUIRE_NATIVE"] = "1" };
        }

        context.DotNetTest(context.TestProject, settings);
    }
}

/// <summary>Default target: build the binding and run the suite.</summary>
[TaskName("Default")]
[IsDependentOn(typeof(TestTask))]
public sealed class DefaultTask : FrostingTask<BuildContext>;

/// <summary>Packs the managed-only LadybugDB package (no native payload).</summary>
[TaskName("PackManaged")]
[IsDependentOn(typeof(BuildManagedTask))]
public sealed class PackManagedTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var msbuild = new DotNetMSBuildSettings()
            .WithProperty("Version", context.Version)
            .WithProperty("ContinuousIntegrationBuild", "true");

        if (!string.IsNullOrEmpty(context.Commit))
        {
            msbuild.WithProperty("RepositoryCommit", context.Commit);
        }

        context.DotNetPack(context.ManagedProject, new DotNetPackSettings
        {
            Configuration = context.BuildConfiguration,
            OutputDirectory = context.ArtifactsDir,
            MSBuildSettings = msbuild,
        });
    }
}

/// <summary>
/// Stages and packs one native package per shipped RID (LadybugDB.Native.&lt;rid&gt;) from the single
/// runtime packaging template.
/// </summary>
[TaskName("PackRuntimes")]
public sealed class PackRuntimesTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        foreach (string rid in context.AllRids)
        {
            context.EnsureNativeStaged(rid);

            var msbuild = new DotNetMSBuildSettings()
                .WithProperty("Version", context.Version)
                .WithProperty("NativeRid", rid)
                .WithProperty("PackageId", $"LadybugDB.Native.{rid}");

            if (!string.IsNullOrEmpty(context.Commit))
            {
                msbuild.WithProperty("RepositoryCommit", context.Commit);
            }

            context.DotNetPack(context.RuntimeProject, new DotNetPackSettings
            {
                Configuration = context.BuildConfiguration,
                OutputDirectory = context.ArtifactsDir,
                MSBuildSettings = msbuild,
            });
        }
    }
}

/// <summary>
/// Packs the LadybugDB.Native meta-package: no payload, depends on every per-RID native package.
/// The committed nuspec is a template; version/commit tokens are substituted here so we never pass
/// semicolon-laden NuspecProperties through MSBuild.
/// </summary>
[TaskName("PackNativeMeta")]
[IsDependentOn(typeof(PackRuntimesTask))]
public sealed class PackNativeMetaTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        string nuspec = File.ReadAllText(context.MetaNuspecTemplate)
            .Replace("$version$", context.Version)
            .Replace("$commit$", context.Commit);

        string generatedDir = Path.Combine(context.NativeDir, "obj");
        Directory.CreateDirectory(generatedDir);
        string generated = Path.Combine(generatedDir, "LadybugDB.Native.generated.nuspec");
        File.WriteAllText(generated, nuspec);

        var msbuild = new DotNetMSBuildSettings()
            .WithProperty("NuspecFile", generated)
            .WithProperty("NuspecBasePath", context.NativeDir);

        context.DotNetPack(context.MetaProject, new DotNetPackSettings
        {
            Configuration = context.BuildConfiguration,
            OutputDirectory = context.ArtifactsDir,
            MSBuildSettings = msbuild,
        });
    }
}

/// <summary>Aggregate: produce and validate the full package family (managed + per-RID native + meta).</summary>
[TaskName("Pack")]
[IsDependentOn(typeof(VerifyPackagesTask))]
public sealed class PackTask : FrostingTask<BuildContext>;
