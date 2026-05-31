# Example: loading the native engine two ways (bundled vs system)

This example shows that the **same** managed `LadybugDB` assembly can run against the native engine in
two different deployment models, chosen only by what is present at runtime:

1. **Bundled** - the app references the managed package **and** a native package, so `liblbug.so` ships
   inside the app and is loaded from the app's own directory.
2. **System** - the app references **only** the managed package, and the native engine is provided by the
   operating system (here: installed from a Debian package into `/usr/lib`).

Both are exercised in Docker (`linux-x64`) by the same tiny consumer app, which prints the absolute path
the OS actually mapped for `liblbug` (read from `/proc/self/maps`) and then runs a real Cypher round-trip.

## Why both work

The binding is split into independent packages:

- **`LadybugDB`** - managed-only NuGet package (the P/Invoke wrapper). It declares **no dependency** on
  any native package, so it can be installed on its own.
- **`LadybugDB.Native.<rid>`** - per-platform native packages, each carrying
  `runtimes/<rid>/native/liblbug.so` (plus the `LadybugDB.Native` meta-package that depends on all RIDs).

Native loading goes through a custom resolver in the managed assembly
(`src/LadybugDB/Interop/Native.cs`):

```
NativeLibrary.SetDllImportResolver(assembly, Resolve);
// Resolve(): NativeLibrary.TryLoad("liblbug.so" | "liblbug" | "lbug_shared", assembly, searchPath)
```

`NativeLibrary.TryLoad(name, assembly, ...)` first searches next to the assembly (the **bundled** case)
and, failing that, hands the bare soname to the OS dynamic linker (`dlopen`), which consults the
`ldconfig` cache and the standard system library directories (the **system** case). So the same managed
code transparently supports both models.

## Running it

Requires Docker with **Linux** containers and the package family built into `../../artifacts`
(`LadybugDB.<v>.nupkg` and `LadybugDB.Native.linux-x64.<v>.nupkg`; produce them with the binding's
`Pack` target if missing).

```powershell
# from this folder (tools/csharp_api/examples/native-loading)
./run.ps1            # both scenarios
./run.ps1 bundled    # only the bundled scenario
./run.ps1 system     # only the system scenario
```

`run.ps1` stages a local NuGet feed from `../../artifacts` into `./feed`, builds each image, runs it, and
tees transcripts into `./logs`.

## Layout

```
native-loading/
  README.md               # this file
  nuget.config            # restore from ./feed (local), nuget.org fallback
  run.ps1                 # stage feed + build/run both scenarios
  app/
    ConsumerApp.csproj    # references LadybugDB (+ native when IncludeNative=true)
    Program.cs            # the provenance + round-trip probe
  scripts/
    build-deb.sh          # build the liblbug .deb from the native nupkg
    run-bundled.sh        # bundled-scenario entrypoint
    run-system.sh         # system-scenario entrypoint (incl. negative control)
  Dockerfile.bundled      # bundled-scenario image
  Dockerfile.system       # system-scenario image
  feed/   (gitignored)    # staged .nupkg files
  logs/   (gitignored)    # build + run transcripts
```

## Scenario 1 - bundled (`Dockerfile.bundled`)

`dotnet publish` with `-p:IncludeNative=true` references `LadybugDB` + `LadybugDB.Native.linux-x64`, so
`liblbug.so` lands next to `ConsumerApp.dll`. No system engine is installed.

Expected: the app loads the engine from its own directory and the Cypher round-trip succeeds -

```
AppDirNative: /app/publish/liblbug.so
LoadedFrom : /app/publish/liblbug.so   [BUNDLED/app]
RESULT: SUCCESS
```

## Scenario 2 - system (`Dockerfile.system`)

`dotnet publish` **managed-only** (references `LadybugDB` only), so the app ships no `liblbug.so`. The
engine is instead installed system-wide by a minimal Debian package (`scripts/build-deb.sh`) that drops
the *same* `liblbug.so` into `/usr/lib/x86_64-linux-gnu/` and runs `ldconfig` - exactly what a
distro-provided native package does. (The `dotnet/sdk:10.0` image is Ubuntu-based, so `dpkg`/`apt` apply.)

Expected: a **negative control** runs the app *before* installing the `.deb` and it fails with
`DllNotFoundException` - proving nothing is bundled. After `dpkg -i`, the same app loads the engine from
the system path and the round-trip succeeds -

```
AppDirNative: <none>
LoadedFrom : /usr/lib/x86_64-linux-gnu/liblbug.so   [SYSTEM]
RESULT: SUCCESS
```

## Note on real distro packaging

The system scenario works because the resolver `dlopen`s the **bare** name `liblbug.so` and the `.deb`
installs an **unversioned** `liblbug.so` on the linker's default search path. But the engine's real SONAME
is `liblbug.so.0`, and a conventional distro package ships the **versioned** `liblbug.so.0` (the
unversioned symlink lives only in a `-dev` package). In that layout the `ldconfig` cache key is
`liblbug.so.0`, which the current resolver never asks for - so a managed-only app could fail to find a
correctly-packaged system engine. To be robust against standard packaging, either add `liblbug.so.0` to
the resolver's candidate list, or have the runtime package expose an unversioned `liblbug.so` on a
default-searched path (as this example's `.deb` does).
