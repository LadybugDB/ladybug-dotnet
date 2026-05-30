# Decisions Log - Ladybug C# Binding

Append-only record of decisions: the choice, the alternatives, and the rationale.
Keep this updated whenever a decision is made.

## D1 - Package id and namespace: `LadybugDB`
- Alternatives: `Ladybug`, `Kuzu`-style, `Cloud.*`-style.
- Rationale: matches the project brand (`com.ladybugdb` Java group, `@ladybugdb/core` npm). Root namespace and NuGet PackageId both `LadybugDB`.

## D2 - Type naming follows the Java binding
- `DataType` + `DataTypeId` (not `LogicalType`). Mirrors `com.ladybugdb` so cross-language users transfer knowledge. The managed value is exposed as a graph of CLR objects rather than a raw handle.

## D3 - Target frameworks: `net10.0` (primary) + `netstandard2.0` (required reach)
- `LangVersion` = `latest` (C# 14) on all targets. `net10.0` is the modern, AOT-friendly primary; `netstandard2.0` is a hard requirement for broad/.NET Framework reach (per product direction). `net8.0` may be added later if useful.

## D4 - Interop strategy is per-TFM
- `net7.0+`: source-generated `[LibraryImport]`, `nint`-free blittable structs, `StringMarshalling.Utf8`, and a `NativeLibrary.SetDllImportResolver` registered via `[ModuleInitializer]`.
- `netstandard2.0`: classic `[DllImport]` (Cdecl) with manual UTF-8 marshaling (no `LibraryImport`, no `UnmanagedType.LPUTF8Str`, no `NativeLibrary` available). Relies on the OS loader; on Windows the native file is `lbug_shared.dll`, which `DllImport("lbug_shared")` finds directly.
- Single canonical DllImport name `lbug_shared`. On modern runtimes the resolver remaps to `liblbug.so` / `liblbug.dylib` on Linux/macOS.

## D5 - ABI correctness
- C `bool` (`_Bool`, 1 byte) is marshaled as `byte` in structs and `[return: MarshalAs(UnmanagedType.U1)] bool` for returns - never the default 4-byte .NET `bool`.
- `lbug_system_config` includes the macOS-only trailing `thread_qos` (`uint`) field always. This keeps the managed struct 56 bytes on every platform (on non-Apple it occupies what is otherwise tail padding), so the by-value struct ABI matches natively everywhere.
- `lbug_state` and `lbug_data_type_id` are marshaled as `int`-backed enums.

## D6 - Owned C strings
- Functions returning `char*` are declared to return `IntPtr`; we copy to a managed `string` and free via `lbug_destroy_string` (helper `Native.TakeString`). A source-generated custom marshaller is a later refinement (Phase 2/3).

## D7 - Value model
- Public API returns a fully-owned managed value (Rust-style), not live `lbug_value*` handles. Result rows are materialized eagerly to neutralize the documented flat-tuple reuse hazard.

## D8 - Query failure surfaces as an exception (idiomatic .NET)
- `Connection.Query` throws `LadybugQueryException` when the query fails (covers both `lbug_state == Error` and `QueryResult.IsSuccess == false`), which is the common .NET idiom (cf. ADO.NET). `QueryResult.IsSuccess` / `GetErrorMessage()` remain available for callers who prefer to inspect. This is a deliberate, documented deviation from the raw two-level C contract.

## D9 - Calling convention / architectures
- Targeting `x64` and `arm64` RIDs only (no `x86`), so the default platform calling convention is unambiguous; `[DllImport]` uses `CallingConvention.Cdecl` for the `netstandard2.0` path.

## D10 - Disposal and thread-safety (no finalizers / no SafeHandle yet)
- Long-lived disposables (`Database`, `Connection`, `QueryResult`, `PreparedStatement`) use `Interlocked.Exchange` for retry-safe, idempotent `Dispose`; reads use `Volatile.Read`.
- `Connection` serializes `Query`/`Prepare`/`Execute`/`Dispose` with a private lock (cross-TFM `object` lock rather than `System.Threading.Lock`, which is net9+ only) to protect the engine's reused flat-tuple buffer.
- Deliberately NO finalizers and NO `SafeHandle`: native results/values hold engine resources tied to their parent connection/db, so non-deterministic finalization order risks use-after-free. Explicit `using`/`Dispose` is required (documented). A `SafeHandle`-based model remains possible future work but is not worth the ordering hazards now.

## D11 - Test + solution tooling
- Solution uses the new `.slnx` format (default in the .NET 10 SDK).
- xUnit v2 (2.9.x) + `Xunit.SkippableFact` for graceful skipping when the native library is absent (xUnit v2 lacks `Assert.Skip`, which is v3-only). Tests build everywhere and skip rather than fail without native bits.

## D12 - INT128 / DateOnly mapping differ by TFM
- `net7.0+` materializes INT128 as `System.Int128` and DATE as `DateOnly`; `netstandard2.0` uses `System.Numerics.BigInteger` and `DateTime` respectively (those BCL types are unavailable on ns2.0). `record` types are enabled on ns2.0 via an `IsExternalInit` polyfill.

## D13 - ABI guard tests instead of a SafeHandle rewrite (P/Invoke review pass)
- Reviewed the whole interop surface against the `dotnet-pinvoke` skill checklist. Findings: signatures match `lbug.h` exactly (no `size_t`/C `long`/`wchar_t` anywhere, so no `CLong`/`nuint` needed); string encoding is explicit UTF-8 on both paths; memory ownership matches the library's own `lbug_destroy_string`/`lbug_destroy_blob`; no naked `bool` (all `UnmanagedType.U1` / `byte`); both TFMs build with 0 SYSLIB interop warnings.
- Added native-independent struct-layout tests (`StructLayoutTests`) asserting `Marshal.SizeOf`/`Marshal.OffsetOf` for every struct that crosses the boundary (the skill's "verify struct sizes match" step). Required `InternalsVisibleTo("LadybugDB.Tests")`. These 17 tests run (do not skip) even without the native library, locking the ABI down at build time.
- Re-affirmed D10: keep explicit `Dispose` over `SafeHandle`. The skill recommends `SafeHandle`, but its finalizer-driven, non-deterministic release reintroduces the parent/child ordering hazard (value -> tuple -> result -> connection -> database). The skill's own review guidance is "don't rewrite working code"; revisit only if a finalizer safety-net is later deemed worth the ordering risk.

## D14 - Local Windows native build (MSVC + Ninja + pip CMake)
- Compiler: MSVC `14.51` (VS Community 2026) over Ninja. Clang 22 is also installed and supported by the
  CMakeLists' Windows branch, but MSVC is the project's best-tuned Windows path, so it's the default.
- Build tooling: CMake `4.3.2` + Ninja `1.13.0` via `pip install --user`. CMake 4.x rejects the ancient
  `cmake_minimum_required` of several vendored deps, so configure with `-DCMAKE_POLICY_VERSION_MINIMUM=3.5`.
- Build the C-API only: `-DBUILD_SHELL=OFF -DBUILD_SINGLE_FILE_HEADER=OFF -DBUILD_STATIC_LBUG=OFF -DBUILD_TESTS=OFF`,
  target `lbug_shared` (~1046 steps, ~8 min, 18 MB DLL at `build/<config>/src/lbug_shared.dll`).
- Repeatable via `scripts/build-native-and-test.ps1` (imports vcvars64, puts cmake/ninja on PATH,
  configures/builds/stages/tests). Verified end-to-end: 27/27 tests pass on win-x64.

## D15 - CI build + publish (reuse precompiled-bin, multi-RID pack, trusted publishing)
- Cross-platform natives: rather than hand-roll a per-RID native matrix, the C# release workflow
  (`.github/workflows/csharp-release.yml`) REUSES the repo's canonical `precompiled-bin-workflow.yml`
  via `workflow_call`. That workflow already encodes the hard platform knowledge - manylinux_2_28 for
  broad glibc compat, `-static-libstdc++`, macOS deployment target, vcvars on Windows - and already
  emits shared libs for exactly the 5 RIDs we ship: `liblbug-{windows-x86_64, linux-x86_64,
  linux-aarch64, osx-x86_64, osx-arm64}`. Trade-off: it also builds static libs + the CLI we ignore,
  but DRY + known-good natives outweigh the extra build time. A lean shared-only matrix is the
  fallback if decoupling is ever needed.
- RID mapping (artifact -> package path): windows-x86_64->win-x64 (`lbug_shared.dll`),
  linux-x86_64->linux-x64 + linux-aarch64->linux-arm64 (`liblbug.so`), osx-x86_64->osx-x64 +
  osx-arm64->osx-arm64 (`liblbug.dylib`). The `.so`/`.dylib` tarballs ship a symlink chain
  (`liblbug.so` -> `.so.0` -> `.so.0.x.y`); we `cp -L` the real object into a single regular file,
  because the binding loads it by path (the SONAME/install_name is irrelevant for a path load) and
  NuGet does not preserve symlinks. The C API is exported on every platform (`LBUG_C_API` =
  `extern "C" __attribute__((visibility("default")))` / `__declspec(dllexport)`), so P/Invoke resolves.
- Single multi-RID package: one job downloads all native artifacts, stages them into
  `lib/runtimes/<rid>/native/`, and packs once. The existing `nuget-package.props` glob already turns
  that into `runtimes/<rid>/native/*` in the `.nupkg`. The pack job ASSERTS all 5 natives + both
  managed TFMs are present in the `.nupkg` so a silent path/glob regression fails the build instead of
  shipping a managed-only package.
- Publish gate: before packing, the workflow runs the full xUnit suite on the linux-x64 runner against
  the freshly built `liblbug.so` (manylinux build loads fine on newer-glibc ubuntu-latest). Publish
  only happens on a `csharp-v*` tag; manual `workflow_dispatch` builds + packs + uploads the artifact
  but does NOT publish.
- Versioning from the tag: `csharp-v1.2.3` -> `-p:Version=1.2.3`. No file edits; the package version is
  decoupled from the engine's CMake version and from the main repo's GitHub-release pipeline (which
  triggers on `release: created`, not on `csharp-v*` tags, so there is no collision).
- Trusted publishing (OIDC), not API keys: `NuGet/login@v1` + `id-token: write` + `environment: release`,
  pushing with the short-lived token (`--skip-duplicate` keeps re-runs idempotent). One-time nuget.org
  setup is required (policy: owner=LadybugDB, repo=ladybug, workflow=`csharp-release.yml`, env=`release`;
  plus a `release` GitHub environment with secret `NUGET_USER` = nuget.org profile name).
- A separate lightweight `csharp-ci.yml` builds both TFMs + runs the managed/ABI suite (native
  round-trips skip) + a pack smoke check on every PR/push touching the binding - fast feedback kept
  out of the heavy release path.

## D16 - Standalone repo + submodule; natives via upstream release download (supersedes D15's CI shape)
- Repo split: the binding now lives in its own repo, `sergey-v9/ladybug-dotnet` (temporary home; to be
  transferred to the LadybugDB org if the maintainers adopt it). It is developed as a git submodule
  mounted at `tools/csharp_api` in `LadybugDB/ladybug`, exactly mirroring `tools/rust_api` ->
  `ladybug-rust` and `tools/java_api` -> `ladybug-java` (see the monorepo `.gitmodules`). The repo
  root == the old `tools/csharp_api/` contents (so `src/`, `test/`, `.github/`, `.agents/` sit at the
  root). For now the submodule is wired in the monorepo LOCALLY ONLY and not pushed upstream.
- Native source CHANGED from D15: the release workflow no longer reuses the monorepo's
  `precompiled-bin-workflow.yml`. A cross-repo reusable-workflow call does a plain `actions/checkout`,
  which checks out the CALLER (this C# repo, which has no engine source) and would fail to build.
  Instead, `release.yml` downloads the prebuilt `liblbug-*` assets from a `LadybugDB/ladybug` GitHub
  Release (`gh release download`), pinned to `ENGINE_VERSION`. This is decoupled, fast, and version-
  pinned - how a published binding consumes a released C lib in reality. `release-artifacts.yml`
  upstream already attaches those assets to releases.
- Engine version pin: `ENGINE_VERSION` (env in `release.yml`, overridable via the `engine_version`
  dispatch input) is the upstream tag the natives come from. The two repos are no longer one commit;
  bumping it requires re-syncing the managed signatures/structs/enums against that release's
  `lbug.h` and updating the ABI tests in the same change (also captured in `AGENTS.md`).
- Workflows renamed for the dedicated repo: `csharp-ci.yml` -> `.github/workflows/ci.yml` (managed-only
  build/test/pack; path filters now repo-relative) and `csharp-release.yml` ->
  `.github/workflows/release.yml` (download-natives -> stage -> linux-x64 gate -> pack -> assert ->
  OIDC publish). Release tag is now `v*` (not `csharp-v*`); `v1.2.3` -> package version `1.2.3`.
- Trusted publishing now targets this repo: nuget.org policy owner=`sergey-v9`, repo=`ladybug-dotnet`,
  workflow=`release.yml`, env=`release`. Caveat: publishing the official `LadybugDB` package id requires
  id ownership - a personal-repo publish is a dry run until the repo/package move to the org.
- The staging/`cp -L`/RID-mapping/package-content-assertion logic and the `LADYBUG_REQUIRE_NATIVE=1`
  gate from D15 all carry over unchanged; only the source of the native artifacts differs.

## Open (decide later)
- Timestamp representation: `DateTime` (UTC) for non-tz precisions vs `DateTimeOffset` for `TIMESTAMP_TZ` (Phase 2).
- Whether to expose write-side construction of unsigned / `UNION` / `ARRAY` values and full `INTERVAL` (months/days).
- `win-arm64` / `linux-musl` coverage (not produced by the upstream precompiled workflow today).
