using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Falcorm.IntegrationTests.Entities;

/// <summary>
/// Entity with 20 columns covering all Falcorm-supported types.
/// </summary>
[Table("AllTypes")]
public class AllTypesEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string ColString { get; set; } = "";
    public string? ColStringNull { get; set; }
    public bool ColBool { get; set; }
    public bool? ColBoolNull { get; set; }
    public int ColInt { get; set; }
    public int? ColIntNull { get; set; }
    public byte[]? ColBytes { get; set; }
    public byte ColByte { get; set; }
    public long ColLong { get; set; }
    public short ColShort { get; set; }
    public decimal ColDecimal { get; set; }
    public double ColDouble { get; set; }
    public float ColFloat { get; set; }
    public DateTime ColDateTime { get; set; }
    public DateTime? ColDateTimeNull { get; set; }
    public DateTimeOffset ColDateTimeOffset { get; set; }
    public Guid ColGuid { get; set; }
}
