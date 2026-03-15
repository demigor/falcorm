# Falcorm

![AOT-compatible](https://img.shields.io/badge/AOT-ready-green)
![.NET 10.0](https://img.shields.io/badge/.NET-10.0-blue)
![MSSQL](https://img.shields.io/badge/MSSQL-supported-blue)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-supported-blue)


**Falco / ORM.** AOT-first, lightweight, superfast, micro-ORM with powerful features.

Falcorm is a minimal data-access layer for .NET: fluent SQL builder, zero-runtime-reflection mapping, and first-class support for **MSSQL** and **PostgreSQL**. All mapping and table access are generated at compile time via source generators, making it ideal for **Native AOT** and high-throughput scenarios. Plays nicely with ASP.NET Core and **Kestrel** — same Falco family.

---

## Features

- **AOT-first** — Generated mappers and session APIs; no reflection at runtime.
- **Lightweight** — Small surface area: `DbSession`, `SqlBuilder<T>`, `DbTable<T>`, and generated extensions.
- **Superfast** — Span-based column mapping, parameterized queries via interpolated strings, optional batching.
- **Dual dialect** — `MssqlBuilder` and `NpgsqlBuilder` with correct `TOP`/`LIMIT`, `OFFSET`/`FETCH`, `string_split`/`unnest`, and insert/update reload of DB generated fields (e.g. `returning`).
- **Fluent API** — `Select`, `Where`, `OrderBy`, `GroupBy`, `Take`, `Skip`, `Union`/`UnionAll`, `Insert`, `Update`, `Delete`, and raw SQL.
- **Async** — All main operations have async variants: `InsertAsync`, `UpdateAsync`, `DeleteAsync`, `FirstOrDefaultAsync`, `FirstAsync`, `SingleOrDefaultAsync`, `SingleAsync`, `ToListAsync`, `RunAsync` (and `RunAsync<T>` for scalar).
- **Safe SQL** — Interpolated string handler turns `$"{value}"` into parameters and supports `Dbo.CN/TN/FN` for quoted identifiers.
- **Batch & transactions** — `BatchInsert`/`BatchUpdate`/`BatchDelete` with `SaveChanges()` and transaction handling.

---

## Why Falcorm

In serious applications, the “bonuses” of a full ORM often don’t pay off. **Change tracking** tends to get in the way: DbContext cache semantics are weak, so you end up maintaining your own dictionaries and using `AsNoTracking()` anyway. **Migrations** are rarely run automatically; schema is usually managed with separate scripts or tooling. **Complex entity graphs** and implicit loading (Include, lazy load) quickly become a performance and predictability problem, so you end up controlling what loads and when by hand — and the need for that ORM layer shrinks.

Falcorm skips tracking, migrations, and magic loading. You get explicit queries, predictable SQL, and full control. If you **do** need change tracking: use **`ICloneable<T>`**, take a snapshot of the entity right after load, then call **`Update(entity, snapshot)`** — only changed columns are written. That’s your own, explicit change tracking without a context cache.

---

## Requirements

- .NET 10.0 (Generators use C# 14 Extensions Properties. C# 14 is available in .NET 10+)

---

## Installation

1. Reference the nuget package and both ORM and the source generators will be activated:

```
dotnet add package Istarion.Falcorm

```

2. Define entities with `[Table("...")]` (and optionally `[Key]`, `[Column("...")]`, etc.). The generator emits `IDbMapper<T>` implementations and a static partial class that extends `DbSession` with typed table access (e.g. `session.Users`, `session.Groups`).

3. Use your existing `DbConnection` (e.g. `SqlConnection`, `NpgsqlConnection`). Falcorm has **no dependency** on SqlClient or Npgsql — it works at the **ADO.NET** level (`DbConnection`, `DbCommand`, `DbDataReader`). Dialect is inferred from the connection type.

### Schema attributes (generator)

Falcorm uses the **standard .NET attributes** from `System.ComponentModel.DataAnnotations` and `System.ComponentModel.DataAnnotations.Schema` — no custom attribute types are required. The source generator respects these on your entity types:

| Attribute | Purpose |
|-----------|--------|
| **`[Table("Name")]`** | Table name; required for generator to pick up the type. |
| **`[Column("Name")]`** | Column name when it differs from the property name. |
| **`[Key]`** | Marks the primary key; used for Find/Get and for Insert/Update/Delete. Composite keys: put `[Key]` on each key member. |
| **`[ConcurrencyCheck]`** | Enables **optimistic concurrency**: token is included in UPDATE WHERE and in reload after update; conflict throws. |
| **`[DatabaseGenerated]`** | `DatabaseGeneratedOption.Identity` / `Computed` — column is omitted from INSERT/UPDATE and reloaded after insert/update when needed. |
| **`[ReadOnly]`** | On the **entity class**: read-only entity; no Insert/Update/Delete mapping is generated (query-only). |

The materializer creates entities via a **parameterless constructor** and then sets properties/fields. Therefore entity types must have a parameterless constructor; types that rely on **`init`** accessors, **`required`** members, or only parameterized constructors are **not yet supported**. The materializer supports both **properties** and **fields** (public, instance, non-static). It maps from **database column names** (including those specified in `[Column]`). A **partial result set** (fewer columns than the entity has) is supported with no performance penalty — mapping is by column name, so missing columns are simply left at default; reading fewer columns can be faster. The **SQL Builder** does not remap property names to column names: in your SQL (Select, Where, OrderBy, Raw, etc.) you use the **actual SQL column names**, not the entity property names.

---

## Quick start

```csharp
using Istarion.Falcorm;
using Microsoft.Data.SqlClient;

var conn = new SqlConnection("Server=...;Database=MyDb;...");
var session = new DbSession(serviceProvider, conn);

// Generated table access (after adding Istarion.Generators)
var user = session.Users.Find(id);           // by primary key, nullable
var user2 = session.Users.Get(id);          // by primary key, throws if not found
var list = session.Users.Where($"{Dbo.CN("Name")} like {pattern}").OrderBy("Name").Take(10).ToList();

// Raw SQL with parameters
var count = session.Run<int>($"select count(*) from dbo.[Users] where Status = {status}");
```

---

## Usage

### Queries

**SqlBuilder** is a single-use object per query; it is not intended to be reused after execution (e.g. after `Run()`, `ToList()`, or `BuildCommand()`). Create a new builder for each request.

- **Select** — `session.Users.Select("Col1").Select("Col2")`, or `session.Users.Select($"Col, {expr} as Alias")`.
- **Table(tableName)** — Use an alternative source for the query: `.Table("OtherTable")` keeps the same entity/mapper but runs the query against the given table (e.g. view, synonym, same schema elsewhere).
- **Where / OrderBy / GroupBy / Raw** — Use interpolated strings; values become parameters, identifiers can be wrapped with `Dbo.CN("Name")`, `Dbo.TN("TableName")`, `Dbo.FN("FunctionName")` for proper quoting. The **`IN`** operator is supported with a collection of strings directly: pass `IEnumerable<string>` in the interpolated string and it is expanded as a single parameter (MSSQL: `string_split`, PostgreSQL: `unnest`).

**Dialect portability:** Falcorm supports two dialects (MSSQL and PostgreSQL). If you need the **same code to run against both**, use **`Dbo.CN` / `TN` / `FN`** in your SQL so that names are expanded in the correct form for the current dialect (e.g. `[Name]` vs `"Name"`); then you can switch the DB driver and the code needs no changes. If you **only ever target one database**, you can write dialect-specific SQL directly (e.g. `dbo.[Table]`, `top(1)`, or PostgreSQL syntax) and skip `Dbo` where it doesn’t matter.

Chaining **`Select(...).Select(...)`**, **`Where(...).Where(...)`**, **`OrderBy(...).OrderBy(...)`**, **`GroupBy(...).GroupBy(...)`**, or **`Raw(...).Raw(...)`** concatenates clauses into the **same** statement (e.g. more columns in SELECT, extra AND in WHERE, comma-separated ORDER BY / GROUP BY, or multiple raw statements separated by `;`). Use **`Then()`** for separate statements in one roundtrip.

```csharp
session.Users.Where($"{Dbo.CN("Name")} like {"%test%"}").ToList();
session.Users.OrderBy($"{Dbo.CN("Name")}, {Dbo.CN("Id")} desc").Take(20).ToList();

// IN with collection of strings (one parameter, dialect-specific expansion)
IEnumerable<string> ids = new[] { "a", "b", "c" };
session.Users.Where($"{Dbo.CN("Id")} in ({ids})").ToList();
```

- **Paging** — `.Skip(n).Take(m)` (generates `OFFSET`/`FETCH` for MSSQL, `LIMIT`/`OFFSET` for PostgreSQL).
- **Union** — `.Union(queries, unionAll: true)`.
- **Streaming read** — **`Enum()`** returns an `IEnumerable<T>` that reads **directly from the data reader** without materializing the full result set; no list allocation, so it is suitable for large result sets.

### Find / Get by key

For entities with `[Key]`, the generator adds typed **extension methods** for lookup: both on the **table** (`IDbTable<T>`) and on the **builder** (`SqlBuilder<T>`).

- **`Find(key1, key2, ...)`** — returns `T?`; `null` if not found. Single or composite key arguments in property order.
- **`Get(key1, key2, ...)`** — same arguments, returns `T`; throws `ArgumentException` if not found.

```csharp
// On table
var user = session.Users.Find(userId);                    // single key
var perm = session.Permissions.Find(roleId, resourceId);   // composite key
var user = session.Users.Get(userId);                      // throws if missing

// On builder (e.g. after Table("OtherView"))
session.Users.Table("UsersArchive").Find(userId);
```

### CRUD (with generated mappers)

- **Insert** — `session.Users.Insert(entity, reload: true)` (reload fills DB-generated columns if any).
- **Update** — `session.Users.Update(entity, snapshot?)` (optional snapshot for partial update). **Optimistic concurrency** is always applied when the entity has a property marked `[ConcurrencyCheck]`.
- **Delete** — `session.Users.Delete(entity)`.

Async variants: **`InsertAsync`**, **`UpdateAsync`**, **`DeleteAsync`**, **`FirstOrDefaultAsync`**, **`FirstAsync`**, **`SingleOrDefaultAsync`**, **`SingleAsync`**, **`ToListAsync`**, **`RunAsync`** / **`RunAsync<T>`**. Use the same fluent chain (e.g. `Where(...).Take(10).ToListAsync(ct)`).

Reload of database-generated/identity columns is done in the **most reliable way**: without using `OUTPUT` (MSSQL), via a separate `SELECT` after the insert/update. So it works on **any table** and with **any triggers**.

Chaining **`Insert(...).Insert(...)`** or **`Update(...).Update(...)`** on the same builder concatenates columns and values into a **single** INSERT or UPDATE statement (one row, more columns), rather than multiple statements. Use **`Then()`** when you want multiple separate statements in one roundtrip.

### Unit of Work

Enqueue multiple changes and commit them in a **single transaction** with `SaveChanges()`:

- **BatchInsert** — schedule insert (with optional reload of DB-generated columns).
- **BatchUpdate** — schedule update (optional snapshot for partial update). If the entity implements **`ICloneable<T>`**, you can use the overload that takes an update action: the snapshot is taken automatically (clone before, then apply the action), so only changed fields are written. Handy for explicit partial updates.
- **BatchDelete** — schedule delete.

```csharp
session.Users.BatchInsert(user);
session.Users.BatchUpdate(user, snapshot);
session.Users.BatchDelete(user);
session.SaveChanges();  // one transaction

// Partial update via action (entity must implement ICloneable<T>)
session.Users.BatchUpdate(user, e => { e.Name = "Lex"; e.LastName = "Falco"; });
session.SaveChanges();
```

The simplest **`ICloneable<T>`** implementation:

```csharp
public class MyEntity : ICloneable<MyEntity>
{
  public MyEntity Clone() => (MyEntity)MemberwiseClone();
}
```

### Then() — chain commands, one roundtrip

Use **`Then()`** to append another statement to the same command. The builder becomes a chain of statements (`stmt1; stmt2; stmt3; ...`) sent in **one roundtrip** when you call `Run()` or `RunAsync()`.

You can mix Inserts, Updates, Deletes, or raw statements. Useful for bulk inserts or multi-step scripts without extra network calls.

```csharp
// Two inserts in one roundtrip
session.Users.Insert("Id, Name", $"{id1},{name1}").Then().Insert("Id, Name", $"{id2},{name2}").Run();

// Insert then delete in one roundtrip
session.Users.Insert("Id, Name", $"{id},{name}").Then().Delete($"Id = {id}").Run();
```

### Raw SQL

- **Run (NonQuery)** — `session.Run($"delete from dbo.[Log] where Id = {id}")`.
- **Run&lt;T&gt; (Scalar)** — `session.Run<int>($"select {Dbo.FN("GetCount")}()")`.

### Ad-hoc commands (no entity type)

For raw or ad-hoc queries without a mapped entity, use `session.Builder<T>()` with a custom materializer or run commands via `session.Run` / `session.Run<T>` as needed.

---

## Benchmarks

Read-performance benchmarks (Companies, Currencies, AssetClasses, Datasheets) compare Falcorm with EF Core and Dapper under **JIT** and **Native AOT**. Results and analysis (including the ADO.NET ceiling, allocation comparison, and AOT notes) are in **[Benchmarks/DataRead/README.md](src/Benchmarks/DataRead/README.md)**.

---

## Solving Partial Updates: PatchDTO and Falcorm

The **PatchDTO** source generator (same **Istarion.Generators** project) generates **partial-update DTOs** from a projection method: you define which fields are read-only, which are writable, and the generator emits a class whose setters apply directly to the entity. That fits Falcorm’s partial-update model: load with Falcorm, apply a patch (e.g. from JSON), then persist with `Update(entity, snapshot)` or `BatchUpdate`.

**Typical flow:**

1. **Load** the entity with Falcorm: `var entity = session.Resources.Find(id);` (or `Get`, or a query).
2. **Take a snapshot** (e.g. `var snapshot = entity.Clone();`) so Falcorm can do a partial update — only changed columns are written.
3. **Receive and apply** the patch: PatchDTO generates a `Load` method that deserializes the payload (e.g. JSON) and applies it to the **target entity** you pass in — e.g. `MyMappers.ProjectionDto.Load(entity, json, jsonStr => JsonSerializer.Deserialize<MyMappers.ProjectionDto>(jsonStr));` — so the incoming patch is bound to that entity in one step.
4. **Persist** with Falcorm: `session.Resources.Update(entity, snapshot)` or `session.Resources.BatchUpdate(entity, snapshot)` and then `SaveChanges()`.

So: **Falcorm** handles load/save and SQL; **PatchDTO** defines the patch shape and applies it to the entity with zero reflection. Use some form of calculation (e.g. `ReadOnly.Value(...)`) for fields that must not be patched (e.g. Id, Ts), plain property access for normal patchable fields, and `Writable(expression)` for computed values that the client can send. 

Full syntax, async variant, and options are in **[Generators/README.md](src/Generators/README.md)**.

---

## Related

- **[Generators/README.md](src/Generators/README.md)** — **PatchDTO** source generator: full docs (projection syntax, `ReadOnly`/`Writable`, `Load`/`LoadAsync`, `GenerateJsonContext`).
- **[Benchmarks/DataRead/README.md](src/Benchmarks/DataRead/README.md)** — Benchmark suite comparing Falcorm (DbSession + generated mappers) with EF Core, Dapper, and Dapper.AOT.

---

## License

MIT, Copyright © 2026 Istarion Software Pty Ltd.
