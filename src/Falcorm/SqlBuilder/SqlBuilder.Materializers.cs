using System.ComponentModel;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace Istarion.Falcorm;

using Internals;

public partial class SqlBuilder<T>
{
  /// <summary>Builds a command that returns 1 if the query has any rows, 0 otherwise (EXISTS).</summary>
  DbCommand BuildAnyCommand() => BuildCommand(sb => sb.Append("select case when exists ("), sb => sb.Append(") then 1 else 0 end"));

  /// <summary>Returns true if the query returns at least one row.</summary>
  public bool Any()
    => BuildAnyCommand().Run<int>() == 1;

  /// <summary>Returns true if the query with the given predicate returns at least one row.</summary>
  public bool Any([InterpolatedStringHandlerArgument("")] ref SqlBuilderHandler<T> predicate)
    => Where(ref predicate).Any();

  /// <summary>Asynchronously returns true if the query returns at least one row.</summary>
  public async ValueTask<bool> AnyAsync(CancellationToken ct = default)
    => await BuildAnyCommand().RunAsync<int>(ct) == 1;

  /// <summary>Asynchronously returns true if the query with the given predicate returns at least one row.</summary>
  public ValueTask<bool> AnyAsync([InterpolatedStringHandlerArgument("")] ref SqlBuilderHandler<T> predicate, CancellationToken ct = default)
    => Where(ref predicate).AnyAsync(ct);

  /// <summary>Returns the first element of the query result, or default(T) if the result is empty.</summary>
  public T? FirstOrDefault()
  {
    Take(1);
    return _materializer.First(BuildCommand(), true, false);
  }
  /// <summary>Returns the first element that matches the predicate, or default(T) if none match.</summary>
  public T? FirstOrDefault([InterpolatedStringHandlerArgument("")] ref SqlBuilderHandler<T> predicate)
    => Where(ref predicate).FirstOrDefault();

  /// <summary>Returns the first element of the query result. Throws if the result is empty.</summary>
  public T First()
  {
    Take(1);
    return _materializer.First(BuildCommand(), false, false)!;
  }
  /// <summary>Returns the first element that matches the predicate. Throws if none match.</summary>
  public T First([InterpolatedStringHandlerArgument("")] ref SqlBuilderHandler<T> predicate)
    => Where(ref predicate).First();

  /// <summary>Returns the single element of the query result, or default(T) if empty. Throws if more than one element.</summary>
  public T? SingleOrDefault()
  {
    Take(2);
    return _materializer.First(BuildCommand(), true, true);
  }
  /// <summary>Returns the single element that matches the predicate, or default(T) if none. Throws if more than one match.</summary>
  public T? SingleOrDefault([InterpolatedStringHandlerArgument("")] ref SqlBuilderHandler<T> predicate)
    => Where(ref predicate).SingleOrDefault();

  /// <summary>Returns the single element of the query result. Throws if zero or more than one element.</summary>
  public T Single()
  {
    Take(2);
    return _materializer.First(BuildCommand(), false, true)!;
  }
  /// <summary>Returns the single element that matches the predicate. Throws if zero or more than one match.</summary>
  public T Single([InterpolatedStringHandlerArgument("")] ref SqlBuilderHandler<T> predicate)
    => Where(ref predicate).Single();

  /// <summary>Asynchronously returns the first element of the query result, or default(T) if the result is empty.</summary>
  public ValueTask<T?> FirstOrDefaultAsync(CancellationToken ct = default)
  {
    Take(1);
    return _materializer.FirstAsync(BuildCommand(), true, false, ct);
  }
  /// <summary>Asynchronously returns the first element that matches the predicate, or default(T) if none match.</summary>
  public ValueTask<T?> FirstOrDefaultAsync([InterpolatedStringHandlerArgument("")] ref SqlBuilderHandler<T> predicate, CancellationToken ct = default)
    => Where(ref predicate).FirstOrDefaultAsync(ct);

  /// <summary>Asynchronously returns the first element of the query result. Throws if the result is empty.</summary>
  public ValueTask<T> FirstAsync(CancellationToken ct = default)
  {
    Take(1);
    return _materializer.FirstAsync(BuildCommand(), false, false, ct)!;
  }
  /// <summary>Asynchronously returns the first element that matches the predicate. Throws if none match.</summary>
  public ValueTask<T> FirstAsync([InterpolatedStringHandlerArgument("")] ref SqlBuilderHandler<T> predicate, CancellationToken ct = default)
    => Where(ref predicate).FirstAsync(ct);

  /// <summary>Asynchronously returns the single element of the query result, or default(T) if empty. Throws if more than one element.</summary>
  public ValueTask<T?> SingleOrDefaultAsync(CancellationToken ct = default)
  {
    Take(2);
    return _materializer.FirstAsync(BuildCommand(), true, true, ct);
  }
  /// <summary>Asynchronously returns the single element that matches the predicate, or default(T) if none. Throws if more than one match.</summary>
  public ValueTask<T?> SingleOrDefaultAsync([InterpolatedStringHandlerArgument("")] ref SqlBuilderHandler<T> predicate, CancellationToken ct = default)
    => Where(ref predicate).SingleOrDefaultAsync(ct);

  /// <summary>Asynchronously returns the single element of the query result. Throws if zero or more than one element.</summary>
  public ValueTask<T> SingleAsync(CancellationToken ct = default)
  {
    Take(2);
    return _materializer.FirstAsync(BuildCommand(), false, true, ct)!;
  }
  /// <summary>Asynchronously returns the single element that matches the predicate. Throws if zero or more than one match.</summary>
  public ValueTask<T> SingleAsync([InterpolatedStringHandlerArgument("")] ref SqlBuilderHandler<T> predicate, CancellationToken ct = default)
    => Where(ref predicate).SingleAsync(ct);

  /// <summary>Materializes the query result into a new <see cref="List{T}"/>.</summary>
  public List<T> ToList()
    => _materializer.ToList(BuildCommand());
  /// <summary>Applies the predicate and materializes the result into a new <see cref="List{T}"/>.</summary>
  public List<T> ToList([InterpolatedStringHandlerArgument("")] ref SqlBuilderHandler<T> predicate)
    => Where(ref predicate).ToList();

  /// <summary>Asynchronously materializes the query result into a new <see cref="List{T}"/>.</summary>
  public ValueTask<List<T>> ToListAsync(CancellationToken ct = default)
    => _materializer.ToListAsync(BuildCommand(), ct);
  /// <summary>Asynchronously applies the predicate and materializes the result into a new <see cref="List{T}"/>.</summary>
  public ValueTask<List<T>> ToListAsync([InterpolatedStringHandlerArgument("")] ref SqlBuilderHandler<T> predicate, CancellationToken ct = default)
    => Where(ref predicate).ToListAsync(ct);

  /// <summary>Returns the query result as an <see cref="IEnumerable{T}"/> (materialized as a list).</summary>
  public IEnumerable<T> Enum() => _materializer.ToList(BuildCommand());
  /// <summary>Applies the predicate and returns the result as an <see cref="IEnumerable{T}"/>.</summary>
  public IEnumerable<T> Enum([InterpolatedStringHandlerArgument("")] ref SqlBuilderHandler<T> predicate) => Where(ref predicate).Enum();

  /// <summary>Builds a DELETE statement restricted by the given predicate.</summary>
  public SqlBuilder<T> Delete([InterpolatedStringHandlerArgument("")] ref SqlBuilderHandler<T> predicate) => Delete().Where(ref predicate);
  /// <summary>Builds an UPDATE statement with the given setters expression.</summary>
  public SqlBuilder<T> Update([InterpolatedStringHandlerArgument("")] ref SqlBuilderHandler<T> setters) => Update_(setters.Text);
  /// <summary>Builds an INSERT statement with the specified columns and values expression.</summary>
  public SqlBuilder<T> Insert(string columns, [InterpolatedStringHandlerArgument("")] ref SqlBuilderHandler<T> values) => Insert_(columns, values.Text);
  /// <summary>Reloads data for the specified columns using the given filter. For internal use.</summary>
  [EditorBrowsable(EditorBrowsableState.Never)]
  public SqlBuilder<T> Reload(string columns, [InterpolatedStringHandlerArgument("")] ref SqlBuilderHandler<T> filter) => Reload_(columns, filter.Text);
  /// <summary>Runs a check (e.g. existence or constraint). For internal use.</summary>
  [EditorBrowsable(EditorBrowsableState.Never)]
  public SqlBuilder<T> Check() => Check_();

  /// <summary>Specifies the SELECT projection using the given expression.</summary>
  public SqlBuilder<T> Select([InterpolatedStringHandlerArgument("")] ref SqlBuilderHandler<T> predicate) => Select_(predicate.Text);
  /// <summary>Adds a WHERE clause with the given predicate expression.</summary>
  public SqlBuilder<T> Where([InterpolatedStringHandlerArgument("")] ref SqlBuilderHandler<T> predicate) => Where_(predicate.Text);
  /// <summary>Adds a WHERE clause with the given predicate expression.</summary>
  public SqlBuilder<T> Join([InterpolatedStringHandlerArgument("")] ref SqlBuilderHandler<T> join) => Where_(join.Text);
  /// <summary>Adds an ORDER BY clause with the given expression.</summary>
  public SqlBuilder<T> OrderBy([InterpolatedStringHandlerArgument("")] ref SqlBuilderHandler<T> predicate) => OrderBy_(predicate.Text);
  /// <summary>Adds a GROUP BY clause with the given expression.</summary>
  public SqlBuilder<T> GroupBy([InterpolatedStringHandlerArgument("")] ref SqlBuilderHandler<T> predicate) => GroupBy_(predicate.Text);
  /// <summary>Appends raw SQL to the current builder.</summary>
  public SqlBuilder<T> Raw([InterpolatedStringHandlerArgument("")] ref SqlBuilderHandler<T> statement) => Raw_(statement.Text);

  /// <summary>Executes the built command and returns the number of rows affected.</summary>
  public int Run() => BuildCommand().Run();
  /// <summary>Asynchronously executes the built command and returns the number of rows affected.</summary>
  public ValueTask<int> RunAsync(CancellationToken ct = default) => BuildCommand().RunAsync(ct);
  /// <summary>Executes the built command and returns the scalar result of type <typeparamref name="R"/>.</summary>
  public R? Run<R>() => BuildCommand().Run<R>();
  /// <summary>Asynchronously executes the built command and returns the scalar result of type <typeparamref name="R"/>.</summary>
  public ValueTask<R?> RunAsync<R>(CancellationToken ct = default) => BuildCommand().RunAsync<R>(ct);
}

