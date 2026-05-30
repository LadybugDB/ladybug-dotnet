# Roadmap - Ladybug C# Binding

Live status of the phased plan. Keep statuses current.

Legend: [x] done and verified here. As of 2026-05-29 the native `lbug_shared.dll` is built locally
(MSVC + Ninja, win-x64) and the full suite passes end-to-end (28/28, 0 skipped), so the previous
"[~] awaiting native validation" markers are now resolved for win-x64.

As of 2026-05-30 the binding was split into a standalone repo - now adopted into the LadybugDB org as
`LadybugDB/ladybug-dotnet` - and wired back into the monorepo as the `tools/csharp_api` submodule (local
only). The suite still passes 28/28 from the submodule, building `lbug_shared` against the parent engine
via `../../`.

## Phase 0 - Scaffold  [x]
- [x] `tools/csharp_api/` layout
- [x] `.agents/notes/` living docs (DECISIONS, HANDOFF, ROADMAP)
- [x] `Directory.Build.props`, `nuget/nuget-package.props`
- [x] `LadybugDB.slnx` + projects building (both target frameworks)

## Phase 1 - Core happy path  [x build] [x runtime: win-x64]
- [x] Interop layer (`Native`) + per-TFM marshaling (LibraryImport / DllImport)
- [x] `NativeLibrary` resolver (net7+) mapping `lbug_shared` -> `liblbug.{so,dylib}`
- [x] `Database`, `Connection`, `QueryResult`, `FlatTuple`, `Value` (scalars + string), `LadybugVersion`
- [x] Smoke tests compile; [~] skip until a native lib is present

## Phase 2 - Full value/type model  [x build] [x runtime: win-x64]
- [x] Temporal (date, 4 timestamp precisions + tz, interval), decimal, int128, uuid, blob
- [x] Nested: list/array -> object[], struct/union -> dictionary, map -> dictionary
- [x] Graph: `Node`, `Rel`, `RecursiveRel`
- [x] `_is_owned_by_cpp`-safe disposal; eager row materialization
- [~] Type-mapping tests written; validate Cypher/assertions against a native build

## Phase 3 - Prepared statements + hardening  [x build] [x runtime: win-x64]
- [x] `PreparedStatement` with typed + object `Bind` overloads; `Connection.Prepare/Execute`
- [x] Convenience `Connection.Execute(query, parameters)`
- [x] Two-level error model (throw on failure; `IsSuccess`/`GetErrorMessage` still available)
- [x] Per-connection serialization (lock) + retry-safe disposal (`Interlocked`)
- [~] Prepared-statement tests written; broader tinysnb-dataset suite still TODO (needs native lib)
- [x] P/Invoke review pass vs `dotnet-pinvoke` skill: signatures/strings/ownership/`bool` all clean, 0 SYSLIB warnings
- [x] ABI guard tests (`StructLayoutTests`, 17) assert struct sizes/offsets without needing the native lib

## Phase 4 - Packaging + release  [in progress]
- [x] `dotnet pack` produces a valid package (lib/net10.0 + lib/netstandard2.0 + README + XML docs)
- [x] Local native build recipe + `scripts/build-native-and-test.ps1` (win-x64, MSVC + Ninja)
- [x] win-x64 native lib wired into `lib/runtimes/win-x64/native/` and loaded by the tests
- [x] Package contains the win-x64 native lib at `runtimes/win-x64/native/lbug_shared.dll`;
      consume-test from a local feed runs queries end-to-end (native asset flows to consumer output)
- [x] Repo split: standalone `LadybugDB/ladybug-dotnet` (in the LadybugDB org), wired as the `tools/csharp_api` submodule (local)
- [x] Multi-RID release workflow (`.github/workflows/release.yml`, tag `v*`): downloads prebuilt
      `liblbug-*` for all 5 RIDs (win-x64, linux-x64/arm64, osx-x64/arm64) from `LadybugDB/ladybug`
      releases (pinned `ENGINE_VERSION`), stages -> linux-x64 gate -> packs -> asserts contents -> OIDC publish
- [x] `.github/workflows/ci.yml` on PR/push: managed `build-test` (both TFMs + ABI tests + pack smoke) plus a `native-test` matrix (linux-x64 + win-x64) running the suite against downloaded natives
- [x] Repo adopted into the LadybugDB org as `LadybugDB/ladybug-dotnet`
- [ ] One-time nuget.org trusted-publishing policy + `release` environment / `NUGET_USER` secret (maintainer action)
- [ ] First green release run (`release.yml` needs a `LadybugDB/ladybug` release carrying `liblbug-*`; confirm `ENGINE_VERSION` = `v0.17.0`)
- [ ] `LadybugDB` package-id ownership/reservation on nuget.org before a real publish
- [ ] Optional: win-arm64 / linux-musl RIDs (not produced by the upstream precompiled workflow today)

## Phase 5 (optional) - Extras  [pending]
- Native AOT validation, Arrow C Data interface, observability metrics.
