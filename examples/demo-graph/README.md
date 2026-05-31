# demo-graph

Loads a small social graph from CSV and traverses it. The sample uses the familiar `demo-db`
shape: users, cities, follows relationships, and lives-in relationships.

Schema:

- `User(name, age)`
- `City(name, population)`
- `Follows(FROM User TO User, since)`
- `LivesIn(FROM User TO City)`

It demonstrates:

- Bulk loading with `COPY <Table> FROM '<path>'` (note: Cypher wants forward-slash paths on Windows too).
- Relationship traversal, two-hop patterns (`friends-of-friends`), `DISTINCT`, and `ORDER BY`.
- Using a temporary on-disk database directory and cleaning it up afterwards.

The four CSV files live under [`data/`](data/) and are copied next to the binary at build time, so this
sample does not depend on the monorepo `dataset/` checkout.

## Run

```bash
dotnet run
```

Expected output:

```
== Who follows whom, and since when ==
  Adam -> Karissa (since 2020)
  Adam -> Zhang (since 2020)
  Karissa -> Zhang (since 2021)
  Zhang -> Noura (since 2022)

== Where each user lives ==
  Adam lives in Waterloo (population 150000)
  Karissa lives in Waterloo (population 150000)
  Noura lives in Guelph (population 75000)
  Zhang lives in Kitchener (population 200000)

== Friends-of-friends of Adam ==
  Noura
  Zhang
```
