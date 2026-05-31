# LadybugDB C# examples

Runnable samples for the published `LadybugDB` NuGet packages. They restore from
[nuget.org](https://www.nuget.org/packages/LadybugDB) and use the version pinned in
[`Directory.Build.props`](Directory.Build.props) (override with `dotnet run -p:LadybugVersion=<version>`).

Every project references two packages:

- `LadybugDB` &mdash; the managed API.
- `LadybugDB.Native` &mdash; the native engine for all supported platforms. For a slim, single-platform
  app, reference one runtime package instead, e.g. `LadybugDB.Native.win-x64` (see the comment in each
  `.csproj`).

## Database usage examples

| Example | What it shows |
| --- | --- |
| [`quickstart`](quickstart/) | Open an in-memory database, create a table, insert rows, and read them back. |
| [`demo-graph`](demo-graph/) | Bulk-load a small social graph from CSV with `COPY`, then traverse relationships. |
| [`prepared-statements`](prepared-statements/) | Reuse a `PreparedStatement` across executions and bind parameters; plus the one-call `Connection.Execute(cypher, parameters)` path. |
| [`result-values`](result-values/) | How `QueryResult.Rows()` materializes scalars, temporal types, lists, structs/maps, nodes, and relationships into CLR objects. |

Run any of them from its own folder:

```bash
cd quickstart
dotnet run
```

To open all four database examples together, use [`Examples.slnx`](Examples.slnx):

```bash
dotnet build Examples.slnx
```

## Package / deployment example

| Example | What it shows |
| --- | --- |
| [`native-loading`](native-loading/) | How the native engine is discovered at runtime &mdash; bundled with the app (native NuGet) vs. installed system-wide (`.deb`). This is a packaging/deployment sample, not a database-usage sample. |
