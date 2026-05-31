# Handoff / Transfer Notes - Ladybug C# Binding

Current state, how to build/test, and immediate next steps. Keep this current whenever work pauses.

## Current state
- Standalone repo in the LadybugDB org: `LadybugDB/ladybug-dotnet`. Developed as the `tools/csharp_api`
  submodule of `LadybugDB/ladybug` (mirrors `tools/rust_api`, `tools/java_api`). The submodule is wired
  in the monorepo LOCALLY ONLY here (the monorepo gitlink is not pushed upstream yet).
- Phases 0-3 implemented. The solution builds for BOTH target frameworks (`net10.0` + `netstandard2.0`),
  and `dotnet pack` produces a valid package.
- END-TO-END VALIDATED on Windows x64: native `lbug_shared.dll` built locally with MSVC + Ninja and
  the full xUnit suite passes (28/28, 0 skipped) against the real engine - including from the submodule
  checkout, which builds the parent engine via `../../`.
- The C# project does NOT build native code. It P/Invokes the Ladybug C API (`lbug.h`, in the parent
  `LadybugDB/ladybug` repo) exported from the `lbug_shared` target.

## What works (verified)
- Interop: `LibraryImport` (net7+) / `DllImport` (ns2.0) for ~90 C API functions, resolver, UTF-8 helpers.
- API: `Database`, `Connection`, `QueryResult`, `FlatTuple`, `Value` (full type system),
  `PreparedStatement` (typed + object binding), `LadybugVersion`, graph types, `SystemConfig`.
- 28 xUnit tests: 17 ABI/struct-layout guards + 10 native round-trip tests (smoke, type-mapping,
  prepared statements) + 1 native-required gate, all PASSING against `lbug_shared.dll`.

## Local toolchain on this machine (verified 2026-05-29)
- Visual Studio Community 2026 (v18.6): MSVC `14.51.36231` (`cl.exe` x64) + Windows SDK `10.0.26100`.
- LLVM clang `22.1.6` at `C:\Program Files\LLVM\bin` (alternative compiler; not used for the build below).
- CMake `4.3.2` + Ninja `1.13.0` installed via `pip install --user cmake ninja` (in the Python
  user Scripts dir; NOT on the default PATH).

## Build the native lib + validate the wrapper (verified recipe)
The native build needs the MSVC environment (cl/INCLUDE/LIB) and cmake/ninja on PATH. The dotnet/C#
build does NOT need MSVC. Easiest path is the helper script:
```powershell
# from tools/csharp_api  — builds lbug_shared.dll, copies it into lib/runtimes/win-x64/native, runs tests
pwsh -File scripts/build-native-and-test.ps1
```
What it does (and the manual equivalent):
```powershell
# 1. Enter the MSVC x64 env (imports vcvars64.bat) and put pip's cmake/ninja on PATH
#    (see scripts/build-native-and-test.ps1 for the exact env import)
# 2. Configure once (CMake 4.x needs the policy floor for the vendored deps):
cmake -B build/release -G Ninja -DCMAKE_BUILD_TYPE=Release `
  -DBUILD_SHELL=OFF -DBUILD_SINGLE_FILE_HEADER=OFF -DBUILD_STATIC_LBUG=OFF -DBUILD_TESTS=OFF `
  -DCMAKE_POLICY_VERSION_MINIMUM=3.5 .
# 3. Build just the C-API shared lib (~8 min, 1046 steps -> build/release/src/lbug_shared.dll, 18 MB):
cmake --build build/release --target lbug_shared
# 4. Wire it into the binding and run the suite:
Copy-Item build/release/src/lbug_shared.dll tools/csharp_api/lib/runtimes/win-x64/native/ -Force
dotnet test tools/csharp_api/test/LadybugDB.Tests/LadybugDB.Tests.csproj
```

## Build / test / pack via the Cake Frosting pipeline (`cake/`)
All packaging is driven by the Cake Frosting build project under `cake/` (don't hand-run `dotnet pack`).
Use the `build.ps1` / `build.sh` bootstrap from `tools/csharp_api`:
```powershell
./build.ps1 --target Test     # build both TFMs + stage host native + run suite
./build.ps1 --target Pack     # full package family -> ./artifacts, verified
```
- Versioning: the package version tracks the upstream engine version (`v0.17.0` -> `0.17.0`) with an
  `-alpha.N` prerelease suffix while in development, so the default is `0.17.0-alpha.1`. It lives in ONE
  place - `version.txt` at the binding root - which `BuildContext`, `nuget-package.props`, and both
  workflows all read; bump the alpha (or the engine) there with no code change. Overrides: `--prerelease
  alpha.2` (suffix), `--package-version <v>` (exact; the release workflow passes the git tag), and
  `--prerelease ""` (stable build equal to the engine version).
- The binding now ships a FAMILY (see DECISIONS D18): managed-only `LadybugDB`, one
  `LadybugDB.Native.<rid>` per RID, and the `LadybugDB.Native` meta-package. `Pack` stages every RID's
  native (downloading from the pinned engine release when not already present), packs all 7, and
  `VerifyPackages` asserts contents.
- VERIFIED locally end-to-end (2026-05-30, win-x64 host, engine `v0.17.0`): `--target Pack` produced
  and verified all 7 packages; `--target Test` passed 28/28 with the native loaded. Confirmed layouts:
  `LadybugDB` = `lib/net10.0` + `lib/netstandard2.0` (with XML docs) + `README.md`, no `runtimes/` and
  zero dependencies; `LadybugDB.Native.<rid>` = `runtimes/<rid>/native/<lib>` + `lib/netstandard2.0/_._`
  and no dependencies; `LadybugDB.Native` = `_._` + dependencies on all five per-RID packages.
- Cake arg notes: the package version is `--package-version` (the host reserves `--version`);
  `--engine-version` overrides the pinned engine; `--commit` (or `GITHUB_SHA`) stamps the repository
  metadata. Packages land in `tools/csharp_api/artifacts/` (gitignored).

## Gotchas
- PowerShell mangles `-DCMAKE_POLICY_VERSION_MINIMUM=3.5` into `=3` when args aren't quoted; pass cmake
  `-D` flags via an explicit `@()` array (the helper script does this).
- Importing `vcvars64.bat` sets `Platform=x64`, which makes subsequent `dotnet` builds output to
  `bin/x64/...`. Harmless; tests still find the native lib. Use a fresh shell for pure C# work.
- The DLL links the dynamic MSVC CRT (`vcruntime140`, `msvcp140`, `ucrtbase`); present with VS here,
  needs the VC++ redistributable on clean machines.

## Layout
```
tools/csharp_api/
  Directory.Build.props          # shared build props (LangVersion, OS/arch detection, paths)
  nuget/nuget-package.props      # managed package metadata (managed-only; natives ship separately)
  LadybugDB.slnx
  build.ps1 / build.sh           # bootstrap for the Cake Frosting pipeline
  cake/                          # Cake Frosting packaging pipeline (NOT in LadybugDB.slnx)
    LadybugDB.Build.csproj       # the build console app (Cake.Frosting)
    BuildContext.cs              # Main entry point + RID set, paths, EnsureNativeStaged()
    Tasks/                       # Clean, Restore, BuildManaged, Test, FetchNatives,
                                 #   PackManaged, PackRuntimes, PackNativeMeta, VerifyPackages, Pack, Default
    common.props                 # shared NuGet metadata for the native packages
    native/                      # native packaging assets
      LadybugDB.Native.Runtime.csproj  # one template, packed once per RID
      LadybugDB.Native.Meta.csproj     # thin host to pack the meta nuspec via dotnet
      LadybugDB.Native.nuspec          # meta-package template ($version$/$commit$ tokens)
      _._                              # empty lib marker
  src/LadybugDB/                 # the binding (multi-target net10.0 + netstandard2.0)
    Interop/                     # Native (P/Invoke), per-TFM marshaling, structs, resolver
    *.cs                         # Database, Connection, QueryResult, FlatTuple, Value, ...
  test/LadybugDB.Tests/          # xUnit tests (net10.0)
  lib/runtimes/<rid>/native/     # native libs are staged here for packaging/tests (gitignored)
  artifacts/                     # produced .nupkg/.snupkg (gitignored)
  download/                      # cached engine release assets (gitignored)
  .agents/notes/                 # DECISIONS / HANDOFF / ROADMAP
```

## Native library requirement
- Tests and any real use need the native shared library at runtime, named per-OS:
  - Windows: `lbug_shared.dll`  ·  Linux: `liblbug.so`  ·  macOS: `liblbug.dylib`
- The test project's `PlaceNativeLibrary` target copies `lib/runtimes/<rid>/native/*` next to the test
  output, where the DllImport resolver finds it. `lib/` is gitignored, so the DLL is a local artifact.
- Tests SKIP (not fail) when the native lib is absent (`TestEnvironment.NativeAvailable`).

## CI / CD (GitHub Actions, in the standalone repo)
Both workflows invoke the Cake pipeline via `dotnet run --project cake/LadybugDB.Build.csproj -- ...`
(natives come from upstream releases; the engine is never built here). `GH_TOKEN` is set so
`FetchNatives` can download the prebuilt assets.
- `.github/workflows/ci.yml` - PR/push validation, path-filtered to `src/**`, `test/**`, `cake/**`,
  `nuget/**`, `**/*.props`, `LadybugDB.slnx`. A `test` matrix (linux-x64 + win-x64) runs `--target Test`
  (stages the host native, runs the suite with no skips), and a `pack` job runs `--target Pack`
  (version derived from `version.txt`) to build + verify the whole family without publishing.
- `.github/workflows/release.yml` - the release pipeline. Two jobs:
  1. `pack`: resolves the version (tag `v1.2.3` -> `1.2.3`) and engine version, runs `--target Test`
     as the linux-x64 gate against the real engine, then `--target Pack` (which stages all 5 RIDs, packs
     the 7 packages, and `VerifyPackages` asserts every package's contents). Uploads the artifacts.
  2. `publish` (only on `v*` tags): trusted publishing via `NuGet/login@v1`
     (`id-token: write`) + `dotnet nuget push "artifacts/*.nupkg" --skip-duplicate` - now pushes ALL 7
     packages (the `.snupkg` symbols ride along with the managed package).
- The upstream engine release the natives are taken from defaults to the version's base (from
  `version.txt`, or the `v*` tag on release); override per-run with `--engine-version` / `ENGINE_VERSION`
  or the `engine_version` dispatch input. Keep it in sync with the managed ABI.

### Releasing a version
```bash
git tag v0.1.0            # tag drives the package version (v1.2.3 -> 1.2.3)
git push origin v0.1.0
```
`workflow_dispatch` (with a `version` input) builds + packs + uploads the artifacts WITHOUT publishing -
use it to dry-run the full family build before tagging.

### One-time setup before the first publish (MAINTAINER ACTION)
- nuget.org -> Account -> Trusted Publishing -> Add a policy for EACH package id (the publish job pushes
  all 7): `LadybugDB`, `LadybugDB.Native`, and `LadybugDB.Native.{win-x64, linux-x64, linux-arm64,
  osx-x64, osx-arm64}`. owner=`LadybugDB`, repo=`ladybug-dotnet`, workflow file=`release.yml`; leave the
  policy's Environment blank for now (the publish job runs without a GitHub environment).
- Repo Settings -> Secrets and variables -> Actions: add repository secret `NUGET_USER` = the nuget.org
  PROFILE name (not email) that owns/co-owns the package ids. (DONE.) A dedicated `release` environment
  (required reviewers as an approval gate, with the policy's Environment set to match) can be added later
  when we separate release vs test environments.
- All 7 package ids must be owned (or reserved) by that nuget.org account before the first real push;
  until then, use `workflow_dispatch` / local `./build.ps1 --target Pack` as a dry run.

## Next steps
1. Local end-to-end is DONE (2026-05-30): `--target Pack` built + verified all 7 packages and
   `--target Test` passed 28/28 against the engine `v0.17.0` natives on win-x64. Next, run `release.yml`
   via `workflow_dispatch` once to confirm the same on CI (the linux-x64 gate + all-RID download/pack).
2. Complete the nuget.org trusted-publishing policies (one per package id, Environment blank), then
   tag `v*`.
3. Reserve/own all 7 package ids on nuget.org under the publishing account (the repo already lives in the
   LadybugDB org).
4. Phase 3 (remaining): expand the suite to mirror the Java + C API tests over `dataset/tinysnb`.
5. Phase 5 (optional): Native AOT validation, Arrow C Data interface, observability.
