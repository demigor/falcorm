using System.Data.Common;

namespace Istarion.Falcorm.Internals;

internal static class DbInternals
{
  extension(DbSession session)
  {
    public IDbTable<T> Scalars<T>() => new DbTable<T, DbScalarMapper<T>>(session);


    public SqlBuilder<T> Builder<T>() => session.Scalars<T>().Builder();
  }

  extension(DbCommand cmd)
  {
    public int Run()
    {
      using var c = cmd;
      return c.ExecuteNonQuery();
    }

    public async ValueTask<int> RunAsync(CancellationToken ct = default)
    {
      await using var c = cmd;
      return await c.ExecuteNonQueryAsync(ct);
    }

    public T? Run<T>()
    {
      using var c = cmd;
      return ToType<T>(c.ExecuteScalar());
    }

    public async ValueTask<T?> RunAsync<T>(CancellationToken ct = default)
    {
      await using var c = cmd;
      return ToType<T>(await c.ExecuteScalarAsync(ct));
    }
  }

  static T? ToType<T>(object? result)
    => result == DBNull.Value || result == null ? default : (T)Convert.ChangeType(result, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
}


internal struct DbScalarMapper<T> : IDbMapper<T>
{
  public static string TableName => "";

  public static (int update, int keys, int generated) ColumnCounts => (1, 0, 0);

  public static void CreateMap(DbDataReader reader, Span<byte> map)
  {
    //map.Fill(0xFF);
    //map[0] = 0;
  }

  public static T Read(DbDataReader reader, Span<byte> map, T? entity)
    => reader.IsDBNull(0) ? entity ?? default! : reader.GetFieldValue<T>(0);
}