namespace Istarion.Falcorm;

/// <summary>
/// Immutable database object identifier (table, column, function, or raw SQL).
/// </summary>
public readonly struct Dbo
{
  /// <summary>Kind of database object reference.</summary>
  public enum IdType
  {
    /// <summary>Table name.</summary>
    TN,
    /// <summary>Column name (optionally qualified by table).</summary>
    CN,
    /// <summary>Function name.</summary>
    FN,
    /// <summary>Raw SQL fragment.</summary>
    Raw,
    /// <summary>Raw SQL fragment materialized in Sql Server only.</summary>
    Mssql,
    /// <summary>Raw SQL fragment materialized in Postgres only.</summary>
    Npgsql
  }

  Dbo(string? name, string? schema, IdType type)
  {
    Name = name;
    Schema = schema;
    Type = type;
  }

  /// <summary>Object name (e.g. table, column, or function name); for Raw, holds the SQL text.</summary>
  public readonly string? Name;
  /// <summary>Schema or table qualifier, depending on <see cref="IdType"/>.</summary>
  public readonly string? Schema;
  /// <summary>Identifier type (TN/CN/FN/Raw).</summary>
  public readonly IdType Type;

  /// <summary>Creates a function reference.</summary>
  public static Dbo FN(string name, string? schema = null) => new(name, schema, IdType.FN);
  /// <summary>Creates a column reference, optionally qualified by table name/alias.</summary>
  public static Dbo CN(string name, string? table = null) => new(name, table, IdType.CN);
  /// <summary>Creates a table reference.</summary>
  public static Dbo TN(string name, string? schema = null) => new(name, schema, IdType.TN);
  /// <summary>Creates a raw SQL fragment reference.</summary>
  public static Dbo Raw(string? sql) => new(sql, null, IdType.Raw);
  /// <summary>Creates a raw Mssql only SQL fragment reference.</summary>
  public static Dbo Mssql(string? sql) => new(sql, null, IdType.Mssql);
  /// <summary>Creates a raw Postgres only SQL fragment reference.</summary>
  public static Dbo Postgres(string? sql) => new(sql, null, IdType.Npgsql);
}