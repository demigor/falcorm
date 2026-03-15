namespace Istarion.Falcorm;

/// <summary>
/// Database table access interface for entities of type <typeparamref name="T"/>.
/// Provides synchronous and asynchronous CRUD operations, including batched variants.
/// </summary>
/// <typeparam name="T">Entity type mapped to the table.</typeparam>
public interface IDbTable<T>
{
  /// <summary>Current database session used for all operations.</summary>
  DbSession Session { get; }

  /// <summary>Name of the database table.</summary>
  public string TableName { get; }

  /// <summary>Inserts the entity into the table. Optionally reloads it from the database after insert.</summary>
  /// <param name="entity">Entity to insert.</param>
  /// <param name="reload">If true, reloads the entity after insert (e.g. to get database generated values like identity or computed columns).</param>
  void Insert(T entity, bool reload = true);

  /// <summary>Updates the entity in the table. Optionally uses a snapshot for change detection and reloads after update.</summary>
  /// <param name="entity">Entity with new values.</param>
  /// <param name="snapshot">Optional previous state for change detection; if null, all columns may be updated.</param>
  /// <param name="reload">If true, reloads the entity after update.</param>
  /// <returns>True if the entity was updated, false if no changes or row not found.</returns>
  bool Update(T entity, T? snapshot = default, bool reload = true);

  /// <summary>Deletes the entity from the table.</summary>
  /// <param name="entity">Entity to delete (typically identified by primary key).</param>
  /// <returns>True if a row was deleted, false otherwise.</returns>
  bool Delete(T entity);

  /// <summary>Asynchronously inserts the entity. Optionally reloads it after insert.</summary>
  ValueTask InsertAsync(T entity, bool reload = true, CancellationToken ct = default);

  /// <summary>Asynchronously updates the entity. Optionally uses a snapshot and reloads after update.</summary>
  /// <returns>True if the entity was updated, false otherwise.</returns>
  ValueTask<bool> UpdateAsync(T entity, T? snapshot = default, bool reload = true, CancellationToken ct = default);

  /// <summary>Asynchronously deletes the entity from the table.</summary>
  /// <returns>True if a row was deleted, false otherwise.</returns>
  ValueTask<bool> DeleteAsync(T entity, CancellationToken ct = default);


  /// <summary>Queues an insert for batch execution. Optionally reloads the entity after the batch is flushed.</summary>
  void BatchInsert(T entity, bool reload = true);

  /// <summary>Queues an update for batch execution. Optionally uses a snapshot and reloads after the batch is flushed.</summary>
  void BatchUpdate(T entity, T? snapshot = default, bool reload = true);

  /// <summary>Queues a delete for batch execution.</summary>
  void BatchDelete(T entity);
}