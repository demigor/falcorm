using System.Data.Common;

namespace Istarion.Falcorm;

using Internals;

/// <summary>Lightweight database table accessor for entity type <typeparamref name="T"/> with mapper <typeparamref name="M"/>.</summary>
public readonly struct DbTable<T, M>(DbSession session) : IDbTable<T>, IDbMaterializer<T> where M : IDbMapper<T>
{
  /// <summary>Current database session used for all operations.</summary>
  public readonly DbSession Session = session;
  DbSession IDbTable<T>.Session => Session;
  /// <summary>Name of the table in the database.</summary>
  public string TableName => M.TableName;

  /// <summary>Enumerates all rows from the command result set, materializing each with the mapper.</summary>
  static IEnumerable<T> Enum(DbCommand command)
  {
    using var cmd = command;
    using var reader = cmd.ExecuteReader();
    var map = new byte[reader.FieldCount];

    M.CreateMap(reader, map.AsSpan());

    while (reader.Read())
      yield return M.Read(reader, map.AsSpan());
  }

  /// <summary>Materializes all rows from the command result set into a list.</summary>
  static List<T> ToList(DbCommand command)
  {
    using var cmd = command;
    using var reader = cmd.ExecuteReader();
    Span<byte> map = stackalloc byte[reader.FieldCount];
    M.CreateMap(reader, map);

    var result = new List<T>();

    while (reader.Read())
      result.Add(M.Read(reader, map));

    return result;
  }

  /// <summary>Reads the first (or single) row from the command; throws or returns default according to orDefault and single.</summary>
  static T? First(DbCommand command, bool orDefault, bool single)
  {
    using var cmd = command;
    using var reader = cmd.ExecuteReader();

    if (reader.Read())
    {
      Span<byte> map = stackalloc byte[reader.FieldCount];
      M.CreateMap(reader, map);
      var result = M.Read(reader, map);

      if (single && reader.Read())
        throw new InvalidOperationException("Sequence contains more than one element");

      return result;
    }

    return orDefault ? default : throw new InvalidOperationException("Sequence contains no elements");
  }

  /// <summary>Asynchronously reads the first (or single) row from the command; throws or returns default according to orDefault and single.</summary>
  static async ValueTask<T?> FirstAsync(DbCommand command, bool orDefault, bool single, CancellationToken ct)
  {
    await using var cmd = command;
    await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);

    if (await reader.ReadAsync(ct).ConfigureAwait(false))
    {
      Span<byte> map = stackalloc byte[reader.FieldCount];
      M.CreateMap(reader, map);
      var result = M.Read(reader, map);
      if (single && await reader.ReadAsync(ct).ConfigureAwait(false))
        throw new InvalidOperationException("Sequence contains more than one element");

      return result;
    }
    return orDefault ? default : throw new InvalidOperationException("Sequence contains no elements");
  }

  /// <summary>Asynchronously materializes all rows from the command result set into a list.</summary>
  static async ValueTask<List<T>> ToListAsync(DbCommand command, CancellationToken ct)
  {
    await using var cmd = command;
    await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);

    var map = new byte[reader.FieldCount];
    M.CreateMap(reader, map.AsSpan());

    var results = new List<T>();

    while (await reader.ReadAsync(ct).ConfigureAwait(false) && !ct.IsCancellationRequested)
      results.Add(M.Read(reader, map.AsSpan()));

    return results;
  }

  #region IDbMaterializer<T> Members

  T? IDbMaterializer<T>.First(DbCommand command, bool orDefault, bool single) => First(command, orDefault, single);
  ValueTask<T?> IDbMaterializer<T>.FirstAsync(DbCommand command, bool orDefault, bool single, CancellationToken ct) => FirstAsync(command, orDefault, single, ct);
  List<T> IDbMaterializer<T>.ToList(DbCommand command) => ToList(command);
  ValueTask<List<T>> IDbMaterializer<T>.ToListAsync(DbCommand command, CancellationToken ct) => ToListAsync(command, ct);
  IEnumerable<T> IDbMaterializer<T>.Enum(DbCommand command) => Enum(command);

  #endregion

  /// <summary>Reads the current row from the reader into the entity using the mapper.</summary>
  static void Reload(DbDataReader reader, T entity)
  {
    Span<byte> map = stackalloc byte[reader.FieldCount];
    M.CreateMap(reader, map);
    M.Read(reader, map, entity);
  }

  /// <summary>If the first column is 1, reloads the entity from the next result row; otherwise calls DmlFailed.</summary>
  static void ReloadResult(DbDataReader reader, T entity, CUD op)
  {
    if (reader.Read() && reader.GetInt32(0) == 1)
      Reload(reader, entity);
    else
      DmlFailed(entity, op);
  }

  /// <summary>Verifies the first column is 1 (success); otherwise calls DmlFailed for the given entity and operation.</summary>
  static void CheckResult(DbDataReader reader, T entity, CUD op)
  {
    if (reader.Read() && reader.GetInt32(0) == 1)
    {
      // 
    }
    else
      DmlFailed(entity, op);
  }

  /// <summary>Throws an appropriate exception when insert or optimistic concurrency fails.</summary>
  static void DmlFailed(T entity, CUD op)
  {
    if (op == CUD.Create)
      throw new InvalidOperationException("Insert error");
    else
      throw new InvalidOperationException("Optimistic locking error");
  }

  /// <summary>Executes the insert command; optionally reloads the entity from the first result row.</summary>
  static void Insert(DbCommand command, bool reload, T entity)
  {
    using var cmd = command;
    if (reload)
    {
      using var reader = cmd.ExecuteReader();
      if (reader.Read())
      {
        Reload(reader, entity);
        return;
      }
    }
    else
    {
      var cnt = cmd.ExecuteNonQuery();
      if (cnt == 1) return;
    }
    throw new InvalidOperationException("Insert failed");
  }

  /// <summary>Asynchronously executes the insert command; optionally reloads the entity from the first result row.</summary>
  static async ValueTask InsertAsync(DbCommand command, bool reload, T entity, CancellationToken ct = default)
  {
    await using var cmd = command;
    if (reload)
    {
      await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
      if (await reader.ReadAsync(ct).ConfigureAwait(false))
      {
        Reload(reader, entity);
        return;
      }
    }
    else
    {
      var cnt = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
      if (cnt == 1) return;
    }
    throw new InvalidOperationException("Insert failed");
  }

  /// <summary>Executes the update command if non-null; optionally reloads the entity from the result. Returns false when no update was needed.</summary>
  static bool Update(DbCommand? command, bool reload, T entity)
  {
    if (command == null) return false; // no update needed

    using var cmd = command;
    if (reload)
    {
      using var reader = cmd.ExecuteReader();
      if (reader.Read())
      {
        if (reader.GetInt32(0) == 0)
          throw new InvalidOperationException("Optimistic Locking");

        Reload(reader, entity);
        return true;
      }
    }
    else
    {
      var cnt = cmd.ExecuteNonQuery();
      if (cnt == 1) return true;
    }
    throw new InvalidOperationException("Update failed");
  }

  /// <summary>Asynchronously executes the update command if non-null; optionally reloads the entity. Returns false when no update was needed.</summary>
  static async ValueTask<bool> UpdateAsync(DbCommand? command, bool reload, T entity, CancellationToken ct = default)
  {
    if (command == null) return false; // no update needed

    await using var cmd = command;
    if (reload)
    {
      await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
      if (await reader.ReadAsync(ct).ConfigureAwait(false))
      {
        if (reader.GetInt32(0) == 0)
          throw new InvalidOperationException("Optimistic Locking");

        Reload(reader, entity);
        return true;
      }
    }
    else
    {
      var cnt = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
      if (cnt == 1) return true;
    }
    throw new InvalidOperationException("Update failed");
  }

  /// <summary>Inserts the entity into the table.</summary>
  /// <param name="entity">Entity to insert.</param>
  /// <param name="reload">If true, reloads the entity from the database after insert (e.g. to fill generated keys).</param>
  public void Insert(T entity, bool reload = true)
    => Insert(M.Insert(this.Builder(), entity, ref reload).BuildCommand(), reload, entity);

  /// <summary>Updates the entity in the table. Returns false if no columns changed (no update executed).</summary>
  /// <param name="entity">Entity to update.</param>
  /// <param name="snapshot">Optional snapshot for optimistic locking; if not provided, current entity state is used.</param>
  /// <param name="reload">If true, reloads the entity from the database after update.</param>
  /// <returns>True if an update was executed and succeeded; false if no update was needed.</returns>
  public bool Update(T entity, T? snapshot = default, bool reload = true)
    => Update(M.Update(this.Builder(), entity, snapshot, ref reload)?.BuildCommand(), reload, entity);

  /// <summary>Deletes the entity from the table.</summary>
  /// <param name="entity">Entity to delete.</param>
  /// <returns>True if exactly one row was deleted; otherwise false.</returns>
  public bool Delete(T entity)
    => M.Delete(this.Builder(), entity).BuildCommand().Run() == 1;

  /// <summary>Inserts the entity into the table asynchronously.</summary>
  /// <param name="entity">Entity to insert.</param>
  /// <param name="reload">If true, reloads the entity from the database after insert (e.g. to fill generated keys).</param>
  /// <param name="ct">Cancellation token.</param>
  public ValueTask InsertAsync(T entity, bool reload = true, CancellationToken ct = default)
    => InsertAsync(M.Insert(this.Builder(), entity, ref reload).BuildCommand(), reload, entity, ct);

  /// <summary>Updates the entity in the table asynchronously. Returns false if no columns changed (no update executed).</summary>
  /// <param name="entity">Entity to update.</param>
  /// <param name="snapshot">Optional snapshot for optimistic locking; if not provided, current entity state is used.</param>
  /// <param name="reload">If true, reloads the entity from the database after update.</param>
  /// <param name="ct">Cancellation token.</param>
  /// <returns>True if an update was executed and succeeded; false if no update was needed.</returns>
  public ValueTask<bool> UpdateAsync(T entity, T? snapshot = default, bool reload = true, CancellationToken ct = default)
    => UpdateAsync(M.Update(this.Builder(), entity, snapshot, ref reload)?.BuildCommand(), reload, entity, ct);

  /// <summary>Deletes the entity from the table asynchronously.</summary>
  /// <param name="entity">Entity to delete.</param>
  /// <param name="ct">Cancellation token.</param>
  /// <returns>True if exactly one row was deleted; otherwise false.</returns>
  public async ValueTask<bool> DeleteAsync(T entity, CancellationToken ct = default)
    => await M.Delete(this.Builder(), entity).BuildCommand().RunAsync(ct) == 1;


  /// <summary>Queues an insert of the entity to be executed in the current batch.</summary>
  /// <param name="entity">Entity to insert.</param>
  /// <param name="reload">If true, reloads the entity from the database after the batch executes (e.g. to fill generated keys).</param>
  public void BatchInsert(T entity, bool reload = true)
  {
    var self = this;
    Session.Batch((cmd, ref prev, out full) =>
    {
      var (update, keys, generated) = M.ColumnCounts;
      if (full = !prev.Fits(cmd, update + keys + (reload ? keys + generated : 0))) return null;

      var builder = self.Builder(cmd, prev);
      builder = M.Insert(builder, entity, ref reload);

      prev = builder;
      return reload ? (r => ReloadResult(r, entity, CUD.Create)) : null;
    }, entity, CUD.Create);
  }

  /// <summary>Queues an update of the entity to be executed in the current batch.</summary>
  /// <param name="entity">Entity to update.</param>
  /// <param name="snapshot">Optional snapshot for optimistic locking; if not provided, current entity state is used.</param>
  /// <param name="reload">If true, reloads the entity from the database after the batch executes.</param>
  public void BatchUpdate(T entity, T? snapshot = default, bool reload = true)
  {
    var self = this;
    Session.Batch((cmd, ref prev, out full) =>
    {
      var (update, keys, generated) = M.ColumnCounts;
      if (full = !prev.Fits(cmd, update + keys + (reload ? keys + generated : 0))) return null;

      var builder = self.Builder(cmd, prev);
      builder = M.Update(builder, entity, snapshot, ref reload);
      if (builder == null)
        return null;

      prev = builder;
      return reload ? (r => ReloadResult(r, entity, CUD.Update)) : (r => CheckResult(r, entity, CUD.Update));
    }, entity, CUD.Update);
  }

  /// <summary>Queues a delete of the entity to be executed in the current batch.</summary>
  /// <param name="entity">Entity to delete.</param>
  public void BatchDelete(T entity)
  {
    var self = this;
    Session.Batch((cmd, ref prev, out full) =>
    {
      var (_, keys, _) = M.ColumnCounts;
      if (full = !prev.Fits(cmd, keys)) return null;

      var builder = self.Builder(cmd, prev);
      builder = M.Delete(builder, entity);

      prev = builder;
      return r => CheckResult(r, entity, CUD.Delete);
    }, entity, CUD.Delete);
  }
}
