# Falcorm AppHost (Aspire)

Orchestrates integration tests via .NET Aspire: starts **SQL Server** and **PostgreSQL** containers, then runs the **IntegrationTests** project, which creates tables and seeds data.

## Running

1. Ensure **Docker** is installed and running.
2. In the solution, set **AppHost** as the startup project.
3. Run (F5 or Ctrl+F5). The Aspire dashboard will open; the testapp console app will create tables and seed data.

Connection strings for testapp are injected automatically (ConnectionStrings__FalcormTest, POSTGRESDB_URI / ConnectionStrings__postgresdb).

## Aspire vs Docker Compose

- **Docker Compose** (`docker-compose.dcproj`) — YAML-based; run containers and the app with `docker compose up`.
- **Aspire** — C#-based (AppHost), same containers and same testapp, plus a dashboard and single F5 run from VS.

Either option can be used as preferred.
