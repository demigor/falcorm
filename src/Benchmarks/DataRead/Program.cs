using System.Data.Common;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using Dapper;
using Entities;
using Istarion.Falcorm;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// Enable Dapper.AOT source generator (required for Native AOT; without this, Dapper uses reflection and fails at runtime).
[module: DapperAot]

// Connection string: set env DataReadBenchmark_ConnectionString or pass as first argument.
// Example: "Server=localhost;Database=Nitro50;UID=nitro;PWD=nitro;TrustServerCertificate=True"
var connectionString = System.Environment.GetEnvironmentVariable("DataReadBenchmark_ConnectionString")
  ?? (args.Length > 0 ? args[0] : "Server=localhost;Database=Nitro50;Integrated Security=true;TrustServerCertificate=True");

var summary = BenchmarkRunner.Run<DataReadBenchmarks>(ManualConfig.Create(DefaultConfig.Instance)
  .AddDiagnoser(MemoryDiagnoser.Default));

return 0;

[MemoryDiagnoser]
public class DataReadBenchmarks
{
  static string _connectionString = null!;
  static DbConnection _connDbMapper = null!;
  static DbConnection _connDapper = null!;
  static DbConnection _connDapperAot = null!;
  static DbSession _session = null!;
  static DataEntities _efContext = null!;
  static IServiceProvider _sp = null!;

  // No SqlMapper.SetTypeMap / custom type mappers: they use reflection (GetProperties, GetCustomAttributes)
  // and fail under Native AOT. Dapper.AOT generates mapping at compile time when [module: DapperAot] is set.
  // For [Column("X")] mapping, entities would need [UseColumnAttribute] (see DAP043); without it, mapping is by property name.

  [GlobalSetup]
  public void GlobalSetup()
  {
    _connectionString = System.Environment.GetEnvironmentVariable("DataReadBenchmark_ConnectionString")
      ?? "Server=localhost;Database=Nitro50;Integrated Security=true;TrustServerCertificate=True";

    var services = new ServiceCollection();
    _sp = services.BuildServiceProvider();

    _connDbMapper = new SqlConnection(_connectionString);
    _connDbMapper.Open();
    _session = new DbSession(_sp, _connDbMapper);

    _connDapper = new SqlConnection(_connectionString);
    _connDapper.Open();

    _connDapperAot = new SqlConnection(_connectionString);
    _connDapperAot.Open();

    var optionsBuilder = new DbContextOptionsBuilder<DataEntities>().UseSqlServer(_connectionString);
    var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<DataEntities>.Instance;
    _efContext = new DataEntities(optionsBuilder.Options, logger);
  }

  [GlobalCleanup]
  public void GlobalCleanup()
  {
    _connDbMapper?.Dispose();
    _connDapper?.Dispose();
    _connDapperAot?.Dispose();
    _efContext?.Dispose();
  }

  static string Table(string name) => $"dbo.[{name}]";
// --- Companies ---
  [Benchmark(Baseline = true)]
  public List<Company> Companies_Falcorm() => _session.Companies.ToList();

  [Benchmark]
  public List<Company> Companies_EF() => [.. _efContext.Companies.AsNoTracking()];

  [Benchmark]
  public List<Company> Companies_Dapper() => [.. _connDapper.Query<Company>($"select * from {Table("TCompanies")}")];

  // --- Currencies ---
  [Benchmark]
  public List<Currency> Currencies_Falcorm() => _session.Currencies.ToList();

  [Benchmark]
  public List<Currency> Currencies_EF() => [.. _efContext.Currencies.AsNoTracking()];

  [Benchmark]
  public List<Currency> Currencies_Dapper() => [.. _connDapper.Query<Currency>($"select * from {Table("TCurrencies")}")];

  // --- AssetClasses ---
  [Benchmark]
  public List<AssetClass> AssetClasses_Falcorm() => _session.AssetClasses.ToList();

  [Benchmark]
  public List<AssetClass> AssetClasses_EF() => [.. _efContext.AssetClasses.AsNoTracking()];

  [Benchmark]
  public List<AssetClass> AssetClasses_Dapper() => [.. _connDapper.Query<AssetClass>($"select * from {Table("TAssetClasses")}")];

  // --- Datasheets ---
  [Benchmark]
  public List<Datasheet> Datasheets_Falcorm() => _session.Datasheets.ToList();

  [Benchmark]
  public List<Datasheet> Datasheets_EF() => [.. _efContext.Datasheets.AsNoTracking()];

  [Benchmark]
  public List<Datasheet> Datasheets_Dapper() => [.. _connDapper.Query<Datasheet>($"select * from {Table("TDatasheets")}")];

}

