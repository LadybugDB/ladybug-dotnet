using System.Globalization;
using LadybugDB;

// Result values: what QueryResult.Rows() materializes engine types into, as CLR objects.

using Database database = new();
using Connection connection = new(database);

// 1) Scalars and temporal types, one column at a time.
connection.Query(
    "CREATE NODE TABLE T(" +
    "id INT64, flag BOOL, count INT32, amount DOUBLE, name STRING, " +
    "day DATE, moment TIMESTAMP, PRIMARY KEY(id))").Dispose();
connection.Query(
    "CREATE (:T {id: 1, flag: true, count: 42, amount: 3.5, name: 'hello', " +
    "day: date('2020-01-15'), moment: timestamp('2020-01-15 10:30:00')})").Dispose();

Console.WriteLine("== Scalars and temporal types ==");
using (QueryResult result = connection.Query(
    "MATCH (t:T) RETURN t.flag, t.count, t.amount, t.name, t.day, t.moment"))
{
    string[] columns = result.ColumnNames.ToArray();
    foreach (object?[] row in result.Rows())
    {
        for (int i = 0; i < row.Length; i++)
        {
            Console.WriteLine($"  {columns[i],-9} = {Format(row[i])}  [{TypeName(row[i])}]");
        }
    }
}

Console.WriteLine();

// 2) Nested values: lists, structs, and maps.
Print(connection, "List literal", "RETURN [1, 2, 3] AS list");
Print(connection, "Struct literal", "RETURN {x: 1, y: 'a'} AS s");
Print(connection, "Map literal", "RETURN map([1, 2], ['a', 'b']) AS m");

// 3) Graph values: node, relationship, and a variable-length path.
connection.Query("CREATE NODE TABLE Person(name STRING, age INT64, PRIMARY KEY(name))").Dispose();
connection.Query("CREATE REL TABLE Knows(FROM Person TO Person, since INT64)").Dispose();
connection.Query("CREATE (:Person {name: 'Alice', age: 30})").Dispose();
connection.Query("CREATE (:Person {name: 'Bob', age: 42})").Dispose();
connection.Query(
    "MATCH (a:Person {name: 'Alice'}), (b:Person {name: 'Bob'}) " +
    "CREATE (a)-[:Knows {since: 2020}]->(b)").Dispose();

Print(connection, "Node", "MATCH (p:Person {name: 'Alice'}) RETURN p");
Print(connection, "Relationship", "MATCH (:Person)-[r:Knows]->(:Person) RETURN r");
Print(connection, "Variable-length path", "MATCH p = (:Person {name: 'Alice'})-[:Knows*1..2]->(:Person) RETURN p");

static void Print(Connection connection, string label, string cypher)
{
    Console.WriteLine($"== {label} ==");
    using QueryResult result = connection.Query(cypher);
    foreach (object?[] row in result.Rows())
    {
        foreach (object? value in row)
        {
            Console.WriteLine($"  {Format(value)}  [{TypeName(value)}]");
        }
    }

    Console.WriteLine();
}

static string TypeName(object? value) => value?.GetType().Name ?? "null";

static string Format(object? value)
{
    switch (value)
    {
        case null:
            return "null";
        case string text:
            return $"\"{text}\"";
        case byte[] bytes:
            return "0x" + Convert.ToHexString(bytes);
        case Node node:
            return $"({node.Label} {FormatDictionary(node.Properties)})";
        case Rel rel:
            return $"-[{rel.Label} {FormatDictionary(rel.Properties)}]->";
        case RecursiveRel path:
            return $"path of {path.Nodes.Count} node(s) and {path.Rels.Count} rel(s)";
        // STRUCT/node/rel properties materialize to Dictionary<string, object?>, while a MAP
        // materializes to Dictionary<object, object?> (its keys can be any type). The non-generic
        // IDictionary handles both.
        case System.Collections.IDictionary dictionary:
            return FormatMap(dictionary);
        case object?[] list:
            return "[" + string.Join(", ", list.Select(Format)) + "]";
        case IFormattable formattable:
            return formattable.ToString(null, CultureInfo.InvariantCulture);
        default:
            return value.ToString() ?? string.Empty;
    }
}

static string FormatDictionary(IReadOnlyDictionary<string, object?> map)
    => "{" + string.Join(", ", map.Select(entry => $"{entry.Key}: {Format(entry.Value)}")) + "}";

static string FormatMap(System.Collections.IDictionary map)
{
    var entries = new List<string>(map.Count);
    foreach (System.Collections.DictionaryEntry entry in map)
    {
        entries.Add($"{Format(entry.Key)}: {Format(entry.Value)}");
    }

    return "{" + string.Join(", ", entries) + "}";
}
