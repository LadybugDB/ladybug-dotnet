using LadybugDB;

// Demo graph: a small social network bulk-loaded from CSV with COPY, then traversed.

// The CSV files are vendored under ./data and copied next to the binary at build time.
string dataDirectory = Path.Combine(AppContext.BaseDirectory, "data");

// Use a throwaway on-disk database so COPY behaves like a real load; clean it up at the end.
string databaseDirectory = Path.Combine(Path.GetTempPath(), "ladybug-demo-graph-" + Guid.NewGuid().ToString("N"));

try
{
    using Database database = new(databaseDirectory);
    using Connection connection = new(database);

    // Schema: two node tables and two relationship tables.
    connection.Query("CREATE NODE TABLE User(name STRING, age INT64, PRIMARY KEY(name))").Dispose();
    connection.Query("CREATE NODE TABLE City(name STRING, population INT64, PRIMARY KEY(name))").Dispose();
    connection.Query("CREATE REL TABLE Follows(FROM User TO User, since INT64)").Dispose();
    connection.Query("CREATE REL TABLE LivesIn(FROM User TO City)").Dispose();

    // Bulk load each table from its CSV file.
    Copy(connection, "User", Path.Combine(dataDirectory, "user.csv"));
    Copy(connection, "City", Path.Combine(dataDirectory, "city.csv"));
    Copy(connection, "Follows", Path.Combine(dataDirectory, "follows.csv"));
    Copy(connection, "LivesIn", Path.Combine(dataDirectory, "lives-in.csv"));

    Console.WriteLine("== Who follows whom, and since when ==");
    using (QueryResult follows = connection.Query(
        "MATCH (a:User)-[f:Follows]->(b:User) RETURN a.name, b.name, f.since ORDER BY a.name, b.name"))
    {
        foreach (object?[] row in follows.Rows())
        {
            Console.WriteLine($"  {row[0]} -> {row[1]} (since {row[2]})");
        }
    }

    Console.WriteLine();
    Console.WriteLine("== Where each user lives ==");
    using (QueryResult livesIn = connection.Query(
        "MATCH (u:User)-[:LivesIn]->(c:City) RETURN u.name, c.name, c.population ORDER BY u.name"))
    {
        foreach (object?[] row in livesIn.Rows())
        {
            Console.WriteLine($"  {row[0]} lives in {row[1]} (population {row[2]})");
        }
    }

    Console.WriteLine();
    Console.WriteLine("== Friends-of-friends of Adam ==");
    using (QueryResult friendsOfFriends = connection.Query(
        "MATCH (:User {name: 'Adam'})-[:Follows]->()-[:Follows]->(b:User) " +
        "RETURN DISTINCT b.name ORDER BY b.name"))
    {
        foreach (object?[] row in friendsOfFriends.Rows())
        {
            Console.WriteLine($"  {row[0]}");
        }
    }
}
finally
{
    TryDeleteDirectory(databaseDirectory);
}

// Cypher's COPY needs forward-slash paths on every platform, including Windows.
static void Copy(Connection connection, string table, string csvPath)
{
    string posixPath = csvPath.Replace('\\', '/');
    connection.Query($"COPY {table} FROM '{posixPath}'").Dispose();
}

static void TryDeleteDirectory(string path)
{
    try
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
    catch
    {
        // Best-effort cleanup of the temporary database directory.
    }
}
