using System.Data.Common;

namespace Istarion.Falcorm.Internals;

/// <summary>
/// Materializes database query results from a <see cref="DbCommand"/> into entities of type <typeparamref name="T"/>.
/// Extends <see cref="IDbTable{T}"/> with synchronous and asynchronous enumeration and single-result operations.
/// </summary>
/// <typeparam name="T">The entity type to materialize from the command result set.</typeparam>
public interface IDbMaterializer<T> : IDbTable<T>
{
  /// <summary>Enumerates rows from the command result set, materializing each as an instance of <typeparamref name="T"/>.</summary>
  /// <param name="command">The executed command whose result set is read.</param>
  /// <returns>An enumerable sequence of materialized entities.</returns>
  IEnumerable<T> Enum(DbCommand command);

  /// <summary>Materializes all rows from the command result set into a list of <typeparamref name="T"/>.</summary>
  /// <param name="command">The executed command whose result set is read.</param>
  /// <returns>A list of materialized entities.</returns>
  List<T> ToList(DbCommand command);

  /// <summary>Returns the first (or single) row from the command result set as <typeparamref name="T"/>.</summary>
  /// <param name="command">The executed command whose result set is read.</param>
  /// <param name="orDefault">If true, return default when no row; otherwise throw when no row.</param>
  /// <param name="single">If true, require exactly one row (throw when zero or more than one).</param>
  /// <returns>The first/single entity, or default when allowed and no row exists.</returns>
  T? First(DbCommand command, bool orDefault, bool single);

  /// <summary>Asynchronously materializes all rows from the command result set into a list of <typeparamref name="T"/>.</summary>
  /// <param name="command">The executed command whose result set is read.</param>
  /// <param name="ct">Cancellation token.</param>
  /// <returns>A value task that completes with a list of materialized entities.</returns>
  ValueTask<List<T>> ToListAsync(DbCommand command, CancellationToken ct);

  /// <summary>Asynchronously returns the first (or single) row from the command result set as <typeparamref name="T"/>.</summary>
  /// <param name="command">The executed command whose result set is read.</param>
  /// <param name="orDefault">If true, return default when no row; otherwise throw when no row.</param>
  /// <param name="single">If true, require exactly one row (throw when zero or more than one).</param>
  /// <param name="ct">Cancellation token.</param>
  /// <returns>A value task that completes with the first/single entity, or default when allowed and no row exists.</returns>
  ValueTask<T?> FirstAsync(DbCommand command, bool orDefault, bool single, CancellationToken ct);
}
