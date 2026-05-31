# prepared-statements

Shows parameter binding and statement reuse.

It demonstrates:

- `Connection.Prepare(...)` to compile a query once.
- Fluent `PreparedStatement.Bind(name, value)` with typed overloads, then `Execute()`.
- Reusing one prepared statement for many inserts and for repeated reads with different parameters.
- The one-call convenience path `Connection.Execute(cypher, IReadOnlyDictionary<string, object?>)`.

Using parameters (instead of string-concatenated Cypher) is also the safe way to pass user-supplied
values into a query.

## Run

```bash
dotnet run
```

Expected output:

```
Inserted 4 people.

== People at least N years old (one prepared SELECT, reused) ==
-- minAge = 30
   Alice (30)
   Bob (42)
   Dan (51)
-- minAge = 50
   Dan (51)

== People in a given city (Connection.Execute with a parameter dictionary) ==
   Alice lives in Waterloo
   Carol lives in Waterloo
```
