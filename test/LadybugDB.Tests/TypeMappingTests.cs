using System;
using System.Collections.Generic;
using System.Linq;
using LadybugDB;
using Xunit;

namespace LadybugDB.Tests;

/// <summary>
/// Phase 2 type-mapping tests. These exercise the full value materializer (integer family,
/// temporal, int128, nested collections, and graph types). They skip when the native library is
/// absent; the Cypher and expected CLR mappings are validated once a native build is available.
/// </summary>
public sealed class TypeMappingTests
{
    [SkippableFact]
    public void Scalar_and_temporal_columns_map_to_clr_types()
    {
        Skip.IfNot(TestEnvironment.NativeAvailable, "Native Ladybug library is not available.");

        string dbPath = TestEnvironment.NewTempDbPath();
        try
        {
            using var db = new Database(dbPath);
            using var conn = new Connection(db);

            conn.Query(
                "CREATE NODE TABLE T(" +
                "id INT64, i8 INT8, i16 INT16, i32 INT32, u32 UINT32, " +
                "dbl DOUBLE, fl FLOAT, flag BOOL, name STRING, " +
                "d DATE, ts TIMESTAMP, big INT128, " +
                "PRIMARY KEY(id))").Dispose();

            conn.Query(
                "CREATE (:T {id: 1, i8: 1, i16: 2, i32: 3, u32: 4, " +
                "dbl: 1.5, fl: 2.5, flag: true, name: 'hi', " +
                "d: date('2020-01-15'), ts: timestamp('2020-01-15 10:30:00'), " +
                "big: 123456789012345678901234567890})").Dispose();

            using QueryResult result = conn.Query(
                "MATCH (t:T) RETURN t.i8, t.i16, t.i32, t.u32, t.dbl, t.fl, t.flag, t.name, t.d, t.ts, t.big");

            object?[] row = result.Rows().Single();

            Assert.Equal((sbyte)1, row[0]);
            Assert.Equal((short)2, row[1]);
            Assert.Equal(3, row[2]);
            Assert.Equal((uint)4, row[3]);
            Assert.Equal(1.5d, row[4]);
            Assert.Equal(2.5f, row[5]);
            Assert.Equal(true, row[6]);
            Assert.Equal("hi", row[7]);
            Assert.Equal(new DateOnly(2020, 1, 15), row[8]);
            Assert.Equal(new DateTime(2020, 1, 15, 10, 30, 0, DateTimeKind.Utc), row[9]);
            Assert.Equal(Int128.Parse("123456789012345678901234567890"), row[10]);
        }
        finally
        {
            TestEnvironment.TryDelete(dbPath);
        }
    }

    [SkippableFact]
    public void List_literal_maps_to_object_array()
    {
        Skip.IfNot(TestEnvironment.NativeAvailable, "Native Ladybug library is not available.");

        string dbPath = TestEnvironment.NewTempDbPath();
        try
        {
            using var db = new Database(dbPath);
            using var conn = new Connection(db);

            using QueryResult result = conn.Query("RETURN [1, 2, 3] AS list");
            object?[] row = result.Rows().Single();

            var list = Assert.IsType<object?[]>(row[0]);
            Assert.Equal(new object?[] { 1L, 2L, 3L }, list);
        }
        finally
        {
            TestEnvironment.TryDelete(dbPath);
        }
    }

    [SkippableFact]
    public void Struct_literal_maps_to_dictionary()
    {
        Skip.IfNot(TestEnvironment.NativeAvailable, "Native Ladybug library is not available.");

        string dbPath = TestEnvironment.NewTempDbPath();
        try
        {
            using var db = new Database(dbPath);
            using var conn = new Connection(db);

            using QueryResult result = conn.Query("RETURN {x: 1, y: 'a'} AS s");
            object?[] row = result.Rows().Single();

            var dict = Assert.IsType<Dictionary<string, object?>>(row[0]);
            Assert.Equal(1L, dict["x"]);
            Assert.Equal("a", dict["y"]);
        }
        finally
        {
            TestEnvironment.TryDelete(dbPath);
        }
    }

    [SkippableFact]
    public void Node_value_materializes_id_label_and_properties()
    {
        Skip.IfNot(TestEnvironment.NativeAvailable, "Native Ladybug library is not available.");

        string dbPath = TestEnvironment.NewTempDbPath();
        try
        {
            using var db = new Database(dbPath);
            using var conn = new Connection(db);

            conn.Query("CREATE NODE TABLE Person(name STRING, age INT64, PRIMARY KEY(name))").Dispose();
            conn.Query("CREATE (:Person {name: 'Alice', age: 30})").Dispose();

            using QueryResult result = conn.Query("MATCH (p:Person) RETURN p");
            object?[] row = result.Rows().Single();

            var node = Assert.IsType<Node>(row[0]);
            Assert.Equal("Person", node.Label);
            Assert.Equal("Alice", node.Properties["name"]);
            Assert.Equal(30L, node.Properties["age"]);
        }
        finally
        {
            TestEnvironment.TryDelete(dbPath);
        }
    }
}
