# Falcorm Integration Tests

Test project for running Falcorm against real databases (MSSQL and PostgreSQL).

## Entities

1. **AllTypesEntity** ([Table("AllTypes")]) — 19 columns of supported types: `int`, `string`, `bool`, `byte`, `byte[]`, `long`, `short`, `decimal`, `double`, `float`, `DateTime`, `DateTimeOffset`, `Guid` and their nullable variants. [Key] on Id, [DatabaseGenerated(Identity)].
2. **SimpleEntity** ([Table("Simple")]) — 5 columns: Id, Ts, Name, Description, Tags. [Key] on Id, [ConcurrencyCheck] on Ts, [DatabaseGenerated(Identity)] on Id.
3. **WideStringEntity** ([Table("WideString")]) — 100 string columns (Id + S01…S99). [Key] and [DatabaseGenerated(Identity)] on Id.

## Running via Aspire (recommended for development)

In the solution, set **AppHost** as the startup project and press F5. Aspire will start SQL Server and PostgreSQL containers, run testapp with the correct connection strings, and open the dashboard. See [Host/README.md](../Host/README.md) for more.

## Running via Docker Compose

From the `src` root:

```bash
docker compose up --build
```

- **mssql** — SQL Server 2022, port 1433, database `FalcormTest`, user `sa`, password `Falcorm_Test_123`.
- **postgres** — PostgreSQL 18, port 5432, database `falcormtest`, user `falcorm`, password `falcorm_test_123`.
- **testapp** — console app: creates tables if missing, seeds data on first run (AllTypes 100k, Simple 1k, WideString 10k rows per database).

## Debugging in Visual Studio

1. Install **Container Tools** in the Visual Studio installer.
2. In the solution, set **docker-compose** as the startup project (right-click the Docker project → Set as Startup Project).
3. Once, build and publish the test project in Debug (so the container mounts the folder with all dependencies):
   ```bash
   dotnet publish IntegrationTests/Falcorm.IntegrationTests.csproj -c Debug -o IntegrationTests/bin/Debug/net10.0/publish
   ```
4. Start debugging (F5). mssql, postgres, and testapp will start; the testapp container will wait for the debugger. VS will attach to the process in the container (via labels from `docker-compose.vs.debug.yml`).

After code changes, repeat step 3 (publish), then F5 again.

## Local run (without Docker)

1. Run MSSQL and Postgres locally (or only the DBs in Docker).
2. Run the project; set environment variables if needed:
   - `MssqlConnectionString`
   - `PostgresConnectionString`

By default, connection strings point to `localhost`.
