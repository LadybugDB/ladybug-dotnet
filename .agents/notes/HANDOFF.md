# Handoff / Transfer Notes - Ladybug C# Binding

Current state, how to build/test, and immediate next steps. Keep this current whenever work pauses.

## Current state
- Phases 0-3 implemented under `tools/csharp_api/`. The solution builds for BOTH target frameworks
  (`net10.0` + `netstandard2.0`), and `dotnet pack` produces a valid package.
- END-TO-END VALIDATED on Windows x64: native `lbug_shared.dll` built locally with MSVC + Ninja and
  the full xUnit suite passes (27/27, 0 skipped) against the real engine.
- The C# project does NOT build native code. It P/Invokes the Ladybug C API
  (`src/include/c_api/lbug.h`) exported from the `lbug_shared` target.

## What works (verified)
- Interop: `LibraryImport` (net7+) / `DllImport` (ns2.0) for ~90 C API functions, resolver, UTF-8 helpers.
- API: `Database`, `Connection`, `QueryResult`, `FlatTuple`, `Value` (full type system),
  `PreparedStatement` (typed + object binding), `LadybugVersion`, graph types, `SystemConfig`.
- 27 xUnit tests: 17 ABI/struct-layout guards + 10 native round-trip tests (smoke, type-mapping,
  prepared statements) all PASSING against `lbug_shared.dll`.

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

## Managed-only build / pack (no native needed)
```powershell
cd tools/csharp_api
dotnet build LadybugDB.slnx -c Release          # both TFMs, 0 warnings
dotnet pack  src/LadybugDB/LadybugDB.csproj -c Release -p:Version=0.1.0-local
```
- Whatever native libs sit under `lib/runtimes/<rid>/native/` at pack time are bundled into the
  package at `runtimes/<rid>/native/`. With only win-x64 staged, the package is win-x64-only.
- Verified package contents: `lib/net10.0` + `lib/netstandard2.0` (each with XML docs), `README.md`,
  and `runtimes/win-x64/native/lbug_shared.dll`. Zero external dependencies in the nuspec.
- Verified by consuming `LadybugDB.0.1.0-local.nupkg` from a local feed: queries run end-to-end and
  the native DLL flows to the consumer's `bin/.../runtimes/win-x64/native/` automatically.
- Output goes to `tools/csharp_api/packages/` (gitignored).

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
  nuget/nuget-package.props      # package metadata + native runtime packing
  LadybugDB.sln
  src/LadybugDB/                 # the binding (multi-target net10.0 + netstandard2.0)
    Interop/                     # Native (P/Invoke), per-TFM marshaling, structs, resolver
    *.cs                         # Database, Connection, QueryResult, FlatTuple, Value, ...
  test/LadybugDB.Tests/          # xUnit tests (net10.0)
  lib/runtimes/<rid>/native/     # native libs are dropped here for packaging/tests (gitignored)
  .agents/notes/               # DECISIONS / HANDOFF / ROADMAP
```

## Native library requirement
- Tests and any real use need the native shared library at runtime, named per-OS:
  - Windows: `lbug_shared.dll`  ·  Linux: `liblbug.so`  ·  macOS: `liblbug.dylib`
- The test project's `PlaceNativeLibrary` target copies `lib/runtimes/<rid>/native/*` next to the test
  output, where the DllImport resolver finds it. `lib/` is gitignored, so the DLL is a local artifact.
- Tests SKIP (not fail) when the native lib is absent (`TestEnvironment.NativeAvailable`).

## CI / CD (GitHub Actions)
Two workflows at the repo root drive cross-platform packaging + publishing:
- `.github/workflows/csharp-ci.yml` - PR/push validation. Builds both TFMs, runs the managed + ABI
  suite (native round-trips skip without a native lib), and a `dotnet pack` smoke check. Path-filtered
  to `tools/csharp_api/**` + `lbug.h`.
- `.github/workflows/csharp-release.yml` - the release pipeline. Three jobs:
  1. `build-native`: `uses: ./.github/workflows/precompiled-bin-workflow.yml` to build `liblbug` for
     all 5 RIDs (reuses the repo's canonical native builder; see DECISIONS D15).
  2. `pack`: downloads those artifacts, `cp -L`s each shared lib into `lib/runtimes/<rid>/native/`
     (win-x64=`lbug_shared.dll`, linux-x64/arm64=`liblbug.so`, osx-x64/arm64=`liblbug.dylib`),
     runs the FULL suite on linux-x64 against the real engine as a gate, packs with `-p:Version` from
     the tag, then ASSERTS all 5 natives + both managed TFMs are in the `.nupkg`.
  3. `publish` (only on `csharp-v*` tags, `environment: release`): trusted publishing via
     `NuGet/login@v1` (`id-token: write`) + `dotnet nuget push --skip-duplicate`.

### Releasing a version
```bash
git tag csharp-v0.1.0      # tag drives the package version (csharp-v1.2.3 -> 1.2.3)
git push origin csharp-v0.1.0
```
`workflow_dispatch` (with a `version` input) builds + packs + uploads the artifact WITHOUT publishing -
use it to dry-run the cross-platform build before tagging.

### One-time setup before the first publish (USER ACTION)
- nuget.org -> Account -> Trusted Publishing -> Add policy:
  owner=`LadybugDB`, repo=`ladybug`, workflow file=`csharp-release.yml`, environment=`release`.
- Repo Settings -> Environments -> `release`: add secret `NUGET_USER` = your nuget.org PROFILE name
  (not email). Optionally add required reviewers as an approval gate.
- `LadybugDB` package id is unclaimed/permanent once first pushed - verify before tagging.
- Before the first real publish, dry-run: `dotnet pack -c Release -o ./artifacts` (or the dispatch run).

## Next steps
1. Run `csharp-release.yml` via `workflow_dispatch` once to confirm the cross-platform natives build
   and the package assembles+passes the linux-x64 gate (the matrix has only been proven on win-x64
   locally so far).
2. Complete the nuget.org trusted-publishing policy + `release` environment, then tag `csharp-v*`.
3. Phase 3 (remaining): expand the suite to mirror the Java + C API tests over `dataset/tinysnb`.
4. Phase 5 (optional): Native AOT validation, Arrow C Data interface, observability.
