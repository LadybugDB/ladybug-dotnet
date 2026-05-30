# LadybugDB C# binding — agent guide

Standalone repo (`ladybug-dotnet`): a hand-written P/Invoke binding over the Ladybug C API.
Developed as a git submodule mounted at `tools/csharp_api` in the `LadybugDB/ladybug` monorepo
(like `tools/rust_api` / `tools/java_api`), so the C engine and its header (`src/include/c_api/lbug.h`)
live in that parent repo — reachable at `../../` when mounted, NOT in this repo. The managed
enums/structs/signatures mirror that header exactly; if they drift, calls corrupt memory or crash at
runtime, not at compile time.

**Working notes — read first, keep current.** This binding is an in-progress port driven by three living
docs under `.agents/notes/`: `ROADMAP.md` (phase status / what's left), `HANDOFF.md` (current state +
verified build recipe), `DECISIONS.md` (append-only ABI/design/repo-split log). They carry context across
agent sessions — start a task by reading them, and update the relevant one in the same change (full
descriptions under [Deeper docs](#deeper-docs)).

## Boundaries

- **Always:** keep managed signatures, struct layouts, and enum values in lockstep with the upstream
`lbug.h`; stage a native lib and run the FULL test suite before proposing any interop change (ABI
bugs never surface at compile time).
- **Ask first:** changing marshalling, calling convention, or struct layout; bumping the pinned engine
version (the base of `version.txt`); adding/dropping a RID or changing `runtimes/{rid}/native/` packaging.
- **Never:** alter a struct's field order/width, the `byte`-for-C-`bool` rule, or the who-frees-what
string/blob ownership without sign-off. Never add a finalizer or `SafeHandle` to the disposables
(deliberate — see D10). Never commit native binaries: everything under `lib/` is a gitignored artifact.

## Build + test

Managed-only — no native toolchain needed, this is what CI does (run from the repo root, i.e.
`tools/csharp_api/` when mounted as the submodule):

```bash
dotnet build LadybugDB.slnx -c Release # both TFMs: net10.0 + netstandard2.0
dotnet test test/LadybugDB.Tests/LadybugDB.Tests.csproj -c Release
```

The C# build never compiles C; it loads a prebuilt `lbug_shared` shared library at runtime.

Native lib + true end-to-end suite — only when checked out as the monorepo submodule (the script
builds the parent engine via `../../`); Windows, MSVC + Ninja, ~8 min on first build:

```powershell
pwsh -File scripts/build-native-and-test.ps1
```

Builds `lbug_shared.dll`, stages it into `lib/runtimes/win-x64/native/`, and runs the suite. The
manual CMake recipe (other OSes, flags, the `-DCMAKE_POLICY_VERSION_MINIMUM=3.5` floor) is in
`.agents/notes/HANDOFF.md`.

## Tests

- Round-trip/interop tests (`SmokeTests`, `TypeMappingTests`, `PreparedStatementTests`) need the
native lib and **skip** (not fail) when it is absent. Stage a native lib first to exercise them.
- ABI guard tests (`StructLayoutTests`) assert every cross-boundary struct's size/offsets and run
**without** native — `dotnet test ... --filter FullyQualifiedName~StructLayoutTests`. A failure
here means you broke the ABI. Note: these do NOT cover enum values or function signatures.
- Set `LADYBUG_REQUIRE_NATIVE=1` to turn a "native didn't load" skip into a hard failure (the release
CI sets it when gating on the downloaded native lib).

## Interop contract (what silently breaks the ABI)

- **Two declaration files, kept identical:** `Interop/Native.LibraryImport.cs` (net7+,
source-generated `[LibraryImport]`, `StringMarshalling.Utf8`) and `Interop/Native.DllImport.cs`
(netstandard2.0, classic `[DllImport]`, `Cdecl`, manual UTF-8 via `ToUtf8`). Add or change a
function in **both**.
- **Cdecl; x64/arm64 only.** Structs are blittable, `[StructLayout(LayoutKind.Sequential)]`, all in
`Interop/NativeTypes.cs`.
- **C `bool` is 1 byte** → `byte` in structs, `[return: MarshalAs(UnmanagedType.U1)] bool` on
returns. Never the default 4-byte .NET `bool`.
- `**lbug_system_config` is 56 bytes** — always include the macOS-only trailing `thread_qos` so the
by-value struct matches the native ABI on every platform.
- **Memory ownership:** `char`* returns are declared `IntPtr`, copied to a managed string, then freed
via `lbug_destroy_string` (`Native.TakeString`); blobs via `lbug_destroy_blob`. The
`_is_owned_by_cpp` flag on result/tuple/value structs governs disposal — don't free what C owns.
- Strings cross as NUL-terminated UTF-8 both ways. `lbug_state` / `lbug_data_type_id` are int-backed
enums; the data-type-id integer values are non-contiguous and must match the header exactly.

## Bindings are hand-written

There is **no** binding generator (no ClangSharp). "Source-generated" refers only to .NET's built-in
`LibraryImport` marshaller. Edit interop by hand against the upstream `lbug.h`; don't hunt for a regen command.

## Upstream coupling

The binding targets the Ladybug engine C API in the separate `LadybugDB/ladybug` repo, pinned to one
release: the engine tag is the base of `version.txt` at the repo root (e.g. `0.17.0-alpha.1` -> `v0.17.0`),
overridable per build via `--engine-version` / `ENGINE_VERSION`. The two repos are no longer a single
commit: when moving to a new engine release, re-sync the managed signatures/structs/enums against that
release's `src/include/c_api/lbug.h` and update the ABI tests in the same change.

## Packaging

The binding ships as a **family** of packages, not one fat package: the managed-only `LadybugDB`, one
`LadybugDB.Native.<rid>` per RID (carrying just `runtimes/<rid>/native/*` + an empty `_._` lib marker),
and the `LadybugDB.Native` meta-package that depends on all of them. Consumers reference `LadybugDB`
plus a native package (the meta for all platforms, or a single RID for a slim app). Shipped RIDs:
win-x64, linux-x64, linux-arm64, osx-x64, osx-arm64.

A **Cake Frosting** build project under `cake/` drives everything (don't hand-run `dotnet pack`):

```bash
./build.sh --target Test                           # build + stage host native + run the suite
./build.sh --target Pack --package-version <v>     # full family into ./artifacts, then verify contents
```

- `cake/BuildContext.cs` is the single source of truth for the RID set and the RID->(asset, library)
  mapping; `EnsureNativeStaged` downloads the pinned engine asset (`gh`) and extracts the canonical
  library into `lib/runtimes/<rid>/native/` (skips when one is already staged from a local source build).
- `cake/native/LadybugDB.Native.Runtime.csproj` is one template packed once per RID; the meta-package
  is `cake/native/LadybugDB.Native.nuspec` (version/commit tokens substituted at pack time). `_._` and
  `SuppressDependenciesWhenPacking` keep the native packages free of compile references and framework deps.
- `VerifyPackages` asserts the managed assemblies, every per-RID payload, and the meta's dependency set.

The release pipeline (`.github/workflows/release.yml`, tag `v*`) runs `--target Test` (linux-x64 gate
against the real engine) then `--target Pack`, and publishes all 7 packages via OIDC. The engine release
the natives come from defaults to the version's base (`version.txt`, or the `v*` tag on release). Details
+ one-time nuget.org setup (the trusted publishing policy must now cover every package id):
`.agents/notes/HANDOFF.md`.

## Deeper docs

- `src/include/c_api/lbug.h` (in the parent `LadybugDB/ladybug` repo) — source of truth for every
signature, struct, and enum.
- `.agents/notes/DECISIONS.md` — interop/ABI/repo-split decision log (numbered D-entries with rationale).
- `.agents/notes/HANDOFF.md` — verified native build recipe, gotchas, CI/release, how to cut a release.
- `.agents/notes/ROADMAP.md` — phased status.
- `dotnet-pinvoke` skill (in the monorepo's `.agents/skills/`) — P/Invoke technique. C# style is
enforced by the .NET SDK analyzers (the build is warning-clean); don't restate it.

