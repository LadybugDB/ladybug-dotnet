# Maintaining LadybugDB for .NET

This document is the durable maintainer guide for the `LadybugDB/ladybug-dotnet` repository. It replaces
the development-era working notes.

## Repository Shape

This is the standalone, hand-written C# binding for the native Ladybug C API. It can be mounted locally
at `tools/csharp_api` inside a `LadybugDB/ladybug` checkout, where the engine source and
`src/include/c_api/lbug.h` are available at `../../`. The repositories advance independently: a binding
release pins a published engine tag rather than a parent-repository submodule commit.

Main areas:

- `src/LadybugDB/` - managed API and P/Invoke interop.
- `test/LadybugDB.Tests/` - ABI guards and native round-trip tests.
- `cake/` - Cake Frosting build, test, native download, pack, and verification pipeline.
- `examples/` - runnable package-consumer examples.
- `lib/runtimes/<rid>/native/` - locally staged native libraries; gitignored.
- `artifacts/` - generated NuGet packages; gitignored.
- `download/` - cached upstream native release assets; gitignored.

## Versioning Policy

All packages in the family share one package version:

- `LadybugDB`
- `LadybugDB.Native`
- `LadybugDB.Native.win-x64`
- `LadybugDB.Native.linux-x64`
- `LadybugDB.Native.linux-arm64`
- `LadybugDB.Native.osx-x64`
- `LadybugDB.Native.osx-arm64`

The first three numeric segments track the native Ladybug engine release. The optional fourth numeric
segment is the .NET binding/package revision for binding-only releases over the same engine.

Examples:

- `0.17.0` - first stable .NET package family for engine `v0.17.0`.
- `0.17.0.1` - binding/package-only release that still uses engine `v0.17.0`.
- `0.17.0.2` - another binding/package-only release over engine `v0.17.0`.
- `0.17.1` - first package family for engine `v0.17.1`.
- `0.18.0-preview.1` - preview package family for a future engine `v0.18.0`.

`version.txt` is the default package-family version source. The build pipeline derives the native engine
tag from the first three numeric package-version segments unless explicitly overridden with
`--engine-version` or `ENGINE_VERSION`.

## Build, Test, Pack

From the repository root:

```powershell
dotnet build LadybugDB.slnx -c Release
dotnet test test/LadybugDB.Tests/LadybugDB.Tests.csproj -c Release
```

Native round-trip tests skip when no native library is staged. To require a native load, set:

```powershell
$env:LADYBUG_REQUIRE_NATIVE = '1'
dotnet test test/LadybugDB.Tests/LadybugDB.Tests.csproj -c Release
```

Use the Cake pipeline for package work:

```powershell
./build.ps1 --target Test
./build.ps1 --target Pack
```

On non-Windows shells:

```bash
./build.sh --target Test
./build.sh --target Pack
```

`Pack` downloads prebuilt native assets from `LadybugDB/ladybug` releases when they are not already staged
under `lib/runtimes/<rid>/native/`, then verifies package contents.

## Local Native Build

When this repository is checked out as `tools/csharp_api` in the main `LadybugDB/ladybug` monorepo, the
Windows helper can build the native shared library from the parent engine tree and run the full suite:

```powershell
pwsh -File scripts/build-native-and-test.ps1
```

Manual Windows recipe, from the main monorepo root with MSVC, CMake, and Ninja available:

```powershell
cmake -B build/release -G Ninja -DCMAKE_BUILD_TYPE=Release `
  -DBUILD_SHELL=OFF -DBUILD_SINGLE_FILE_HEADER=OFF -DBUILD_STATIC_LBUG=OFF -DBUILD_TESTS=OFF `
  -DCMAKE_POLICY_VERSION_MINIMUM=3.5 .
cmake --build build/release --target lbug_shared
Copy-Item build/release/src/lbug_shared.dll tools/csharp_api/lib/runtimes/win-x64/native/ -Force
dotnet test tools/csharp_api/test/LadybugDB.Tests/LadybugDB.Tests.csproj -c Release
```

PowerShell can mangle unquoted `-D` arguments; pass CMake flags as quoted strings or via an explicit
PowerShell array when scripting.

## Adopting an Upstream Engine Release

Use a fresh binding worktree so `lib/` and `download/` cannot contain a native or release archive from
the previous engine. Set `LADYBUG_ENGINE_REPO` to a local `LadybugDB/ladybug` checkout, then fetch and
inspect the old and new release tags:

```powershell
$engine = $env:LADYBUG_ENGINE_REPO
if (-not $engine -or -not (Test-Path (Join-Path $engine 'src/include/c_api/lbug.h'))) {
    throw 'Set LADYBUG_ENGINE_REPO to a LadybugDB/ladybug checkout.'
}
$old = 'v0.18.1'
$new = 'v0.18.2'

git -C $engine fetch origin --tags --prune
gh release view $new --repo LadybugDB/ladybug
git -C $engine diff --exit-code $old $new -- src/include/c_api/
git -C $engine log --oneline "$old..$new" -- src/include/c_api/ src/c_api/
```

The header diff is the ABI gate. An empty diff means no release-driven interop change. A non-empty diff
requires the ABI checklist below, including updates to both declaration files and native-gated tests.
Implementation-only changes can still affect behavior, so review the `src/c_api/` log even when the
header is unchanged.

Confirm that the new release contains every asset named by `cake/BuildContext.cs`:

```powershell
$required = @(
    'liblbug-windows-x86_64.zip',
    'liblbug-linux-x86_64.tar.gz',
    'liblbug-linux-aarch64.tar.gz',
    'liblbug-osx-x86_64.tar.gz',
    'liblbug-osx-arm64.tar.gz'
)
$assets = @(gh release view $new --repo LadybugDB/ladybug --json assets --jq '.assets[].name')
$missing = @($required | Where-Object { $_ -notin $assets })
if ($missing.Count -ne 0) { throw "Missing release assets: $($missing -join ', ')" }
```

Set `version.txt` to the exact stable package version, update active version references in the README
and examples, then validate the real native and the full package family:

```powershell
.\build.ps1 --target Test --engine-version $new
.\build.ps1 --target Pack --package-version ($new.TrimStart('v')) --engine-version $new
```

`Test` must run with native skips disabled. `Pack` must verify the managed package, all five per-RID
native packages, and the native meta-package. Merge through CI before creating and pushing the matching
`vX.Y.Z` binding tag; only a tag push publishes to NuGet.

## Release Flow

1. Follow **Adopting an Upstream Engine Release** when the first three version segments change.
2. Update `version.txt`; for a binding-only release, increment only the optional fourth segment.
3. Run Cake `Test` and `Pack` against the intended engine tag.
4. Merge through CI.
5. Tag the merged package version and push that tag; the tag-triggered workflow publishes.

The release workflow gates on linux-x64 against the real engine, packs the full package family, verifies
contents, and publishes all packages to NuGet through trusted publishing.

Manual `workflow_dispatch` builds and uploads artifacts without publishing. Use it for dry runs.

## ABI Update Checklist

The binding mirrors the C API in `LadybugDB/ladybug` exactly. ABI mistakes can compile cleanly and still
corrupt memory at runtime.

When moving to a new engine release:

1. Compare managed declarations against that release's `src/include/c_api/lbug.h`.
2. Update both interop declaration files:
   - `src/LadybugDB/Interop/Native.LibraryImport.cs`
   - `src/LadybugDB/Interop/Native.DllImport.cs`
3. Update structs/enums in `src/LadybugDB/Interop/NativeTypes.cs`.
4. Update or add ABI guard tests in `test/LadybugDB.Tests/StructLayoutTests.cs`.
5. Stage the matching native library and run the native round-trip tests.

Rules that should not change without deliberate review:

- Calling convention is Cdecl.
- C `bool` is one byte: use `byte` in structs and `[MarshalAs(UnmanagedType.U1)]` on bool returns.
- `lbug_system_config` includes the macOS-only trailing `thread_qos` field so the by-value struct layout
  matches across platforms.
- Native strings/blobs are copied to managed memory and then freed with the matching native destroy
  function.
- Result/tuple/value disposal must respect the native ownership flag.

## Package Family

`LadybugDB` is managed-only. Native libraries ship separately in one package per RID, and
`LadybugDB.Native` is a meta-package that depends on every per-RID native package. Consumers reference:

- `LadybugDB` plus `LadybugDB.Native` for all supported platforms, or
- `LadybugDB` plus one `LadybugDB.Native.<rid>` package for a slim single-platform app.

The shipped RIDs are `win-x64`, `linux-x64`, `linux-arm64`, `osx-x64`, and `osx-arm64`.

## Examples

`examples/` contains two categories:

- Database-usage examples: quickstart, demo graph, prepared statements, and result/value materialization.
  These consume published NuGet packages and share the example package version in
  `examples/Directory.Build.props`.
- `native-loading/`: deployment/package-loading example showing bundled native NuGet vs. system-installed
  native library behavior.

Examples are not currently part of CI because their package-restore behavior depends on a published package
version being available.
