using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Text;

namespace Istarion.Falcorm;

using Internals;

/// <summary>Base class for building SQL command text and parameters. Subclasses implement provider-specific SQL generation.</summary>
public abstract class SqlBuilder(DbCommand cmd)
{
  internal enum ClauseKind { Select, Join, Update, Where, OrderBy, GroupBy, Raw, Inserts, Reloads }

  internal readonly DbCommand _cmd = cmd;
  internal StringBuilder _sb = null!;

  internal abstract void BuildSql(StringBuilder sb);

  /// <summary>
  /// Formats supplied Dbo in a Dialect manner
  /// </summary>
  public abstract string? Format(in Dbo dbo);
  internal abstract void AppendDbo(in Dbo dbo, StringBuilder? sb = null);

  internal DbParameter AddParam(object? value)
  {
    var result = _cmd.CreateParameter();
    result.ParameterName = $"@p{_cmd.Parameters.Count}";
    result.Value = value ?? DBNull.Value;
    _cmd.Parameters.Add(result);
    return result;
  }

  internal void AddParam(StringBuilder sb, DateTime? param)
  {
    var p = AddParam(param);
    p.DbType = DbType.DateTime2;
    sb.Append(p.ParameterName);
  }

  internal void AddParam(StringBuilder sb, byte[]? param)
  {
    var p = AddParam(param);
    p.DbType = DbType.Binary;
    sb.Append(p.ParameterName);
  }

  internal virtual void AddParam(StringBuilder sb, IEnumerable<string?> param)
  {
    AddParam(sb, (object?)param);
  }

  internal void AddParam(StringBuilder sb, object? param)
  {
    sb.Append(AddParam(param).ParameterName);
  }

  /// <summary>Builds and returns the SQL text for the current command.</summary>
  /// <returns>The generated SQL string.</returns>
  public string Sql()
  {
    var sb = new StringBuilder();
    BuildSql(sb);
    return sb.ToString();
  }

  /// <summary>Builds and returns the SQL text for the current command.</summary>
  /// <returns>The generated SQL string.</returns>
  internal string Sql(Action<StringBuilder> prefix, Action<StringBuilder> suffix)
  {
    var sb = new StringBuilder();
    prefix(sb);
    BuildSql(sb);
    suffix(sb);
    return sb.ToString();
  }

  /// <summary>Gets the maximum number of parameters supported by this builder (provider-specific).</summary>
  public abstract int MaxParamCount { get; }
}

/// <summary>Generic SQL builder for a typed entity: supports SELECT, INSERT, UPDATE, DELETE, raw SQL, unions, paging (Take/Skip), and chained statements (Then).</summary>
/// <typeparam name="T">The entity type for the table.</typeparam>
public abstract partial class SqlBuilder<T>(IDbTable<T> source, DbCommand cmd, SqlBuilder<T>? inner, SqlBuilder[]? unions = null, bool unionAll = false) : SqlBuilder(cmd)
{
  internal readonly IDbMaterializer<T> _materializer = (IDbMaterializer<T>)source;
  readonly SqlBuilder? _inner = inner;
  readonly SqlBuilder[]? _unions = unions;
  readonly bool _unionAll = unionAll;
  internal List<(ClauseKind kind, string clause, string? values)>? _clauses;
  internal int? _take, _skip;
  internal string? _tableName;
  internal string? _alias;
  internal SqlBuilder? _prev;


  internal abstract SqlBuilder<T> Wrap();
  /// <summary>Combines this query with the given builders using UNION or UNION ALL.</summary>
  /// <param name="unions">The query builders to union with.</param>
  /// <param name="unionAll">If true, use UNION ALL; otherwise use UNION.</param>
  /// <returns>This builder for chaining.</returns>
  public abstract SqlBuilder<T> Union(IEnumerable<SqlBuilder> unions, bool unionAll = false);
  /// <summary>Combines this query with the given builders using UNION ALL.</summary>
  /// <param name="unions">The query builders to union with.</param>
  /// <returns>This builder for chaining.</returns>
  public SqlBuilder<T> UnionAll(IEnumerable<SqlBuilder> unions) => Union(unions, true);
  /// <summary>Creates a new empty builder of the same type bound to the same source and command.</summary>
  /// <returns>A new <see cref="SqlBuilder{T}"/> instance.</returns>
  public abstract SqlBuilder<T> New();

  internal void AddClause(ClauseKind kind, string clause, string? values = null) => (_clauses ??= []).Add((kind, clause, values));

  internal IEnumerable<string> GetClauses(ClauseKind kind) => _clauses?.Where(i => i.kind == kind).Select(i => i.clause) ?? [];
  internal IEnumerable<(string columns, string values)> GetAppends(ClauseKind kind) => _clauses?.Where(i => i.kind == kind).Select(i => (i.clause, i.values!)) ?? [];

  internal SqlBuilder<T> Select_(string columns)
  {
    Mode(StatementKind.Select);

    if (IsFinalized)
      return Wrap().Select_(columns);

    AddClause(ClauseKind.Select, columns);
    return this;
  }

  internal SqlBuilder<T> Join_(string join)
  {
    if (IsFinalized)
      return Wrap().Join_(join);

    AddClause(ClauseKind.Join, join);
    return this;
  }

  internal SqlBuilder<T> Where_(string condition)
  {
    if (IsFinalized)
      return Wrap().Where_(condition);

    AddClause(ClauseKind.Where, condition);
    return this;
  }

  internal SqlBuilder<T> OrderBy_(string orderBy)
  {
    Mode(StatementKind.Select);

    if (IsFinalized)
      return Wrap().OrderBy_(orderBy);

    AddClause(ClauseKind.OrderBy, orderBy);
    return this;
  }

  internal SqlBuilder<T> GroupBy_(string groupBy)
  {
    Mode(StatementKind.Select);

    if (IsFinalized)
      return Wrap().GroupBy_(groupBy);

    AddClause(ClauseKind.GroupBy, groupBy);
    return this;
  }

  internal SqlBuilder<T> Update_(string update)
  {
    Mode(StatementKind.Update);

    AddClause(ClauseKind.Update, update);
    return this;
  }

  internal SqlBuilder<T> Raw_(string statement)
  {
    Mode(StatementKind.Raw);

    AddClause(ClauseKind.Raw, statement);
    return this;
  }

  internal SqlBuilder<T> Insert_(string columns, string values)
  {
    Mode(StatementKind.Insert);

    AddClause(ClauseKind.Inserts, columns, values);
    return this;
  }

  internal SqlBuilder<T> Reload_(string columns, string where)
  {
    if (_kind != StatementKind.Insert && _kind != StatementKind.Update)
      throw new InvalidOperationException("Reload requires Insert or Update statements");

    AddClause(ClauseKind.Reloads, columns, where);
    return this;
  }

  internal SqlBuilder<T> Check_()
  {
    if (_kind != StatementKind.Insert && _kind != StatementKind.Update && _kind != StatementKind.Delete)
      throw new InvalidOperationException("Check requires DML statement");

    AddClause(ClauseKind.Reloads, "");
    return this;
  }

  /// <summary>Sets the target table name for the statement. Cannot be changed after the builder is materialized.</summary>
  /// <param name="tableName">The table name (optionally schema-qualified).</param>
  /// <returns>This builder for chaining.</returns>
  /// <exception cref="InvalidOperationException">Thrown when the table name is already materialized.</exception>
  public SqlBuilder<T> Table(string tableName)
  {
    if (_inner != null)
      throw new InvalidOperationException("The table name is already materialized and cannot be altered");

    _tableName = tableName;
    return this;
  }

  bool IsFinalized => _take != null || _skip != null;

  internal override void BuildSql(StringBuilder sb)
  {
    _sb = sb;

    if (_prev != null)
    {
      _prev.BuildSql(_sb);
      _sb.Append(";\n\n");
    }

    switch (_kind ?? StatementKind.Select)
    {
      case StatementKind.Select: BuildSelect(); break;
      case StatementKind.Insert: BuildInsert(); break;
      case StatementKind.Update: BuildUpdate(); break;
      case StatementKind.Delete: BuildDelete(); break;
      case StatementKind.Raw: BuildRaw(); break;
    }

    _sb = null!;
  }

  internal virtual void BuildRaw()
  {
    foreach (var setter in GetClauses(ClauseKind.Raw))
      _sb.Append(setter);
  }

  internal virtual void BuildInsert()
  {
    _sb.Append("insert into ");
    AppendDbo(Dbo.TN(_tableName ?? _materializer.TableName), _sb);
    _sb.Append(" (");
    var separator = false;
    foreach (var (columns, _) in GetAppends(ClauseKind.Inserts))
    {
      if (separator) _sb.Append(", ");
      _sb.Append(columns);
      separator = true;
    }
    _sb.Append(") values (");
    separator = false;
    foreach (var (_, values) in GetAppends(ClauseKind.Inserts))
    {
      if (separator) _sb.Append(", ");
      _sb.Append(values);
      separator = true;
    }
    _sb.Append(')');
  }

  internal virtual void BuildDelete()
  {
    _sb.Append("delete");
    AppendFrom();
    AppendWhere();
  }

  internal virtual void BuildUpdate()
  {
    _sb.Append("update ");

    _alias ??= "t";

    _sb.Append(_alias);

    _sb.Append(" set ");

    var separator = false;
    foreach (var setter in GetClauses(ClauseKind.Update))
    {
      if (separator) _sb.Append(", ");
      _sb.Append(setter);
      separator = true;
    }

    AppendFrom();

    AppendWhere();
  }

  internal virtual void BuildSelect()
  {
    _sb.Append("select ");

    AppendTop();

    AppendColumns();

    AppendFrom();

    AppendJoins();

    AppendWhere();

    AppendOrderBy();

    AppendGroupBy();

    AddLimit();
  }

  void AddLimit()
  {
    if (AppendLimit()) return;

    if (_skip != null)
    {
      if (!(GetClauses(ClauseKind.OrderBy).Any()))
        _sb.Append(" order by (select 1)");

      _sb.Append(" offset ");
      AddParam(_sb, _skip.Value);
      _sb.Append(" rows");

      if (_take != null)
      {
        _sb.Append(" fetch next ");
        AddParam(_sb, _take.Value);
        _sb.Append(" rows only");
      }
    }
  }

  internal virtual void AppendTop() { }

  internal virtual bool AppendLimit() => false;

  internal void AppendColumns()
  {
    var hasColumns = false;

    foreach (var column in GetClauses(ClauseKind.Select))
    {
      if (hasColumns) _sb.Append(", ");
      _sb.Append(column);
      hasColumns = true;
    }

    if (!hasColumns)
    {
      if (_alias != null)
      {
        _sb.Append(_alias);
        _sb.Append('.');
      }
      _sb.Append('*');
    }
  }

  internal void AppendFrom()
  {
    if (_tableName == "")
      return;

    _sb.Append(" from ");

    if (_inner != null)
    {
      _sb.Append('(');
      _inner.BuildSql(_sb);
      _sb.Append(')');
    }
    else if (_unions != null)
    {
      _sb.Append('(');
      var separator = false;
      foreach (var union in _unions)
      {
        if (separator)
        {
          _sb.Append(" union ");

          if (_unionAll)
            _sb.Append("all ");
        }

        union.BuildSql(_sb);
        separator = true;
      }
      _sb.Append(')');
    }
    else
      AppendDbo(Dbo.TN(_tableName ?? _materializer.TableName), _sb);

    if (_alias != null)
    {
      _sb.Append(' ');
      _sb.Append(_alias);
    }
  }

  internal void AppendJoins()
  {
    var hasJoins = false;
    foreach (var join in GetClauses(ClauseKind.Join))
    {
      if (hasJoins) _sb.Append(" ");
      _sb.Append(join);
      hasJoins = true;
    }
  }

  internal void AppendWhere()
  {
    var hasWheres = false;
    foreach (var predicate in GetClauses(ClauseKind.Where))
    {
      if (hasWheres) _sb.Append(" and ");
      else _sb.Append(" where ");
      _sb.Append(predicate);
      hasWheres = true;
    }
  }

  internal void AppendOrderBy()
  {
    var hasOrderBys = false;
    foreach (var predicate in GetClauses(ClauseKind.OrderBy))
    {
      if (hasOrderBys) _sb.Append(", ");
      else _sb.Append(" order by ");
      _sb.Append(predicate);
      hasOrderBys = true;
    }
  }

  internal void AppendGroupBy()
  {
    var hasGroupBys = false;
    foreach (var predicate in GetClauses(ClauseKind.GroupBy))
    {
      if (hasGroupBys) _sb.Append(", ");
      else _sb.Append(" group by ");
      _sb.Append(predicate);
      hasGroupBys = true;
    }
  }

  /// <summary>Sets the alias for the table or subquery in the FROM clause.</summary>
  /// <param name="alias">The alias name.</param>
  /// <returns>This builder for chaining.</returns>
  public SqlBuilder<T> Alias(string alias)
  {
    _alias = alias;
    return this;
  }

  /// <summary>Limits the number of rows returned (TOP / FETCH NEXT). Multiple calls use the minimum count.</summary>
  /// <param name="count">Maximum number of rows to return.</param>
  /// <returns>This builder for chaining.</returns>
  public SqlBuilder<T> Take(int count)
  {
    if (_take == null)
      _take = count;
    else
      _take = Math.Min(_take.Value, count);

    return Mode(StatementKind.Select);
  }

  /// <summary>Skips the specified number of rows (OFFSET). If Take is already set, wraps the current query and applies Skip on the outer query.</summary>
  /// <param name="count">Number of rows to skip.</param>
  /// <returns>This builder for chaining.</returns>
  public SqlBuilder<T> Skip(int count)
  {
    if (_take != null)
      return Wrap().Skip(count).Mode(StatementKind.Select);

    _skip = count + (_skip ?? 0);
    return Mode(StatementKind.Select);
  }

  /// <summary>Kind of SQL statement the builder is configured for.</summary>
  public enum StatementKind
  {
    /// <summary>SELECT query.</summary>
    Select,
    /// <summary>INSERT statement.</summary>
    Insert,
    /// <summary>UPDATE statement.</summary>
    Update,
    /// <summary>DELETE statement.</summary>
    Delete,
    /// <summary>Raw SQL statement(s).</summary>
    Raw
  }


  StatementKind? _kind = null;
  /// <summary>Builds the SQL text, assigns it to the command, and ensures the session is open. Returns the configured <see cref="DbCommand"/>.</summary>
  /// <returns>The <see cref="DbCommand"/> with <see cref="DbCommand.CommandText"/> set to the generated SQL.</returns>
  public DbCommand BuildCommand()
  {
    _cmd.CommandText = Sql();
    _materializer.Session.EnsureOpen();
    return _cmd;
  }

  internal DbCommand BuildCommand(Action<StringBuilder> prefix, Action<StringBuilder> suffix)
  {
    _cmd.CommandText = Sql(prefix, suffix);
    _materializer.Session.EnsureOpen();
    return _cmd;
  }

  SqlBuilder<T> Mode(StatementKind kind)
  {
    if (_kind != kind)
    {

      if (_kind != null)
        throw new InvalidOperationException($"SqlBuilder has been set for {_kind}");

      _kind = kind;
    }
    return this;
  }

  /// <summary>Gets the current statement kind, or null if not yet set.</summary>
  public StatementKind? Kind => _kind;
  /// <summary>Gets whether this builder is configured for an UPDATE statement.</summary>
  public bool HasUpdates => _kind == StatementKind.Update;
  /// <summary>Gets whether this builder is configured for an INSERT statement.</summary>
  public bool HasInserts => _kind == StatementKind.Insert;

  /// <summary>Configures the builder for a DELETE statement.</summary>
  /// <returns>This builder for chaining.</returns>
  public SqlBuilder<T> Delete() => Mode(StatementKind.Delete);
  /// <summary>Configures the builder for an UPDATE statement.</summary>
  /// <returns>This builder for chaining.</returns>
  public SqlBuilder<T> Update() => Mode(StatementKind.Update);

  /// <summary>Creates a new builder that runs after this one; both statements are emitted in a single batch (separated by semicolon).</summary>
  /// <returns>A new builder chained after this one.</returns>
  public SqlBuilder<T> Then()
  {
    var result = New();
    result._prev = this;
    return result;
  }
}


/// <summary>Interpolated string handler for building SQL with parameterized values via <see cref="SqlBuilder{T}"/>.</summary>
[InterpolatedStringHandler]
public readonly ref struct SqlBuilderHandler<T>
{
  /// <summary>The builder that receives the generated SQL and parameters.</summary>
  public readonly SqlBuilder<T> Builder;
  readonly StringBuilder _sb;

  /// <summary>Initializes the handler with the given builder and capacity hints.</summary>
  /// <param name="literalLength">Estimated length of literal segments.</param>
  /// <param name="formattedCount">Number of interpolated segments.</param>
  /// <param name="builder">The SQL builder to use.</param>
  /// <param name="isValid">Set to true when the handler is successfully constructed.</param>
  public SqlBuilderHandler(int literalLength, int formattedCount, SqlBuilder<T> builder, out bool isValid)
  {
    Builder = builder;
    _sb = new StringBuilder(literalLength);
    isValid = true;
  }

  /// <summary>Initializes the handler from a table source.</summary>
  public SqlBuilderHandler(int literalLength, int formattedCount, IDbTable<T> source, out bool isValid)
    : this(literalLength, formattedCount, source.Builder(), out isValid)
  {
  }
  /// <summary>Initializes the handler from a session.</summary>
  public SqlBuilderHandler(int literalLength, int formattedCount, DbSession session, out bool isValid)
    : this(literalLength, formattedCount, session.Builder<T>(), out isValid)
  {
  }

  /// <summary>Appends a literal string segment to the SQL being built.</summary>
  /// <param name="s">The literal text.</param>
  public readonly void AppendLiteral(string s) => _sb.Append(s);

  /// <summary>Appends a schema-qualified identifier (table/column name) to the SQL.</summary>
  /// <param name="value">The <see cref="Dbo"/> identifier.</param>
  public readonly void AppendFormatted(in Dbo value)
  {
    Builder.AppendDbo(in value, _sb);
  }

  /// <summary>Appends a parameterized DateTime value (DbType.DateTime2).</summary>
  /// <param name="value">The date/time value.</param>
  public readonly void AppendFormatted(DateTime value)
  {
    Builder.AddParam(_sb, value);
  }

  /// <summary>Appends a parameterized binary value.</summary>
  /// <param name="value">The byte array.</param>
  public readonly void AppendFormatted(byte[] value)
  {
    Builder.AddParam(_sb, value);
  }

  /// <summary>Appends a parameterized list of strings (e.g. for IN clauses).</summary>
  /// <param name="value">The collection of string values.</param>
  public readonly void AppendFormatted(IEnumerable<string?> value)
  {
    Builder.AddParam(_sb, value);
  }

  /// <summary>Appends a parameterized value of any type.</summary>
  /// <param name="value">The value to add as a parameter.</param>
  public readonly void AppendFormatted(object? value)
  {
    Builder.AddParam(_sb, value);
  }

  /// <summary>Gets the SQL text built so far from literal and formatted segments.</summary>
  public readonly string Text => _sb.ToString();
}
