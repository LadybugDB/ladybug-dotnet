# LadybugDB for .NET

Official C# binding for the [Ladybug](https://github.com/ladybugdb/ladybug) embedded graph database.
It wraps the native Ladybug C API via P/Invoke and ships prebuilt native libraries for supported
platforms, so you can run Cypher queries against an embedded graph database directly from .NET.

> Status: under construction. See `.agents/notes/ROADMAP.md` for progress.

## Target frameworks

- `net10.0` (primary, AOT/trim friendly, source-generated `LibraryImport`)
- `netstandard2.0` (broad reach, including .NET Framework)

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

## How the native library is obtained

This repo does not contain the engine source; the native library comes from the upstream
[Ladybug](https://github.com/ladybugdb/ladybug) engine:

- **CI / release** (`.github/workflows/release.yml`) downloads the prebuilt `liblbug-*` assets from
  an upstream `LadybugDB/ladybug` GitHub Release (pinned via `ENGINE_VERSION`) and stages them into
  `lib/runtimes/<rid>/native/` before packing the multi-RID NuGet package.
- **Local development** is easiest when this repo is checked out as the `tools/csharp_api` submodule
  inside the Ladybug monorepo: `scripts/build-native-and-test.ps1` then builds `lbug_shared` from the
  parent engine tree, stages it into `lib/runtimes/win-x64/native/`, and runs the suite.

See `.agents/notes/HANDOFF.md` for details.
