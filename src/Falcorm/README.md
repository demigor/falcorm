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
- **Dual dialect** — `SqlServer` and `Postgres` with correct `TOP`/`LIMIT`, `OFFSET`/`FETCH`, `string_split`/`unnest`, and insert/update reload of DB generated fields (e.g. `returning`).
- **Fluent API** — `Select`, `Where`, `OrderBy`, `GroupBy`, `Take`, `Skip`, `Union`/`UnionAll`, `Insert`, `Update`, `Delete`, and raw SQL.
- **Async** — All main operations have async variants: `InsertAsync`, `UpdateAsync`, `DeleteAsync`, `FirstOrDefaultAsync`, `FirstAsync`, `SingleOrDefaultAsync`, `SingleAsync`, `ToListAsync`, `RunAsync` (and `RunAsync<T>` for scalar).
- **Safe SQL** — Interpolated string handler turns `$"{value}"` into parameters and supports `Dbo.CN/TN/FN` for quoted identifiers.
- **Batch & transactions** — `BatchInsert`/`BatchUpdate`/`BatchDelete` with `SaveChanges()` and transaction handling.
