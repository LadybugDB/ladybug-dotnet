using System.Collections.Generic;
using System.Linq;
using LadybugDB;
using Xunit;

namespace LadybugDB.Tests;

/// <summary>
/// Phase 1 smoke tests: the end-to-end happy path (open, query, read scalars and strings). When the
/// native Ladybug library is not present, the tests skip rather than fail.
/// </summary>
public sealed class SmokeTests
{
    [SkippableFact]
    public void Version_is_reported()
    {
        Skip.IfNot(TestEnvironment.NativeAvailable, "Native Ladybug library is not available.");

        Assert.False(string.IsNullOrWhiteSpace(LadybugVersion.Version));
        Assert.True(LadybugVersion.StorageVersion > 0);
    }

    [SkippableFact]
    public void Create_insert_and_query_roundtrip()
    {
        Skip.IfNot(TestEnvironment.NativeAvailable, "Native Ladybug library is not available.");

        string dbPath = TestEnvironment.NewTempDbPath();

        try
        {
            using var db = new Database(dbPath);
            using var conn = new Connection(db);

            conn.Query("CREATE NODE TABLE Person(name STRING, age INT64, PRIMARY KEY(name))").Dispose();
            conn.Query("CREATE (:Person {name: 'Alice', age: 30})").Dispose();
            conn.Query("CREATE (:Person {name: 'Bob', age: 42})").Dispose();

            using QueryResult result = conn.Query("MATCH (p:Person) RETURN p.name, p.age ORDER BY p.age");

            Assert.True(result.IsSuccess);
            Assert.Equal(2UL, result.ColumnCount);
            Assert.Equal(new[] { "p.name", "p.age" }, result.ColumnNames.ToArray());

            List<object?[]> rows = result.Rows().ToList();

            Assert.Equal(2, rows.Count);
            Assert.Equal("Alice", rows[0][0]);
            Assert.Equal(30L, rows[0][1]);
            Assert.Equal("Bob", rows[1][0]);
            Assert.Equal(42L, rows[1][1]);
        }
        finally
        {
            TestEnvironment.TryDelete(dbPath);
        }
    }

    [SkippableFact]
    public void Failed_query_throws_with_message()
    {
        Skip.IfNot(TestEnvironment.NativeAvailable, "Native Ladybug library is not available.");

        string dbPath = TestEnvironment.NewTempDbPath();

        try
        {
            using var db = new Database(dbPath);
            using var conn = new Connection(db);

            LadybugQueryException ex = Assert.Throws<LadybugQueryException>(
                () => conn.Query("MATCH (n:DoesNotExist) RETURN n"));

            Assert.False(string.IsNullOrWhiteSpace(ex.Message));
        }
        finally
        {
            TestEnvironment.TryDelete(dbPath);
        }
    }
}
