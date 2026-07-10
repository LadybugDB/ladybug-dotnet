# LadybugDB for .NET

Official C# binding for the [Ladybug](https://github.com/ladybugdb/ladybug) embedded graph database.
It wraps the native Ladybug C API via P/Invoke and ships prebuilt native libraries for supported
platforms, so you can run Cypher queries against an embedded graph database directly from .NET.

> Current package family: `0.18.1`, built against the Ladybug `v0.18.1` engine.

## Target frameworks

- `net10.0` (primary, AOT/trim friendly, source-generated `LibraryImport`)
- `netstandard2.0` (broad reach, including .NET Framework)

## Installation

The binding is split into a managed package and separate native packages, so an app only carries the
platform binaries it needs. Reference the managed package **plus** a native package.

For an app that should run on any supported platform, add the native meta-package (it pulls in every
per-platform native package):

```bash
dotnet add package LadybugDB
dotnet add package LadybugDB.Native
```

For a slim, single-platform app, reference just the native package for that platform instead of the
meta-package:

```bash
dotnet add package LadybugDB
dotnet add package LadybugDB.Native.win-x64
```

Available native packages: `LadybugDB.Native.win-x64`, `LadybugDB.Native.linux-x64`,
`LadybugDB.Native.linux-arm64`, `LadybugDB.Native.osx-x64`, `LadybugDB.Native.osx-arm64`. The
`LadybugDB.Native` meta-package depends on all of them.

## Quick start

```csharp
using LadybugDB;

using var db = new Database("./demo.db");
using var conn = new Connection(db);

conn.Query("CREATE NODE TABLE Person(name STRING, age INT64, PRIMARY KEY(name))").Dispose();
conn.Query("CREATE (:Person {name: 'Alice', age: 30})").Dispose();

using var result = conn.Query("MATCH (p:Person) RETURN p.name, p.age");
foreach (var row in result.Rows())
{
    Console.WriteLine($"{row[0]} is {row[1]} years old");
}
```

## Building / testing

```powershell
dotnet build LadybugDB.slnx -c Release
dotnet test  LadybugDB.slnx -c Release
```

The binding needs the native Ladybug shared library at runtime (`lbug_shared.dll` /
`liblbug.so` / `liblbug.dylib`). When the native library is not available, the native round-trip
tests skip (the ABI/struct-layout guards still run).

## How the packages are built

This repo does not contain the engine source; the native libraries come from the upstream
[Ladybug](https://github.com/ladybugdb/ladybug) engine. Packaging is driven by a Cake Frosting build
project under [`cake/`](cake) (run it with the `build.ps1` / `build.sh` bootstrap):

```bash
./build.sh --target Test     # build + stage the host native + run the suite
./build.sh --target Pack     # build the full package family into ./artifacts
```

All packages in the family share one version. The first three numeric segments track the upstream engine
release, and the optional fourth segment is the .NET package revision for binding-only releases. For
example, package `0.18.1` wraps the Ladybug `v0.18.1` engine; a future binding-only fix over the same
engine would be `0.18.1.1`. Prerelease suffixes are reserved for preview builds.

The package version is defined once in `version.txt` at the repo root. Override it with
`--package-version <v>` (the release workflow uses the git tag). The engine release defaults to the first
three numeric package-version segments; override with `--engine-version` when needed.

- **`Pack`** stages the prebuilt `liblbug-*` assets for every shipped RID (downloaded from an upstream
  `LadybugDB/ladybug` GitHub Release, pinned to the engine version derived from `version.txt`; override
  with `--engine-version`),
  packs the managed `LadybugDB` package, one `LadybugDB.Native.<rid>` package per RID, and the
  `LadybugDB.Native` meta-package, then verifies every package's contents.
- **CI / release** (`.github/workflows/`) invoke the same pipeline; the release workflow gates on the
  linux-x64 suite against the real engine and publishes all packages to nuget.org via OIDC.
- **Local development** is easiest when this repo is checked out as the `tools/csharp_api` submodule
  inside the Ladybug monorepo: `scripts/build-native-and-test.ps1` builds `lbug_shared` from the parent
  engine tree, stages it into `lib/runtimes/win-x64/native/`, and runs the suite. With a native already
  staged there, `--target Test` reuses it instead of downloading.
