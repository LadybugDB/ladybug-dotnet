# Roadmap - Ladybug C# Binding

Live status of the phased plan. Keep statuses current.

Legend: [x] done and verified here. As of 2026-05-29 the native `lbug_shared.dll` is built locally
(MSVC + Ninja, win-x64) and the full suite passes end-to-end (27/27, 0 skipped), so the previous
"[~] awaiting native validation" markers are now resolved for win-x64.

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
- [x] Multi-RID CI release workflow (`.github/workflows/csharp-release.yml`): reuses
      `precompiled-bin-workflow.yml` for all 5 RIDs (win-x64, linux-x64/arm64, osx-x64/arm64),
      stages -> tests on linux-x64 -> packs -> asserts package contents -> publishes via OIDC
- [x] Lightweight `csharp-ci.yml` (both TFMs + managed/ABI tests + pack smoke check) on PR/push
- [ ] One-time nuget.org trusted-publishing policy + `release` environment / `NUGET_USER` secret (user action)
- [ ] First green CI run (the matrix natives have only been built+loaded locally on win-x64 so far)
- [ ] Optional: win-arm64 / linux-musl RIDs (not produced by the precompiled workflow today)

## Phase 5 (optional) - Extras  [pending]
- Native AOT validation, Arrow C Data interface, observability metrics.
