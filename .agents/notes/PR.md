# Split native packaging into a package family + Cake Frosting build

> Living PR description — keep this current as the branch evolves. The checklist at the bottom is the
> source of truth for what's left before merge / first publish.

## Summary

Restructures the LadybugDB .NET binding's packaging so the native engine no longer ships inside the
managed package. Instead it's a **package family**:

- `LadybugDB` — managed-only (no native payload).
- `LadybugDB.Native.<rid>` — one package per platform, carrying just that RID's native library.
- `LadybugDB.Native` — meta-package that depends on all per-RID packages.

Consumers reference `LadybugDB` **plus** a native package: the meta-package for "runs anywhere", or a
single `LadybugDB.Native.<rid>` for a slim, single-platform app. All packaging is driven by a new
**Cake Frosting** build project under `cake/`.

## Motivation

The old single package bundled every platform's native library into one `.nupkg`, so every consumer
carried all of them regardless of target. Splitting the natives into per-RID packages (with a meta
"all platforms" package) lets an app pull only what it needs, while keeping a one-line install for the
common case.

## What changed

### Package family

- Managed `LadybugDB` is now managed-only: removed the `runtimes/`** native glob from
`nuget/nuget-package.props` (it now ships `lib/net10.0` + `lib/netstandard2.0` + README + symbols only).
- Each `LadybugDB.Native.<rid>` carries `runtimes/<rid>/native/`* plus an empty `lib/netstandard2.0/_._`
marker (so NuGet adds no compile reference) and declares no dependencies.
- `LadybugDB.Native` meta-package depends on all five per-RID packages.
- Shipped RIDs: `win-x64`, `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`.
- The managed package intentionally has **no** dependency on any native package — that decoupling is
what makes the slim, single-RID install possible.

### Build pipeline (`cake/`)

- A Cake Frosting console app: `LadybugDB.Build.csproj` + `BuildContext.cs` (which also hosts the `Main`
entry point), and the pipeline tasks under `Tasks/` (`Pipeline.cs` for the small tasks +
`VerifyPackagesTask.cs`). Bootstrap with `build.ps1` / `build.sh`.
- Tasks: `Clean`, `Restore`, `BuildManaged`, `Test`, `FetchNatives`, `PackManaged`, `PackRuntimes`,
`PackNativeMeta`, `VerifyPackages`, `Pack`, `Default`.
- `BuildContext` is the single source of truth for the RID set and the RID -> (release asset, library)
mapping. `EnsureNativeStaged` reuses a locally-built native if present, otherwise downloads the pinned
engine release asset (via `gh`) and extracts the canonical library (pulling the real shared object out
of the `.so`/`.dylib` symlink chain, since NuGet doesn't preserve symlinks).
- Per-RID packages come from one parameterized template (`cake/native/LadybugDB.Native.Runtime.csproj`,
packed once per RID). The meta-package is a nuspec template (`cake/native/LadybugDB.Native.nuspec`)
with version/commit substituted at pack time. All packing is `dotnet`-only (no nuget.exe / mono).
- `VerifyPackages` asserts the managed assemblies, every per-RID payload, and the meta's dependency set,
so a silent packaging regression fails the build instead of shipping a broken package.

### Versioning

- The package version tracks the upstream engine version, with an `-alpha.N` prerelease suffix while in
development. Current default: **`0.17.0-alpha.1`**.
- It lives in ONE place - `version.txt` at the binding root - read by `BuildContext`, the managed
`nuget-package.props`, and both CI workflows. Bump the alpha (or the engine) there with no code change.
`--prerelease ""` cuts a stable build equal to the engine version; `--package-version <v>` overrides
exactly (the release workflow passes the `v*` git tag).

### CI / release

- `ci.yml` and `release.yml` now invoke the pipeline (`dotnet run --project cake/... -- --target ...`)
instead of inline shell. `ci.yml` runs a `test` matrix (linux-x64 + win-x64) and a `pack` smoke;
`release.yml` runs the linux-x64 test gate, packs + verifies, then publishes all packages via OIDC.

### Docs

- README (install + build sections), `AGENTS.md` (Packaging).

## Consumption

```bash
# runs on any supported platform
dotnet add package LadybugDB
dotnet add package LadybugDB.Native

# slim, single-platform
dotnet add package LadybugDB
dotnet add package LadybugDB.Native.win-x64
```

## Validation

- Local end-to-end (win-x64 host, engine `v0.17.0`): `--target Pack` produced and verified all 7
packages as `0.17.0-alpha.1` (correct layouts; meta pins all five per-RID deps; no stray references);
`--target Test` passed 28/28 with the native loaded (no skips).
- Build is warning- and lint-clean.

## Next steps / TODO

Maintainer actions before first publish:

- **Owner — still TODO (one-time):** create the `release` GitHub environment on
`LadybugDB/ladybug-dotnet` and add secret `NUGET_USER` = the nuget.org **profile name** (NOT email).
The `publish` job declares `environment: release` and `NuGet/login@v1` reads `secrets.NUGET_USER`, so
without these the publish step fails with a 403/auth error.
- First green CI run: trigger `release.yml` via `workflow_dispatch` to confirm the linux-x64 gate +
all-RID download/pack on CI (proven locally on win-x64 so far).

Follow-ups (can land later):

- Bump the `-alpha.N` suffix as the binding stabilizes; drop it for the first stable `v0.17.0`.
- Phase 3 (remaining): expand the suite to mirror the Java + C API tests over `dataset/tinysnb`.
- Phase 5 (optional): Native AOT validation, Arrow C Data interface, observability.

