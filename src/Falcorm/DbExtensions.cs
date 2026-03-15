using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace Istarion.Falcorm;

using Internals;

/// <summary>Extension methods for <see cref="IDbTable{T}"/>, <see cref="SqlBuilder{T}"/>, <see cref="DbSession"/>, and related types to build and execute SQL with a fluent API.</summary>
public static class DbExtensions
{
  extension<T>(IDbTable<T> source)
  {
    /// <summary>Adds a WHERE clause to the query using the interpolated predicate.</summary>
    public SqlBuilder<T> Where([InterpolatedStringHandlerArgument("source")] ref SqlBuilderHandler<T> predicate)
      => predicate.Builder.Where_(predicate.Text);
    /// <summary>Adds a JOIN clause to the query using the interpolated predicate.</summary>
    public SqlBuilder<T> Join([InterpolatedStringHandlerArgument("source")] ref SqlBuilderHandler<T> join)
      => join.Builder.Join_(join.Text);

    /// <summary>Adds an ORDER BY clause to the query using the interpolated expression.</summary>
    public SqlBuilder<T> OrderBy([InterpolatedStringHandlerArgument("source")] ref SqlBuilderHandler<T> predicate)
      => predicate.Builder.OrderBy_(predicate.Text);

    /// <summary>Adds a GROUP BY clause to the query using the interpolated expression.</summary>
    public SqlBuilder<T> GroupBy([InterpolatedStringHandlerArgument("source")] ref SqlBuilderHandler<T> predicate)
      => predicate.Builder.GroupBy_(predicate.Text);

    /// <summary>Specifies the SELECT columns for the query using the interpolated expression.</summary>
    public SqlBuilder<T> Select([InterpolatedStringHandlerArgument("source")] ref SqlBuilderHandler<T> predicate)
      => predicate.Builder.Select_(predicate.Text);

    /// <summary>Adds UPDATE column setters using the interpolated expression.</summary>
    public SqlBuilder<T> Update([InterpolatedStringHandlerArgument("source")] ref SqlBuilderHandler<T> setters)
      => setters.Builder.Update_(setters.Text);

    /// <summary>Adds an INSERT statement with the given column list and interpolated values expression.</summary>
    public SqlBuilder<T> Insert(string columns, [InterpolatedStringHandlerArgument("source")] ref SqlBuilderHandler<T> values)
      => values.Builder.Insert_(columns, values.Text);

    /// <summary>Appends raw SQL to the builder using the interpolated string (parameters are still registered).</summary>
    public SqlBuilder<T> Raw([InterpolatedStringHandlerArgument("source")] ref SqlBuilderHandler<T> predicate)
      => predicate.Builder.Raw_(predicate.Text);

    /// <summary>Uses the given table name (optionally schema-qualified) for the query instead of the default table.</summary>
    /// <param name="tableName">The table name to use.</param>
    /// <returns>This builder for chaining.</returns>
    public SqlBuilder<T> Table(string tableName) => source.Builder().Table(tableName);

    /// <summary>Limits the result set to the specified number of rows (TOP/LIMIT).</summary>
    public SqlBuilder<T> Take(int count) => source.Builder().Take(count);

    /// <summary>Skips the specified number of rows (OFFSET).</summary>
    public SqlBuilder<T> Skip(int count) => source.Builder().Skip(count);

    /// <summary>Creates a new SQL builder for this table.</summary>
    public SqlBuilder<T> Builder() => source.Builder(source.Session.CreateCommand());
    /// <summary>Creates a builder bound to the given command and optional previous builder (for batching).</summary>
    internal SqlBuilder<T> Builder(DbCommand command, SqlBuilder? prev = null) => source.Session.Dialect switch { DbDialect.Mssql => new MssqlBuilder<T>(source, command) { _prev = prev }, _ => new NpgsqlBuilder<T>(source, command) { _prev = prev } };

    /// <summary>Returns the first row or default value if none.</summary>
    public T? FirstOrDefault() => source.Builder().FirstOrDefault();
    /// <summary>Returns the first row matching the predicate or default value if none.</summary>
    public T? FirstOrDefault([InterpolatedStringHandlerArgument("source")] ref SqlBuilderHandler<T> predicate) => predicate.Builder.FirstOrDefault(ref predicate);

    /// <summary>Returns the first row; throws if no rows.</summary>
    public T First() => source.Builder().First();
    /// <summary>Returns the first row matching the predicate; throws if no rows.</summary>
    public T First([InterpolatedStringHandlerArgument("source")] ref SqlBuilderHandler<T> predicate) => predicate.Builder.First(ref predicate);

    /// <summary>Returns the single row or default value; throws if more than one row.</summary>
    public T? SingleOrDefault() => source.Builder().SingleOrDefault();
    /// <summary>Returns the single row matching the predicate or default; throws if more than one.</summary>
    public T? SingleOrDefault([InterpolatedStringHandlerArgument("source")] ref SqlBuilderHandler<T> predicate) => predicate.Builder.SingleOrDefault(ref predicate);

    /// <summary>Returns the single row; throws if zero or more than one row.</summary>
    public T Single() => source.Builder().Single();
    /// <summary>Returns the single row matching the predicate; throws if zero or more than one.</summary>
    public T Single([InterpolatedStringHandlerArgument("source")] ref SqlBuilderHandler<T> predicate) => predicate.Builder.Single(ref predicate);

    /// <summary>Asynchronously returns the first row or default value if none.</summary>
    public ValueTask<T?> FirstOrDefaultAsync(CancellationToken ct = default) => source.Builder().FirstOrDefaultAsync(ct);
    /// <summary>Asynchronously returns the first row matching the predicate or default value if none.</summary>
    public ValueTask<T?> FirstOrDefaultAsync([InterpolatedStringHandlerArgument("source")] ref SqlBuilderHandler<T> predicate, CancellationToken ct = default) => predicate.Builder.FirstOrDefaultAsync(ref predicate, ct);

    /// <summary>Asynchronously returns the first row; throws if no rows.</summary>
    public ValueTask<T> FirstAsync(CancellationToken ct = default) => source.Builder().FirstAsync(ct);
    /// <summary>Asynchronously returns the first row matching the predicate; throws if no rows.</summary>
    public ValueTask<T> FirstAsync([InterpolatedStringHandlerArgument("source")] ref SqlBuilderHandler<T> predicate, CancellationToken ct = default) => predicate.Builder.FirstAsync(ref predicate, ct);

    /// <summary>Asynchronously returns the single row or default; throws if more than one row.</summary>
    public ValueTask<T?> SingleOrDefaultAsync(CancellationToken ct = default) => source.Builder().SingleOrDefaultAsync(ct);
    /// <summary>Asynchronously returns the single row matching the predicate or default; throws if more than one.</summary>
    public ValueTask<T?> SingleOrDefaultAsync([InterpolatedStringHandlerArgument("source")] ref SqlBuilderHandler<T> predicate, CancellationToken ct = default) => predicate.Builder.SingleOrDefaultAsync(ref predicate, ct);

    /// <summary>Asynchronously returns the single row; throws if zero or more than one row.</summary>
    public ValueTask<T> SingleAsync(CancellationToken ct = default) => source.Builder().SingleAsync(ct);
    /// <summary>Asynchronously returns the single row matching the predicate; throws if zero or more than one.</summary>
    public ValueTask<T> SingleAsync([InterpolatedStringHandlerArgument("source")] ref SqlBuilderHandler<T> predicate, CancellationToken ct = default) => predicate.Builder.SingleAsync(ref predicate, ct);

    /// <summary>Returns true if the query returns at least one row.</summary>
    public bool Any() => source.Builder().Any();
    /// <summary>Returns true if the query with the given predicate returns at least one row.</summary>
    public bool Any([InterpolatedStringHandlerArgument("source")] ref SqlBuilderHandler<T> predicate) => predicate.Builder.Any(ref predicate);

    /// <summary>Asynchronously returns true if the query returns at least one row.</summary>
    public ValueTask<bool> AnyAsync(CancellationToken ct = default) => source.Builder().AnyAsync(ct);
    /// <summary>Asynchronously returns true if the query with the given predicate returns at least one row.</summary>
    public ValueTask<bool> AnyAsync([InterpolatedStringHandlerArgument("source")] ref SqlBuilderHandler<T> predicate, CancellationToken ct = default) => predicate.Builder.AnyAsync(ref predicate, ct);


    /// <summary>Executes the query and returns all rows as a list.</summary>
    public List<T> ToList() => source.Builder().ToList();
    /// <summary>Executes the query with the given predicate and returns all matching rows as a list.</summary>
    public List<T> ToList([InterpolatedStringHandlerArgument("source")] ref SqlBuilderHandler<T> predicate) => predicate.Builder.ToList(ref predicate);

    /// <summary>Returns an enumerable that executes the query and yields rows.</summary>
    public IEnumerable<T> Enum() => source.Builder().Enum();
    /// <summary>Returns an enumerable that executes the query with the predicate and yields matching rows.</summary>
    public IEnumerable<T> Enum([InterpolatedStringHandlerArgument("source")] ref SqlBuilderHandler<T> predicate) => predicate.Builder.Enum(ref predicate);

    /// <summary>Asynchronously executes the query and returns all rows as a list.</summary>
    public ValueTask<List<T>> ToListAsync(CancellationToken ct = default) => source.Builder().ToListAsync(ct);
    /// <summary>Asynchronously executes the query with the predicate and returns all matching rows as a list.</summary>
    public ValueTask<List<T>> ToListAsync([InterpolatedStringHandlerArgument("source")] ref SqlBuilderHandler<T> predicate, CancellationToken ct = default) => predicate.Builder.ToListAsync(ref predicate, ct);

    /// <summary>Combines multiple subqueries with UNION or UNION ALL.</summary>
    public SqlBuilder<T> Union(IEnumerable<Func<SqlBuilder<T>, SqlBuilder>> queries, bool unionAll = false)
    {
      var builder = source.Builder();
      return builder.Union(queries.Select(q => q(builder.New())), unionAll);
    }
    /// <summary>Combines multiple subqueries with UNION ALL.</summary>
    public SqlBuilder<T> UnionAll(IEnumerable<Func<SqlBuilder<T>, SqlBuilder>> queries) => source.Union(queries, true);

    /// <summary>Builds a DELETE statement with the given predicate.</summary>
    public SqlBuilder<T> Delete([InterpolatedStringHandlerArgument("source")] ref SqlBuilderHandler<T> predicate) => predicate.Builder.Delete(ref predicate);

    /// <summary>Deletes all rows from the table and returns the number of affected rows.</summary>
    public int DeleteAll() => source.Builder().Delete().Run();
  }


  extension<T>(IDbTable<T> source) where T : class, ICloneable<T>
  {
    /// <summary>Clones the entity, applies the updater, then batch-updates only changed properties; optionally reloads from DB.</summary>
    /// <param name="entity">Entity to update.</param>
    /// <param name="updater">Action that modifies the entity (e.g. set properties from a DTO).</param>
    /// <param name="reload">If true, reloads the entity from the database after the batch executes.</param>
    /// <returns>The same entity instance (after update and optional reload).</returns>
    public T BatchUpdate(T entity, Action<T> updater, bool reload = true)
    {
      var origin = entity.Clone();
      updater(entity);
      source.BatchUpdate(entity, origin, reload);
      return entity;
    }

    /// <summary>Executes multiple SQL statements for this table in a single database round-trip. Use the provided <see cref="SqlBuilder{T}"/> to chain Where, Delete, Update, Insert, Raw, etc.; all commands are sent together when the batch completes.</summary>
    /// <param name="commandBuilder">Callback that receives the builder and adds one or more DML statements.</param>
    public void Batch(Action<SqlBuilder<T>> commandBuilder)
    {
      source.Session.Batch((cmd, ref prev, out full) =>
      {
        full = false;
        var builder = source.Builder(cmd, prev);
        commandBuilder(builder);
        prev = builder;
        return null;
      }, null, CUD.Delete);
    }
  }

  extension(SqlBuilder? builder)
  {
    /// <summary>Returns whether the command can accept the additional parameter count without exceeding the builder/dialect limit.</summary>
    /// <param name="cmd">The command whose current parameter count is considered.</param>
    /// <param name="paramCount">Number of additional parameters to add.</param>
    /// <returns>True if adding paramCount parameters would not exceed the limit.</returns>
    public bool Fits(DbCommand cmd, int paramCount) => builder == null || builder.MaxParamCount >= cmd.Parameters.Count + paramCount;
  }

  extension<T>(SqlBuilder<T> builder)
  {
    /// <summary>Adds a WHERE clause with the given predicate string.</summary>
    public SqlBuilder<T> Where(string predicate) => builder.Where_(predicate);
    /// <summary>Adds a JOIN clause with the given join clause string.</summary>
    public SqlBuilder<T> Join(string join) => builder.Join_(join);
    /// <summary>Adds an ORDER BY clause with the given column list.</summary>
    public SqlBuilder<T> OrderBy(string columns) => builder.OrderBy_(columns);
    /// <summary>Adds a GROUP BY clause with the given column list.</summary>
    public SqlBuilder<T> GroupBy(string columns) => builder.GroupBy_(columns);
    /// <summary>Specifies the SELECT columns for the query.</summary>
    public SqlBuilder<T> Select(string columns) => builder.Select_(columns);
    /// <summary>Appends raw SQL to the builder.</summary>
    public SqlBuilder<T> Raw(string statement) => builder.Raw_(statement);
    /// <summary>Formats the interpolated string using the builder's dialect and registers interpolated values as parameters; returns the resulting SQL text.</summary>
    /// <returns>The formatted SQL string with placeholders replaced by parameter references.</returns>
    public string Format([InterpolatedStringHandlerArgument("builder")] ref SqlBuilderHandler<T> sql) => sql.Text;
  }

  /// <summary>Applies the handler to the builder and returns the built command with SQL and parameters set.</summary>
  static DbCommand BuildCommand<T>(ref SqlBuilderHandler<T> command)
    => command.Builder.Raw(ref command).BuildCommand();

  extension(DbSession session)
  {
    /// <summary>Executes the command as ExecuteNonQuery and returns the number of affected rows.</summary>
    public int Run([InterpolatedStringHandlerArgument("session")] ref SqlBuilderHandler<int> command)
      => BuildCommand(ref command).Run();

    /// <summary>Asynchronously executes the command as ExecuteNonQuery and returns the number of affected rows.</summary>
    public ValueTask<int> RunAsync([InterpolatedStringHandlerArgument("session")] ref SqlBuilderHandler<int> command, CancellationToken ct = default)
      => BuildCommand(ref command).RunAsync(ct);

    /// <summary>Executes the command as ExecuteScalar and returns the first column of the first row.</summary>
    public T? Run<T>([InterpolatedStringHandlerArgument("session")] ref SqlBuilderHandler<T> command)
      => BuildCommand(ref command).Run<T>();

    /// <summary>Asynchronously executes the command as ExecuteScalar and returns the first column of the first row.</summary>
    public ValueTask<T?> RunAsync<T>([InterpolatedStringHandlerArgument("session")] ref SqlBuilderHandler<T> command, CancellationToken ct = default)
      => BuildCommand(ref command).RunAsync<T>(ct);
  }
}