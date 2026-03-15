using System.Data;
using System.Data.Common;

namespace Istarion.Falcorm;

/// <summary>Create, Update, or Delete operation type.</summary>
public enum CUD
{
  /// <summary>Insert (create) operation.</summary>
  Create,
  /// <summary>Update operation.</summary>
  Update,
  /// <summary>Delete operation.</summary>
  Delete
}

/// <summary>Strategy for executing batched SQL when saving changes.</summary>
public enum DbSaveStrategy
{
  /// <summary>Use dialect default (buffered for Postgres, unbuffered for MSSQL).</summary>
  Default,
  /// <summary>Buffer multiple statements and execute with multiple result sets.</summary>
  Buffered,
  /// <summary>Execute each statement separately.</summary>
  Unbuffered
}


/// <summary>Database session that manages a connection, transactions, and batched save operations.</summary>
public class DbSession(IServiceProvider sp, DbConnection conn)
{
  /// <summary>Service provider for resolving dependencies.</summary>
  public readonly IServiceProvider ServiceProvider = sp;
  /// <summary>Underlying database connection.</summary>
  public readonly DbConnection Connection = conn;
  /// <summary>Detected SQL dialect (Postgres or MSSQL).</summary>
  public readonly DbDialect Dialect = DetectDialect(conn);
  /// <summary>Current transaction, or null if none is active.</summary>
  public DbTransaction? Transaction => _transaction;
  DbTransaction? _transaction;

  /// <summary>Detects the SQL dialect from the connection type (Npgsql → Postgres, otherwise MSSQL).</summary>
  static DbDialect DetectDialect(DbConnection conn)
  {
    var n = conn.GetType().Name;
    return n.Length == 16 && n[0] == 'N' ? DbDialect.Postgres : DbDialect.Mssql;
  }

  /// <summary>Starts a new database transaction. Throws if a transaction is already active.</summary>
  public void BeginTransaction()
  {
    if (_transaction != null)
      throw new InvalidOperationException("Transaction is already started");

    _transaction = Connection.BeginTransaction();
  }

  /// <summary>Commits the current transaction. Throws if no transaction is active.</summary>
  public void Commit()
  {
    if (_transaction == null)
      throw new InvalidOperationException("No running Transaction");

    _transaction.Commit();
    _transaction = null;
  }

  /// <summary>Rolls back the current transaction, if any. No-op when no transaction is active.</summary>
  public void Rollback()
  {
    if (_transaction == null) return;
    _transaction.Rollback();
    _transaction = null;
  }

  /// <summary>Creates a command bound to this session's connection and current transaction.</summary>
  /// <returns>A new <see cref="DbCommand"/> instance.</returns>
  public DbCommand CreateCommand()
  {
    var cmd = Connection.CreateCommand();
    cmd.Transaction = _transaction;
    return cmd;
  }

  /// <summary>Ensures the connection is open; reopens if broken. Calls <see cref="AfterOpen"/> when opening.</summary>
  protected internal void EnsureOpen()
  {
    if (Connection.State == ConnectionState.Broken)
    {
      Connection.Close();
    }

    if (Connection.State != ConnectionState.Open)
    {
      Connection.Open();
      AfterOpen();
    }
  }

  /// <summary>Override to run logic right after the connection is opened.</summary>
  protected virtual void AfterOpen() { }
  /// <summary>Override to run logic before batched SQL is executed.</summary>
  protected virtual void BeforeSave() { }
  /// <summary>Override to run logic after batched SQL is executed but before commit.</summary>
  protected virtual void AfterSave() { }
  /// <summary>Override to run logic after the transaction is committed.</summary>
  protected virtual void AfterCommit() { }

  /// <summary>Executes all batched create/update/delete operations in a single transaction.</summary>
  /// <param name="strategy">How to batch SQL execution (default, buffered, or unbuffered).</param>
  public void SaveChanges(DbSaveStrategy strategy = DbSaveStrategy.Default)
  {
    if (_batch.Count == 0) return;

    BeginTransaction();
    try
    {
      BeforeSave();

      if (strategy == DbSaveStrategy.Buffered || (strategy == DbSaveStrategy.Default && Dialect == DbDialect.Postgres))
        BufferedBatch();
      else
        UnbufferedBatch();

      AfterSave();

      Commit();

      AfterCommit();

    }
    finally
    {
      _batch.Clear();
      Rollback();
    }
  }

  /// <summary>Executes each batched statement separately and processes its result set before moving to the next.</summary>
  void UnbufferedBatch()
  {
    if (_batch.Count > 0)
    {
      EnsureOpen();
      using var cmd = CreateCommand();
      foreach (var (action, _, _) in _batch)
      {
        var builder = default(SqlBuilder);
        var handler = action(cmd, ref builder, out var _);
        if (builder != null)
        {
          cmd.CommandText = builder.Sql();
          using var reader = cmd.ExecuteReader();
          handler?.Invoke(reader);
          cmd.Parameters.Clear();
        }
      }
    }
  }

  /// <summary>Buffers multiple statements into one command and executes with multiple result sets (used for Postgres by default).</summary>
  void BufferedBatch()
  {
    if (_batch.Count > 0)
    {
      EnsureOpen();
      using var cmd = CreateCommand();
      var handlers = new List<Action<DbDataReader>>(_batch.Count);
      var builder = default(SqlBuilder);
      foreach (var (action, _, _) in _batch)
      {
        var handler = action(cmd, ref builder, out var full);
        if (full)
        {
          FlushBuffer(cmd, handlers, builder!);
          builder = null;
          handler = action(cmd, ref builder, out _);
        }

        if (handler != null)
          handlers.Add(handler);
      }
      if (builder != null)
        FlushBuffer(cmd, handlers, builder);
    }
  }

  /// <summary>Executes the builder's SQL, runs all result-set handlers in order, then clears the command parameters and handler list.</summary>
  static void FlushBuffer(DbCommand cmd, List<Action<DbDataReader>> handlers, SqlBuilder builder)
  {
    if (builder != null)
    {
      cmd.CommandText = builder.Sql();
      using var reader = cmd.ExecuteReader();

      var hasReaders = false;
      foreach (var handler in handlers)
      {
        if (hasReaders && !reader.NextResult())
          throw new InvalidOperationException("Query/Result mismatch");

        handler(reader);
        hasReaders = true;
      }
      cmd.Parameters.Clear();
      handlers.Clear();
    }
  }

  /// <summary>Pending create/update/delete operations to run on <see cref="SaveChanges"/>.</summary>
  protected List<(BatchAction, object? entity, CUD op)> _batch = [];

  /// <summary>Adds a batched operation (action, entity, and CUD type) to be executed on <see cref="SaveChanges"/>.</summary>
  protected internal void Batch(BatchAction action, object? entity, CUD op) => _batch.Add((action, entity, op));

  /// <summary>Builds SQL and returns a reader handler; <c>full</c> indicates the builder should be flushed before continuing.</summary>
  protected internal delegate Action<DbDataReader>? BatchAction(DbCommand cmd, ref SqlBuilder? builder, out bool full);
}
