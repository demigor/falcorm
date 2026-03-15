namespace Istarion.Falcorm;

/// <summary>
/// Identifies the SQL dialect used by the database provider.
/// Used to tailor SQL generation and behavior per database engine.
/// </summary>
public enum DbDialect
{
    /// <summary>Microsoft SQL Server.</summary>
    Mssql,

    /// <summary>PostgreSQL.</summary>
    Postgres
}
