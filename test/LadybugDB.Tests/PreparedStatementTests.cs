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
    public void Execute_with_parameter_dictionary_binds_lists_empty_lists_and_nulls()
    {
        Skip.IfNot(TestEnvironment.NativeAvailable, "Native Ladybug library is not available.");

        string dbPath = TestEnvironment.NewTempDbPath();
        try
        {
            using var db = new Database(dbPath);
            using var conn = new Connection(db);

            conn.Query("CREATE NODE TABLE Person(name STRING, PRIMARY KEY(name))").Dispose();
            conn.Query("CREATE (:Person {name: 'Alice'})").Dispose();
            conn.Query("CREATE (:Person {name: 'Bob'})").Dispose();
            conn.Query("CREATE (:Person {name: 'Mallory'})").Dispose();

            var parameters = new Dictionary<string, object?>
            {
                ["names"] = new List<string> { "Alice", "Bob" },
                ["scores"] = new[] { 0.1f, 0.2f },
                ["empty"] = Array.Empty<string>(),
                ["missing"] = null
            };
            using QueryResult result = conn.Execute(
                """
                MATCH (p:Person)
                WHERE p.name IN $names
                RETURN collect(p.name) AS names, $scores AS scores, $empty AS empty, $missing AS missing
                """,
                parameters);

            object?[] row = result.Rows().Single();
            var names = Assert.IsType<object?[]>(row[0]);
            var scores = Assert.IsType<object?[]>(row[1]);
            var empty = Assert.IsType<object?[]>(row[2]);

            Assert.Equal(new object?[] { "Alice", "Bob" }, names.OrderBy(value => value).ToArray());
            Assert.Equal(new object?[] { 0.1f, 0.2f }, scores);
            Assert.Empty(empty);
            Assert.Null(row[3]);
        }
        finally
        {
            TestEnvironment.TryDelete(dbPath);
        }
    }

    [SkippableFact]
    public void Byte_arrays_bind_as_blobs()
    {
        Skip.IfNot(TestEnvironment.NativeAvailable, "Native Ladybug library is not available.");

        string dbPath = TestEnvironment.NewTempDbPath();
        try
        {
            using var db = new Database(dbPath);
            using var conn = new Connection(db);

            conn.Query("CREATE NODE TABLE Doc(id INT64, data BLOB, PRIMARY KEY(id))").Dispose();

            byte[] payload = { 0x00, 0x01, 0x7F, 0x80, 0xFF };
            using (PreparedStatement insert = conn.Prepare("CREATE (:Doc {id: 1, data: $data})"))
            {
                insert.Bind("data", payload).Execute().Dispose();
            }

            var parameters = new Dictionary<string, object?>
            {
                ["empty"] = Array.Empty<byte>(),
                ["blobs"] = new[] { payload },
                ["noBlobs"] = Array.Empty<byte[]>()
            };
            using QueryResult result = conn.Execute(
                "MATCH (d:Doc) RETURN d.data, $empty, $blobs, $noBlobs",
                parameters);

            object?[] row = result.Rows().Single();
            var blobs = Assert.IsType<object?[]>(row[2]);

            Assert.Equal(payload, Assert.IsType<byte[]>(row[0]));
            Assert.Empty(Assert.IsType<byte[]>(row[1]));
            Assert.Equal(payload, Assert.IsType<byte[]>(Assert.Single(blobs)));
            Assert.Empty(Assert.IsType<object?[]>(row[3]));
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
