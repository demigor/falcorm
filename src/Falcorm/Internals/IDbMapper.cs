using System.Data.Common;

namespace Istarion.Falcorm.Internals;

/// <summary>
/// Database mapper interface for type <typeparamref name="T"/>. Defines table metadata,
/// column mapping, read logic, and SQL builder operations (delete, insert, update).
/// This interface is intended for code generation and is not used in normal application scenarios.
/// </summary>
/// <typeparam name="T">Entity type mapped to a database table.</typeparam>
public interface IDbMapper<T>
{
  /// <summary>Database table name for this entity.</summary>
  static abstract string TableName { get; }

  /// <summary>
  /// Column counts: update (writable), keys (primary key), and generated (e.g. identity, computed).
  /// </summary>
  static abstract (int update, int keys, int generated) ColumnCounts { get; }

  /// <summary>
  /// Builds a column-to-ordinal map from the current reader schema into <paramref name="map"/>.
  /// </summary>
  /// <param name="reader">Active data reader whose columns define the mapping.</param>
  /// <param name="map">Pre-allocated span to receive ordinal indices for each logical column.</param>
  static abstract void CreateMap(DbDataReader reader, Span<byte> map);

  /// <summary>
  /// Reads one entity from <paramref name="reader"/> using the pre-built <paramref name="map"/>.
  /// </summary>
  /// <param name="reader">Data reader positioned on the current row.</param>
  /// <param name="map">Column ordinal map produced by <see cref="CreateMap"/>.</param>
  /// <param name="entity">Optional existing instance to populate; if null, a new instance is created.</param>
  /// <returns>The populated entity.</returns>
  static abstract T Read(DbDataReader reader, Span<byte> map, T? entity = default);

  /// <summary>Appends a DELETE statement for <paramref name="entity"/> to the builder. Override to support deletes.</summary>
  static virtual SqlBuilder<T> Delete(SqlBuilder<T> builder, T entity) => throw new NotSupportedException();

  /// <summary>Appends an INSERT statement for <paramref name="entity"/>. <paramref name="reload"/> can request refetch. Override to support inserts.</summary>
  static virtual SqlBuilder<T> Insert(SqlBuilder<T> builder, T entity, ref bool reload) => throw new NotSupportedException();

  /// <summary>Appends an UPDATE statement when <paramref name="entity"/> differs from <paramref name="snapshot"/>. <paramref name="reload"/> can request refetch. Override to support updates.</summary>
  static virtual SqlBuilder<T>? Update(SqlBuilder<T> builder, T entity, T? snapshot, ref bool reload) => throw new NotSupportedException();
}