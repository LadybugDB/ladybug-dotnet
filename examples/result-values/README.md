# result-values

Shows how `QueryResult.Rows()` materializes engine types into CLR objects, printing each value
alongside its runtime type name.

| Engine type | CLR type |
| --- | --- |
| `BOOL` | `bool` |
| `INT32` / `INT64` | `int` / `long` |
| `DOUBLE` | `double` |
| `STRING` | `string` |
| `DATE` | `DateOnly` |
| `TIMESTAMP` | `DateTime` |
| `LIST` | `object?[]` |
| `STRUCT` / `UNION` | `Dictionary<string, object?>` (named fields) |
| `MAP` | `Dictionary<object, object?>` (keys can be any type) |
| node | `Node` |
| relationship | `Rel` |
| variable-length path | `RecursiveRel` |

Note the deliberate split: a `STRUCT` always has string field names, so it materializes to a
string-keyed dictionary, whereas a `MAP`'s keys can be any type, so it materializes to an
`object`-keyed dictionary. The example's formatter handles both via the non-generic `IDictionary`.

The example includes a small recursive formatter so nested values (lists of values, struct fields,
node/relationship properties) print readably.

## Run

```bash
dotnet run
```
