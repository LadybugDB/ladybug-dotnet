using LadybugDB;

// Prepared statements: compile a parameterized query once, then bind and run it many times.
// Reusing a prepared statement avoids re-planning the query on each execution.

using Database database = new();
using Connection connection = new(database);

connection.Query("CREATE NODE TABLE Person(name STRING, age INT64, city STRING, PRIMARY KEY(name))").Dispose();

// 1) Reuse one prepared INSERT for several rows.
(string Name, long Age, string City)[] people =
[
    ("Alice", 30, "Waterloo"),
    ("Bob", 42, "Kitchener"),
    ("Carol", 25, "Waterloo"),
    ("Dan", 51, "Guelph"),
];

using (PreparedStatement insert = connection.Prepare(
    "CREATE (:Person {name: $name, age: $age, city: $city})"))
{
    foreach ((string name, long age, string city) in people)
    {
        // Bind is fluent and Execute runs on the owning connection.
        insert.Bind("name", name).Bind("age", age).Bind("city", city);
        insert.Execute().Dispose();
    }
}

Console.WriteLine($"Inserted {people.Length} people.");
Console.WriteLine();

// 2) Reuse one prepared SELECT with different parameter values.
Console.WriteLine("== People at least N years old (one prepared SELECT, reused) ==");
using (PreparedStatement select = connection.Prepare(
    "MATCH (p:Person) WHERE p.age >= $minAge RETURN p.name, p.age ORDER BY p.age"))
{
    foreach (long minimumAge in new long[] { 30, 50 })
    {
        select.Bind("minAge", minimumAge);
        using QueryResult result = select.Execute();
        Console.WriteLine($"-- minAge = {minimumAge}");
        foreach (object?[] row in result.Rows())
        {
            Console.WriteLine($"   {row[0]} ({row[1]})");
        }
    }
}

Console.WriteLine();

// 3) The one-call convenience path: prepare, bind a parameter dictionary, and execute.
Console.WriteLine("== People in a given city (Connection.Execute with a parameter dictionary) ==");
Dictionary<string, object?> parameters = new() { ["city"] = "Waterloo" };
using (QueryResult result = connection.Execute(
    "MATCH (p:Person) WHERE p.city = $city RETURN p.name ORDER BY p.name", parameters))
{
    foreach (object?[] row in result.Rows())
    {
        Console.WriteLine($"   {row[0]} lives in {parameters["city"]}");
    }
}
