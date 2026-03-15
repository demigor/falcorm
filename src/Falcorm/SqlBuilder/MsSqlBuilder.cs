using System.Data.Common;
using System.Text;

namespace Istarion.Falcorm.Internals;

internal class MssqlBuilder<T>(IDbTable<T> source, DbCommand cmd, SqlBuilder<T>? inner = null, SqlBuilder[]? unions = null, bool unionAll = false) : SqlBuilder<T>(source, cmd, inner, unions, unionAll)
{
  internal override SqlBuilder<T> Wrap() => new MssqlBuilder<T>(_materializer, _cmd, this);

  public override SqlBuilder<T> Union(IEnumerable<SqlBuilder> unions, bool unionAll) => new MssqlBuilder<T>(_materializer, _cmd, null, [.. unions], unionAll) { _alias = "t" };

  public override SqlBuilder<T> New() => new MssqlBuilder<T>(_materializer, _cmd);

  public override int MaxParamCount => 2100;
  internal override void AppendDbo(in Dbo dbo, StringBuilder? sb)
  {
    sb ??= _sb;

    switch (dbo.Type)
    {
      case Dbo.IdType.CN:
        {
          if (!string.IsNullOrEmpty(dbo.Schema))
          {
            sb.Append(dbo.Schema);
            sb.Append('.');
          }

          sb.Append('[');
          sb.Append(dbo.Name);
          sb.Append(']');
        }
        break;

      case Dbo.IdType.TN:
      case Dbo.IdType.FN:
        {
          sb.Append(string.IsNullOrEmpty(dbo.Schema) ? "dbo" : dbo.Schema);
          sb.Append(".[");
          sb.Append(dbo.Name);
          sb.Append(']');
        }
        break;

      case Dbo.IdType.Mssql:
        sb.Append(dbo.Name);
        break;
    }

  }

  public override string? Format(in Dbo dbo) => dbo.Type switch
  {
    Dbo.IdType.CN => string.IsNullOrEmpty(dbo.Schema) ? $"[{dbo.Name}]" : $"{dbo.Schema}.[{dbo.Name}]",
    Dbo.IdType.TN or Dbo.IdType.FN => string.IsNullOrEmpty(dbo.Schema) ? $"dbo.[{dbo.Name}]" : $"{dbo.Schema}.[{dbo.Name}]",
    Dbo.IdType.Mssql => dbo.Name,
    _ => null,
  };

  internal override void AppendTop()
  {
    if (_skip == null && _take != null)
    {
      _sb.Append("top(");
      AddParam(_sb, _take.Value);
      _sb.Append(") ");
    }
  }

  internal override void AddParam(StringBuilder sb, IEnumerable<string?> param)
  {
    sb.Append("(select value from string_split(");
    AddParam(sb, string.Join((char)1, param));
    sb.Append(", char(1)))");
  }

  void AppendReturn()
  {
    //; select @@rowcount as _Updated_Count, { selectSb}
    //from @table where { whereKeys}
    //;\  
    var hasReloads = false;

    foreach (var (columns, _) in GetAppends(ClauseKind.Reloads))
    {
      if (!hasReloads)
        _sb.Append("; select @@rowcount as _Updated_Count");

      if (!string.IsNullOrEmpty(columns))
      {
        _sb.Append(", ");
        _sb.Append(columns);
        hasReloads = true;
      }
    }
    if (hasReloads)
    {
      _sb.Append(" from ");
      AppendDbo(Dbo.TN(_tableName ?? _materializer.TableName), _sb);
      _sb.Append(" where ");

      hasReloads = false;

      foreach (var (_, wheres) in GetAppends(ClauseKind.Reloads))
      {
        if (hasReloads)
          _sb.Append(" and ");

        _sb.Append(wheres);

        hasReloads = true;
      }
    }
  }

  internal override void BuildInsert()
  {
    base.BuildInsert();
    AppendReturn();
  }

  internal override void BuildUpdate()
  {
    base.BuildUpdate();
    AppendReturn();
  }

  internal override void BuildDelete()
  {
    base.BuildDelete();
    AppendReturn();
  }
}
