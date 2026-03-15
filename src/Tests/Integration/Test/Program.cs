using System.Data.Common;
using Falcorm.IntegrationTests.Entities;
using Microsoft.Data.SqlClient;
using Npgsql;

// Connection strings: from Aspire (ConnectionStrings__*, *_URI), env (Docker Compose), or defaults
var mssqlCs = Environment.GetEnvironmentVariable("ConnectionStrings__falcormMssql")
    ?? "Server=localhost;Database=FalcormMssql;User Id=sa;Password=ChangeIt!;TrustServerCertificate=True;Encrypt=False;Connect Timeout=30";
var pgCs = Environment.GetEnvironmentVariable("ConnectionStrings__falcormPostgres")
    ?? "Host=localhost;Database=FalcormPostgres;Username=falcorm;Password=ChangeIt!;Timeout=30";

Console.WriteLine("Falcorm Integration Tests — ensure DBs and seed data.");
Console.WriteLine("MSSQL: " + (mssqlCs.Contains("localhost") ? "localhost" : "Aspire/docker"));
Console.WriteLine("Postgres: " + (pgCs.Contains("localhost") ? "localhost" : "Aspire/docker"));

var totalSw = System.Diagnostics.Stopwatch.StartNew();

await TimedAsync("MSSQL Ensure DB", () => EnsureMssqlDatabaseAsync(mssqlCs)).ConfigureAwait(false);
await TimedAsync("MSSQL Create + Seed", () => RunMssqlAsync(mssqlCs)).ConfigureAwait(false);
await TimedAsync("Postgres Create + Seed", () => RunPostgresAsync(pgCs)).ConfigureAwait(false);

Console.WriteLine("--- Read tests ---");
await TimedAsync("MSSQL Read tests", async () =>
{
  await using var conn = new SqlConnection(mssqlCs);
  await conn.OpenAsync().ConfigureAwait(false);
  await RunReadTestsAsync(conn).ConfigureAwait(false);
}).ConfigureAwait(false);
await TimedAsync("Postgres Read tests", async () =>
{
  await using var conn = new NpgsqlConnection(pgCs);
  await conn.OpenAsync().ConfigureAwait(false);
  await RunReadTestsAsync(conn).ConfigureAwait(false);
}).ConfigureAwait(false);

totalSw.Stop();
Console.WriteLine($"Total: {totalSw.ElapsedMilliseconds} ms");
Console.WriteLine("Done.");

static void Timed(string name, Action action)
{
  var sw = System.Diagnostics.Stopwatch.StartNew();
  action();
  sw.Stop();
  Console.WriteLine($"[{sw.ElapsedMilliseconds,6} ms] {name}");
}

static async Task TimedAsync(string name, Func<Task> action)
{
  var sw = System.Diagnostics.Stopwatch.StartNew();
  await action().ConfigureAwait(false);
  sw.Stop();
  Console.WriteLine($"[{sw.ElapsedMilliseconds,6} ms] {name}");
}

static async Task TimedValueTaskAsync(string name, Func<ValueTask> action)
{
  var sw = System.Diagnostics.Stopwatch.StartNew();
  await action().ConfigureAwait(false);
  sw.Stop();
  Console.WriteLine($"[{sw.ElapsedMilliseconds,6} ms] {name}");
}

static string DescriptionKb(int i, int size = 2048)
{
  var phrase = $"Description {i}. Lorem ipsum dolor sit amet, consectetur adipiscing elit. ";
  return string.Concat(Enumerable.Repeat(phrase, (size / phrase.Length) + 1))[..size];
}

static async Task EnsureMssqlDatabaseAsync(string connectionString)
{
  var builder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };
  await using var conn = new SqlConnection(builder.ConnectionString);
  await conn.OpenAsync().ConfigureAwait(false);
  await using var cmd = conn.CreateCommand();
  cmd.CommandText = @"
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'FalcormTest')
BEGIN
  CREATE DATABASE FalcormTest;
END";
  await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
}

static async Task RunMssqlAsync(string connectionString)
{
  await using var conn = new SqlConnection(connectionString);
  await conn.OpenAsync().ConfigureAwait(false);
  var session = new DbSession(null!, conn);

  Timed("MSSQL Create tables", () => CreateMssqlTables(session));
  await TimedAsync("MSSQL Seed", () => SeedAsync(session)).ConfigureAwait(false);
}

static void CreateMssqlTables(DbSession session)
{
  session.Run($@"
IF OBJECT_ID('dbo.AllTypes', 'U') IS NULL
CREATE TABLE dbo.[AllTypes] (
  Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
  ColString nvarchar(max) NOT NULL,
  ColStringNull nvarchar(max) NULL,
  ColBool bit NOT NULL,
  ColBoolNull bit NULL,
  ColInt int NOT NULL,
  ColIntNull int NULL,
  ColBytes varbinary(max) NULL,
  ColByte tinyint NOT NULL,
  ColLong bigint NOT NULL,
  ColShort smallint NOT NULL,
  ColDecimal decimal(18,2) NOT NULL,
  ColDouble float NOT NULL,
  ColFloat real NOT NULL,
  ColDateTime datetime2 NOT NULL,
  ColDateTimeNull datetime2 NULL,
  ColDateTimeOffset datetimeoffset NOT NULL,
  ColGuid uniqueidentifier NOT NULL
);");

  session.Run($@"
IF OBJECT_ID('dbo.Simple', 'U') IS NULL
CREATE TABLE dbo.[Simple] (
  Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
  Ts datetime2 NOT NULL DEFAULT (GETUTCDATE()),
  ColChar nchar(1) NOT NULL DEFAULT N' ',
  Name nvarchar(255) NOT NULL,
  Description nvarchar(max) NULL,
  Tags nvarchar(max) NULL
);");

  session.Run($"IF OBJECT_ID('dbo.tr_Simple_Ts', 'TR') IS NOT NULL DROP TRIGGER dbo.tr_Simple_Ts;");
  session.Run($"CREATE TRIGGER dbo.tr_Simple_Ts ON dbo.[Simple] AFTER UPDATE AS UPDATE s SET Ts = GETUTCDATE() FROM dbo.[Simple] s INNER JOIN inserted i ON s.Id = i.Id;");

  var wideCols = string.Join(",\n  ", Enumerable.Range(1, 99).Select(i => $"S{i:D2} nvarchar(max) NULL"));
  session.Run($@"
IF OBJECT_ID('dbo.WideString', 'U') IS NULL
CREATE TABLE dbo.[WideString] (
  Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
  {Dbo.Raw(wideCols)}
);");
}

static async Task SeedAsync(DbSession session)
{
  var db = session.Dialect == Istarion.Falcorm.DbDialect.Mssql ? "MSSQL" : "Postgres";

  session.AllTypesEntities.DeleteAll();
  session.SimpleEntities.DeleteAll();
  session.WideStringEntities.DeleteAll();

  if (session.AllTypesEntities.FirstOrDefault($"Id = {1}") == null)
  {
    var sw = System.Diagnostics.Stopwatch.StartNew();
    Console.WriteLine($"Seeding {db} AllTypes (100k)...");
    const int batch = 5000;
    for (int b = 0; b < 100_000 / batch; b++)
    {
      for (int i = 0; i < batch; i++)
      {
        var id = b * batch + i + 1;
        session.AllTypesEntities.BatchInsert(new AllTypesEntity
        {
          ColString = $"s_{id}",
          ColBool = id % 2 == 0,
          ColInt = id,
          ColByte = (byte)(id % 256),
          ColLong = id,
          ColShort = (short)(id % 32767),
          ColDecimal = id,
          ColDouble = id,
          ColFloat = id,
          ColDateTime = DateTime.Now,
          ColDateTimeOffset = DateTimeOffset.UtcNow,
          ColGuid = Guid.NewGuid()
        }, false);
      }
      session.SaveChanges();
    }
    sw.Stop();
    Console.WriteLine($"[{sw.ElapsedMilliseconds,6} ms] {db} Seed AllTypes (100k)");
  }

  if (session.SimpleEntities.FirstOrDefault($"Id = {1}") == null)
  {
    var sw = System.Diagnostics.Stopwatch.StartNew();
    Console.WriteLine($"Seeding {db} Simple (1k)...");
    for (int i = 1; i <= 1000; i++)
    {
      session.SimpleEntities.BatchInsert(new SimpleEntity
      {
        Ts = DateTime.UtcNow,
        ColChar = (char)('A' + (i % 26)),
        Name = $"Name_{i}",
        Description = DescriptionKb(i),
        Tags = i % 2 == 0 ? "a,b" : null
      }, false);
    }
    session.SaveChanges();
    sw.Stop();
    Console.WriteLine($"[{sw.ElapsedMilliseconds,6} ms] {db} Seed Simple (1k)");
  }

  if (session.WideStringEntities.FirstOrDefault($"Id = {1}") == null)
  {
    var sw = System.Diagnostics.Stopwatch.StartNew();
    Console.WriteLine($"Seeding {db} WideString (10k)...");
    const int batch = 2000;
    for (int b = 0; b < 10_000 / batch; b++)
    {
      for (int i = 0; i < batch; i++)
      {
        var e = new WideStringEntity();
        for (int c = 1; c <= 99; c++)
          typeof(WideStringEntity).GetProperty($"S{c:D2}")!.SetValue(e, $"v_{b * batch + i + 1}_{c}");
        session.WideStringEntities.BatchInsert(e, false);
      }
      session.SaveChanges();
    }
    sw.Stop();
    Console.WriteLine($"[{sw.ElapsedMilliseconds,6} ms] {db} Seed WideString (10k)");
  }
}

static async Task RunPostgresAsync(string connectionString)
{
  await using var conn = new NpgsqlConnection(connectionString);
  await conn.OpenAsync().ConfigureAwait(false);
  var session = new DbSession(null!, conn);

  Timed("Postgres Create tables", () => CreatePostgresTables(session));
  await TimedAsync("Postgres Seed", () => SeedAsync(session)).ConfigureAwait(false);
}

static void CreatePostgresTables(DbSession session)
{
  // PostgreSQL: same case as MSSQL, no quotes — Postgres lowercases unquoted identifiers
  session.Run($@"
CREATE TABLE IF NOT EXISTS AllTypes (
  Id serial PRIMARY KEY,
  ColString text NOT NULL,
  ColStringNull text,
  ColBool boolean NOT NULL,
  ColBoolNull boolean,
  ColInt integer NOT NULL,
  ColIntNull integer,
  ColBytes bytea,
  ColByte smallint NOT NULL,
  ColLong bigint NOT NULL,
  ColShort smallint NOT NULL,
  ColDecimal numeric(18,2) NOT NULL,
  ColDouble double precision NOT NULL,
  ColFloat real NOT NULL,
  ColDateTime timestamp NOT NULL,
  ColDateTimeNull timestamp,
  ColDateTimeOffset timestamp with time zone NOT NULL,
  ColGuid uuid NOT NULL
);");

  session.Run($@"
CREATE TABLE IF NOT EXISTS Simple (
  Id serial PRIMARY KEY,
  Ts timestamp with time zone NOT NULL DEFAULT (now()),
  ColChar char(1) NOT NULL DEFAULT ' ',
  Name varchar(255) NOT NULL,
  Description text,
  Tags text
);");

  session.Run($@"
CREATE OR REPLACE FUNCTION simple_ts_update() RETURNS trigger AS $$
BEGIN
  NEW.Ts := now();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;
");
  session.Run($"DROP TRIGGER IF EXISTS tr_simple_ts ON Simple;");
  session.Run($"CREATE TRIGGER tr_simple_ts BEFORE UPDATE ON Simple FOR EACH ROW EXECUTE FUNCTION simple_ts_update();");

  var wideCols = string.Join(",\n  ", Enumerable.Range(1, 99).Select(i => $"S{i:D2} text"));
  session.Run($@"
CREATE TABLE IF NOT EXISTS WideString (
  Id serial PRIMARY KEY,
  {Dbo.Raw(wideCols)}
);");
}

static async Task RunReadTestsAsync(DbConnection conn)
{
  var session = new DbSession(null!, conn);

  // FirstOrDefault — sync only (API has no FirstOrDefaultAsync)
  Timed("  AllTypes FirstOrDefault Id=1 (sync)", () => _ = session.AllTypesEntities.FirstOrDefault($"Id = {1}"));
  Timed("  AllTypes FirstOrDefault Id=50000 (sync)", () => _ = session.AllTypesEntities.FirstOrDefault($"Id = {50000}"));
  Timed("  Simple FirstOrDefault Id=1 (sync)", () => _ = session.SimpleEntities.FirstOrDefault($"Id = {1}"));
  Timed("  WideString FirstOrDefault Id=1 (sync)", () => _ = session.WideStringEntities.FirstOrDefault($"Id = {1}"));

  // Take/ToList — sync and async
  Timed("  AllTypes Take 10 (sync)", () => _ = session.AllTypesEntities.Take(10).ToList());
  await TimedValueTaskAsync("  AllTypes Take 10 (async)", async () => await session.AllTypesEntities.Take(10).ToListAsync(default));
  Timed("  AllTypes Take 100 (sync)", () => _ = session.AllTypesEntities.Take(100).ToList());
  await TimedValueTaskAsync("  AllTypes Take 100 (async)", async () => await session.AllTypesEntities.Take(100).ToListAsync(default));
  Timed("  AllTypes Where ColInt=42 Take 1 (sync)", () => _ = session.AllTypesEntities.Where($"ColInt = {42}").Take(1).ToList());
  await TimedValueTaskAsync("  AllTypes Where ColInt=42 Take 1 (async)", async () => await session.AllTypesEntities.Where($"ColInt = {42}").Take(1).ToListAsync(default));

  Timed("  Simple ToList (1k) (sync)", () => _ = session.SimpleEntities.ToList());
  await TimedValueTaskAsync("  Simple ToList (1k) (async)", async () => await session.SimpleEntities.ToListAsync(default));
  Timed("  Simple Where Id>500 Take 20 (sync)", () => _ = session.SimpleEntities.Where($"Id > {500}").Take(20).ToList());
  await TimedValueTaskAsync("  Simple Where Id>500 Take 20 (async)", async () => await session.SimpleEntities.Where($"Id > {500}").Take(20).ToListAsync(default));
  Timed("  Simple Skip 100 Take 20 (sync)", () => _ = session.SimpleEntities.Skip(100).Take(20).ToList());
  await TimedValueTaskAsync("  Simple Skip 100 Take 20 (async)", async () => await session.SimpleEntities.Skip(100).Take(20).ToListAsync(default));
  Timed("  Simple OrderBy Id Take 20 (sync)", () => _ = session.SimpleEntities.OrderBy($"Id").Take(20).ToList());
  await TimedValueTaskAsync("  Simple OrderBy Id Take 20 (async)", async () => await session.SimpleEntities.OrderBy($"Id").Take(20).ToListAsync(default));

  Timed("  WideString Take 50 (sync)", () => _ = session.WideStringEntities.Take(50).ToList());
  await TimedValueTaskAsync("  WideString Take 50 (async)", async () => await session.WideStringEntities.Take(50).ToListAsync(default));
}
