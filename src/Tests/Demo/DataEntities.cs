using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entities;


public partial class DataEntities(DbContextOptions<DataEntities> options, ILogger<DataEntities> logger) : DbContext(options)
{
  public readonly ILogger Log = logger;

  partial void OnAfterMapping(ModelBuilder builder)
  {
    builder.HasDefaultSchema("dbo");

    foreach (var e in builder.Model.GetEntityTypes())
      e.UseSqlOutputClause(false);

    foreach (var e in builder.Model.GetEntityTypes())
      foreach (var p in e.GetProperties().Where(i => i.Name == "Ts"))
        p.SetColumnType("datetime2");
  }
}
