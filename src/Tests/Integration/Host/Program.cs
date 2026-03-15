using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// Fixed SA password so that with Persistent+DataVolume the container and connection string match (avoids "Password did not match").
var mssqlPassword = builder.AddParameter("mssql-password", "Demigor77!", secret: true);
// SQL Server (container). Persistent + DataVolume.
var mssql = builder.AddSqlServer("mssql", password: mssqlPassword)
    .WithImage("mssql/server", "2022-latest")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);
var mssqlDb = mssql.AddDatabase("falcormMssql");

// Fixed Postgres password for Persistent+DataVolume.
var postgresPassword = builder.AddParameter("postgres-password", "Demigor77!", secret: true);
// PostgreSQL (container)
var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithImage("postgres", "18.3-alpine")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);
var pgDb = postgres.AddDatabase("falcormPostgres");

// Integration tests console app — runs after both DBs are ready
var testApp = builder.AddProject<Falcorm_Test>("testapp")
    .WithReference(mssqlDb)
    .WithReference(pgDb)
    .WaitFor(mssqlDb)
    .WaitFor(pgDb);

builder.Build().Run();
