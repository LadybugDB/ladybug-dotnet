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
version (`ENGINE_VERSION`); adding/dropping a RID or changing `runtimes/{rid}/native/` packaging.
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
release: `ENGINE_VERSION` in `.github/workflows/release.yml` is the upstream tag the
prebuilt natives are pulled from. The two repos are no longer a single commit: when bumping
`ENGINE_VERSION`, re-sync the managed signatures/structs/enums against that release's
`src/include/c_api/lbug.h` and update the ABI tests in the same change.

## Packaging

`dotnet pack src/LadybugDB/LadybugDB.csproj -c Release -p:Version=<v>` bundles whatever sits under
`lib/runtimes/<rid>/native/` into `runtimes/<rid>/native/` in the `.nupkg`. Shipped RIDs: win-x64,
linux-x64, linux-arm64, osx-x64, osx-arm64. The release pipeline (`.github/workflows/release.yml`,
tag `v`*) downloads the prebuilt `liblbug-`* assets for the pinned `ENGINE_VERSION` from
`LadybugDB/ladybug` releases, stages them, gates on the linux-x64 suite, asserts package contents,
then publishes via OIDC. Details + one-time nuget.org setup: `.agents/notes/HANDOFF.md`.

## Deeper docs

- `src/include/c_api/lbug.h` (in the parent `LadybugDB/ladybug` repo) — source of truth for every
signature, struct, and enum.
- `.agents/notes/DECISIONS.md` — interop/ABI/repo-split decision log (numbered D-entries with rationale).
- `.agents/notes/HANDOFF.md` — verified native build recipe, gotchas, CI/release, how to cut a release.
- `.agents/notes/ROADMAP.md` — phased status.
- `dotnet-pinvoke` skill (in the monorepo's `.agents/skills/`) — P/Invoke technique. C# style is
enforced by the .NET SDK analyzers (the build is warning-clean); don't restate it.

