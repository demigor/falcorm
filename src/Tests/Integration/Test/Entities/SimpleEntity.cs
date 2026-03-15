namespace Falcorm.IntegrationTests.Entities;

[Table("Simple")]
public class SimpleEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [ConcurrencyCheck]
    public DateTime Ts { get; set; }

    /// <summary>Single character (string used because SqlClient.GetChar is not supported).</summary>
    public char ColChar { get; set; } = ' ';

    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Tags { get; set; }
}
