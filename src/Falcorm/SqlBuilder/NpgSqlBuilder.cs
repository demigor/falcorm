using System.Data.Common;
using System.Text;

namespace Istarion.Falcorm.Internals;

internal class NpgsqlBuilder<T>(IDbTable<T> source, DbCommand cmd, SqlBuilder<T>? inner = null, SqlBuilder[]? unions = null, bool unionAll = false) : SqlBuilder<T>(source, cmd, inner, unions, unionAll)
{
  internal override SqlBuilder<T> Wrap() => new NpgsqlBuilder<T>(_materializer, _cmd, this);
  public override SqlBuilder<T> Union(IEnumerable<SqlBuilder> unions, bool unionAll) => new NpgsqlBuilder<T>(_materializer, _cmd, null, [.. unions], unionAll) { _alias = "t" };
  public override SqlBuilder<T> New() => new NpgsqlBuilder<T>(_materializer, _cmd);

  public override int MaxParamCount => ushort.MaxValue;

  internal override void AddParam(StringBuilder sb, IEnumerable<string?> param)
  {
    sb.Append("unnest(");
    AddParam(sb, [.. param]);
    sb.Append(')');
  }

  internal override void AppendDbo(in Dbo dbo, StringBuilder? sb = null)
  {
    if (dbo.Type == Dbo.IdType.Mssql)
      return;

    sb ??= _sb;
    if (!string.IsNullOrEmpty(dbo.Schema))
    {
      sb.Append(dbo.Schema);
      sb.Append('.');
    }
    sb.Append(dbo.Name);
  }


  internal override bool AppendLimit()
  {
    if (_skip == null && _take != null)
    {
      _sb.Append(" limit ");
      AddParam(_sb, _take.Value);
      return true;
    }
    return false;
  }

  void AppendReturn()
  {
    var hasReloads = false;

    foreach (var (columns, _) in GetAppends(ClauseKind.Reloads))
    {
      if (!hasReloads)
        _sb.Append(" returning 1 as _Updated_Count");

      if (!string.IsNullOrEmpty(columns))
      {
        _sb.Append(", ");
        _sb.Append(columns);
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