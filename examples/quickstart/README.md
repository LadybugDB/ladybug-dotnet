# quickstart

The smallest useful LadybugDB program: open an in-memory database, create a `Person` node table,
insert two rows, and read them back ordered by name.

It demonstrates:

- `new Database()` with an empty path for an in-memory database.
- `Connection.Query(...)` for DDL, inserts, and reads.
- `QueryResult.ColumnNames` and `QueryResult.Rows()` to materialize rows into `object?[]`.

## Run

```bash
dotnet run
```

Expected output:

```
LadybugDB 0.18.2 (storage v42)

name | age
Alice is 25 years old
Bob is 30 years old
```

(`LadybugVersion.Version` reports the native engine version, which can differ from the NuGet package
version.)
