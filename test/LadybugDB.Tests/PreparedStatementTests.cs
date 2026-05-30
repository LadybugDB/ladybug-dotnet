using System.Collections.Generic;
using System.Linq;
using LadybugDB;
using Xunit;

namespace LadybugDB.Tests;

/// <summary>
/// Phase 3 tests for prepared statements and parameter binding. They skip when the native library
/// is absent.
/// </summary>
public sealed class PreparedStatementTests
{
    [SkippableFact]
    public void Prepared_statement_binds_executes_and_reuses()
    {
        Skip.IfNot(TestEnvironment.NativeAvailable, "Native Ladybug library is not available.");

        string dbPath = TestEnvironment.NewTempDbPath();
        try
        {
            using var db = new Database(dbPath);
            using var conn = new Connection(db);

            conn.Query("CREATE NODE TABLE Person(name STRING, age INT64, PRIMARY KEY(name))").Dispose();

            using (PreparedStatement insert = conn.Prepare("CREATE (:Person {name: $name, age: $age})"))
            {
                insert.Bind("name", "Alice").Bind("age", 30L);
                insert.Execute().Dispose();

                insert.Bind("name", "Bob").Bind("age", 42L);
                insert.Execute().Dispose();
            }

            using PreparedStatement select =
                conn.Prepare("MATCH (p:Person) WHERE p.age >= $minAge RETURN p.name ORDER BY p.name");
            select.Bind("minAge", 35L);

            using QueryResult result = select.Execute();
            List<string?> names = result.Rows().Select(row => (string?)row[0]).ToList();

            Assert.Equal(new[] { "Bob" }, names);
        }
        finally
        {
            TestEnvironment.TryDelete(dbPath);
        }
    }

    [SkippableFact]
    public void Execute_with_parameter_dictionary()
    {
        Skip.IfNot(TestEnvironment.NativeAvailable, "Native Ladybug library is not available.");

        string dbPath = TestEnvironment.NewTempDbPath();
        try
        {
            using var db = new Database(dbPath);
            using var conn = new Connection(db);

            conn.Query("CREATE NODE TABLE Person(name STRING, age INT64, PRIMARY KEY(name))").Dispose();
            conn.Query("CREATE (:Person {name: 'Alice', age: 30})").Dispose();

            var parameters = new Dictionary<string, object?> { ["n"] = "Alice" };
            using QueryResult result = conn.Execute("MATCH (p:Person) WHERE p.name = $n RETURN p.age", parameters);

            Assert.Equal(30L, result.Rows().Single()[0]);
        }
        finally
        {
            TestEnvironment.TryDelete(dbPath);
        }
    }

    [SkippableFact]
    public void Prepare_failure_throws_query_exception()
    {
        Skip.IfNot(TestEnvironment.NativeAvailable, "Native Ladybug library is not available.");

        string dbPath = TestEnvironment.NewTempDbPath();
        try
        {
            using var db = new Database(dbPath);
            using var conn = new Connection(db);

            Assert.Throws<LadybugQueryException>(() => conn.Prepare("THIS IS NOT CYPHER $x"));
        }
        finally
        {
            TestEnvironment.TryDelete(dbPath);
        }
    }
}
