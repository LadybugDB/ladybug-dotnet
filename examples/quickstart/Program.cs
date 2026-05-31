using LadybugDB;

// Quickstart: create a tiny table in an in-memory database, insert rows, and read them back.
// Mirrors the engine's C/C++ quickstart (examples/c/main.c, examples/cpp/main.cpp).

Console.WriteLine($"LadybugDB {LadybugVersion.Version} (storage v{LadybugVersion.StorageVersion})");
Console.WriteLine();

// An empty path opens an in-memory database; nothing is written to disk.
using Database database = new();
using Connection connection = new(database);

// Define a node table and insert a couple of rows.
connection.Query("CREATE NODE TABLE Person(name STRING, age INT64, PRIMARY KEY(name))").Dispose();
connection.Query("CREATE (:Person {name: 'Alice', age: 25})").Dispose();
connection.Query("CREATE (:Person {name: 'Bob', age: 30})").Dispose();

// Query and print the rows. QueryResult.Rows() materializes each row into a CLR object?[].
using QueryResult result = connection.Query("MATCH (p:Person) RETURN p.name AS name, p.age AS age ORDER BY p.name");

Console.WriteLine(string.Join(" | ", result.ColumnNames));
foreach (object?[] row in result.Rows())
{
    Console.WriteLine($"{row[0]} is {row[1]} years old");
}
