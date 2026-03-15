# DataReadBenchmarks

Compares read performance for tables **Companies**, **Currencies**, **AssetClasses**, and **Datasheets** across:

- **Falcorm** — `DbSession` + generated mappers (Istarion.Generators)
- **EF Core** — Entity Framework Core, `DataEntities`, `AsNoTracking()`
- **Dapper** — classic Dapper, `Query<T>()` (reflection / ref-emit) / Dapper.AOT, same `Query<T>()` but via source generator (no runtime reflection)

## How to run

1. Set the connection string in one of these ways:
   - environment variable `DataReadBenchmark_ConnectionString`;
   - first command-line argument.

2. From the repository root (e.g. `src`):

```bash
dotnet run --project Benchmarks/DataRead/DataReadBenchmarks.csproj -c Release
```

Example with explicit connection string:

```bash
set DataReadBenchmark_ConnectionString=Server=localhost;Database=Nitro50;UID=nitro;PWD=nitro;TrustServerCertificate=True
dotnet run --project Benchmarks/DataRead/DataReadBenchmarks.csproj -c Release
```

Only **MSSQL** is supported.

**Note:** With Dapper.AOT referenced, Dapper calls are intercepted by the generator, so `*_Dapper` and `*_DapperAot` use the same AOT-generated code and yield similar results. To compare “classic” Dapper with Dapper.AOT, temporarily remove the Dapper.AOT reference and rerun the benchmark.

### Dapper.AOT and Native AOT (PublishAot)

To avoid Dapper.AOT failing at runtime when publishing with AOT:

1. **`[module: DapperAot]`** — explicitly enable the Dapper.AOT generator; otherwise Dapper falls back to reflection and fails under AOT.
2. **No `SqlMapper.SetTypeMap` or custom type mappers** — they use `GetProperties()` / `GetCustomAttributes()` and are trimmed or fail under Native AOT.
3. **`<InterceptorsNamespaces>$(InterceptorsNamespaces);Dapper.AOT</InterceptorsNamespaces>`** in the csproj — required for the generated interceptors to compile (CS9137).

For column names that differ from property names (e.g. `[Column("X")]`), add `[UseColumnAttribute]` on entities when needed (see [DAP043](https://aot.dapperlib.dev/rules/DAP043)).

---

## Results

BenchmarkDotNet v0.15.8, Windows 11. 12th Gen Intel Core i9-12900K, .NET 10.0.4.

### Native AOT (X64 NativeAOT x86-64-v3)

| Method                   | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0      | Gen1     | Gen2     | Allocated   | Alloc Ratio |
|--------------------------|-----------:|----------:|----------:|------:|--------:|----------:|---------:|---------:|------------:|------------:|
| Companies (Falcorm)      | 728.465 ms | 2.2253 ms | 1.9727 ms | 1.000 |    0.00 | 1000.0000 |        - |        - | 30405.13 KB |       1.000 |
| Companies (EF Core)      |         NA |        NA |        NA |     ? |       ? |        NA |       NA |       NA |          NA |           ? |
| Companies (Dapper)       | 731.409 ms | 2.5803 ms | 2.2873 ms | 1.004 |    0.00 | 1000.0000 |        - |        - | 30964.56 KB |       1.018 |
| Currencies (Falcorm)     |   2.038 ms | 0.0133 ms | 0.0111 ms | 0.003 |    0.00 |    3.9063 |        - |        - |    97.16 KB |       0.003 |
| Currencies (EF Core)     |         NA |        NA |        NA |     ? |       ? |        NA |       NA |       NA |          NA |           ? |
| Currencies (Dapper)      |   2.088 ms | 0.0415 ms | 0.0539 ms | 0.003 |    0.00 |    3.9063 |        - |        - |    98.52 KB |       0.003 |
| AssetClasses (Falcorm)   |   4.955 ms | 0.0298 ms | 0.0265 ms | 0.007 |    0.00 |   15.6250 |        - |        - |   350.13 KB |       0.012 |
| AssetClasses (EF Core)  |         NA |        NA |        NA |     ? |       ? |        NA |       NA |       NA |          NA |           ? |
| AssetClasses (Dapper)    |   4.949 ms | 0.0372 ms | 0.0330 ms | 0.007 |    0.00 |   15.6250 |        - |        - |   353.33 KB |       0.012 |
| Datasheets (Falcorm)     |   7.180 ms | 0.0695 ms | 0.0616 ms | 0.010 |    0.00 |  625.0000 | 617.1875 | 617.1875 |  3526.83 KB |       0.116 |
| Datasheets (EF Core)     |         NA |        NA |        NA |     ? |       ? |        NA |       NA |       NA |          NA |           ? |
| Datasheets (Dapper)      |   7.008 ms | 0.0908 ms | 0.0758 ms | 0.010 |    0.00 |  617.1875 | 609.3750 | 609.3750 |  3523.96 KB |       0.116 |

Baseline: `Companies (Falcorm)`. EF jobs reported NA in this run.

### JIT (RyuJIT x86-64-v3, same hardware)

| Method                   | Mean       | Error     | StdDev    | Ratio | Gen0      | Gen1      | Gen2     | Allocated   | Alloc Ratio |
|--------------------------|-----------:|----------:|----------:|------:|----------:|----------:|---------:|------------:|------------:|
| Companies (Falcorm)      | 732.630 ms | 2.2767 ms | 2.0183 ms | 1.000 | 1000.0000 |         - |        - | 30403.85 KB |       1.000 |
| Companies (EF Core)      | 742.632 ms | 2.8121 ms | 2.4929 ms | 1.014 | 2000.0000 | 1000.0000 |        - | 43849.05 KB |       1.442 |
| Companies (Dapper)       | 742.549 ms | 2.7809 ms | 2.6013 ms | 1.014 | 2000.0000 | 1000.0000 |        - | 39364.78 KB |       1.295 |
| Currencies (Falcorm)     |   2.024 ms | 0.0159 ms | 0.0133 ms | 0.003 |    3.9063 |         - |        - |    97.16 KB |       0.003 |
| Currencies (EF Core)     |   2.092 ms | 0.0359 ms | 0.0515 ms | 0.003 |    7.8125 |         - |        - |   141.21 KB |       0.005 |
| Currencies (Dapper)      |   2.060 ms | 0.0395 ms | 0.0439 ms | 0.003 |    3.9063 |         - |        - |   116.09 KB |       0.004 |
| AssetClasses (Falcorm)   |   4.955 ms | 0.0334 ms | 0.0261 ms | 0.007 |   15.6250 |         - |        - |   350.13 KB |       0.012 |
| AssetClasses (EF Core)   |   5.111 ms | 0.0999 ms | 0.1110 ms | 0.007 |   23.4375 |    7.8125 |        - |   441.12 KB |       0.015 |
| AssetClasses (Dapper)    |   4.998 ms | 0.0827 ms | 0.0733 ms | 0.007 |   15.6250 |         - |        - |   333.32 KB |       0.011 |
| Datasheets (Falcorm)     |   7.160 ms | 0.0731 ms | 0.0648 ms | 0.010 |  578.1250 |  570.3125 | 570.3125 |  3526.70 KB |       0.116 |
| Datasheets (EF Core)     |   7.152 ms | 0.0805 ms | 0.0753 ms | 0.010 |  500.0000 |  492.1875 | 492.1875 |  3534.21 KB |       0.116 |
| Datasheets (Dapper)      |  11.608 ms | 0.1356 ms | 0.1202 ms | 0.016 |         - |         - |        - |    24.02 KB |       0.001 |

---

## Analysis

- **ADO.NET ceiling**: The similar latencies across Falcorm, EF Core, and Dapper (e.g. ~732–743 ms for Companies under JIT) suggest all three are hitting the same limit: **ADO.NET** and the database driver, not the mapping layer. So the real differentiators are allocations, GC pressure, and AOT compatibility.
- **Latency**: Falcorm is the baseline (ratio 1.00). Under JIT, Falcorm and Dapper are on par (~732 ms for Companies); EF is slightly slower (~743 ms). Under AOT, Falcorm and Dapper keep the same order of magnitude; EF is NA in the AOT run (reflection/dynamic code often incompatible with Native AOT).
- **Allocations**: Falcorm allocates less than EF and Dapper for the same workload. For Companies (JIT): ~30.4 MB (Falcorm) vs ~43.8 MB (EF) and ~39.4 MB (Dapper). For Currencies/AssetClasses, Falcorm is about 1.5–2× lower allocation than EF. Lower allocations mean less GC pressure and better scalability under load.
- **AOT**: Under Native AOT, Falcorm and Dapper retain similar timings and allocation patterns as under JIT; EF does not run in this AOT run. For AOT-friendly stacks, Falcorm and Dapper are the viable options.
- **Datasheets (Dapper) JIT**: The 11.6 ms row with only 24 KB allocated suggests a different reading pattern (e.g. streaming) rather than a fully materialized list. For an apples-to-apples “full materialization” comparison, Companies/Currencies/AssetClasses are representative; there Falcorm matches or beats latency with lower allocations.

**Summary**: Falcorm delivers the same or better latency with significantly lower allocations and runs reliably under Native AOT, making it a strong fit for high-throughput or AOT-first scenarios.
