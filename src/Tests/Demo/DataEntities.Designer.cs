//------------------------------------------------------------------------------
// This is auto-generated code.
//------------------------------------------------------------------------------
#nullable enable
#pragma warning disable CS8618
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace Entities;

public partial class DataEntities : DbContext
{
  partial void OnBeforeMapping(ModelBuilder builder);
  partial void OnAfterMapping(ModelBuilder builder);
  protected override void OnModelCreating(ModelBuilder builder)
  {
    OnBeforeMapping(builder);

    builder.Entity<Datasheet>(e => 
    {
      e.HasKey(p => p.Id);

      e.Property(p => p.Xid).IsRequired().HasMaxLength(22).IsUnicode(false);

      e.Property(p => p.ValidFrom).IsRequired();

      e.Property(p => p.CreateTime).IsRequired().ValueGeneratedOnAdd();

      e.Property(p => p.DeleteTime);

      e.Property(p => p.Status).IsRequired();

      e.Property(p => p.Ts).IsRequired().ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

      e.Property(p => p.Id).IsRequired().HasMaxLength(22).IsUnicode(false);

      e.Property(p => p.Name).IsRequired().IsUnicode(true);

      e.Property(p => p.Description).IsUnicode(true);

      e.Property(p => p.ProductClassId).HasMaxLength(22).IsUnicode(false);

      e.Property(p => p.ViewDefinition).IsUnicode(true);

      e.Property(p => p.ValidatorDefinition).IsUnicode(false);

      e.Property(p => p.Flags).IsUnicode(true);

    });

    builder.Entity<Currency>(e => 
    {
      e.HasKey(p => p.Id);

      e.Property(p => p.Id).IsRequired().HasMaxLength(22).IsUnicode(false);

      e.Property(p => p.Xid).IsRequired().HasMaxLength(22).IsUnicode(false);

      e.Property(p => p.ValidFrom).IsRequired();

      e.Property(p => p.CreateTime).IsRequired().ValueGeneratedOnAdd();

      e.Property(p => p.DeleteTime);

      e.Property(p => p.Status).IsRequired();

      e.Property(p => p.Ts).IsRequired().ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

      e.Property(p => p.Name).IsRequired().IsUnicode(true);

      e.Property(p => p.NameDe).IsUnicode(true);

      e.Property(p => p.NameFr).IsUnicode(true);

      e.Property(p => p.NameIt).IsUnicode(true);

      e.Property(p => p.Description).IsUnicode(true);

      e.Property(p => p.DescriptionDe).IsUnicode(true);

      e.Property(p => p.DescriptionFr).IsUnicode(true);

      e.Property(p => p.DescriptionIt).IsUnicode(true);

      e.Property(p => p.Flags).IsUnicode(true);

      e.Property(p => p.Ordering);

      e.Property(p => p.Type).IsRequired();

      e.Property(p => p.ExtId).IsUnicode(true);

      e.Property(p => p.RefParents).IsUnicode(false);

      e.Property(p => p.RefChildren).IsUnicode(false);

    });

    builder.Entity<Company>(e => 
    {
      e.HasKey(p => p.Id);

      e.Property(p => p.Xid).IsRequired().HasMaxLength(22).IsUnicode(false);

      e.Property(p => p.ValidFrom).IsRequired();

      e.Property(p => p.CreateTime).IsRequired().ValueGeneratedOnAdd();

      e.Property(p => p.DeleteTime);

      e.Property(p => p.Status).IsRequired();

      e.Property(p => p.Ts).IsRequired().ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

      e.Property(p => p.Id).IsRequired().HasMaxLength(22).IsUnicode(false);

      e.Property(p => p.Name).IsRequired().IsUnicode(true);

      e.Property(p => p.Description).IsUnicode(true);

      e.Property(p => p.DescriptionDe).IsUnicode(true);

      e.Property(p => p.DescriptionFr).IsUnicode(true);

      e.Property(p => p.DescriptionIt).IsUnicode(true);

      e.Property(p => p.Flags).IsUnicode(true);

      e.Property(p => p.LEI).IsUnicode(true);

      e.Property(p => p.BIC).IsUnicode(true);

      e.Property(p => p.BBG).IsUnicode(true);

      e.Property(p => p.Type).IsRequired();

      e.Property(p => p.ExtId).IsUnicode(true);

      e.Property(p => p.Tags).IsUnicode(true);

      e.Property(p => p.RefParents).IsUnicode(false);

      e.Property(p => p.RefChildren).IsUnicode(false);

    });

    builder.Entity<AssetClass>(e => 
    {
      e.HasKey(p => p.Id);

      e.Property(p => p.Id).IsRequired().HasMaxLength(22).IsUnicode(false);

      e.Property(p => p.Xid).IsRequired().HasMaxLength(22).IsUnicode(false);

      e.Property(p => p.ValidFrom).IsRequired();

      e.Property(p => p.CreateTime).IsRequired().ValueGeneratedOnAdd();

      e.Property(p => p.DeleteTime);

      e.Property(p => p.Status).IsRequired();

      e.Property(p => p.Ts).IsRequired().ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

      e.Property(p => p.Name).IsRequired().IsUnicode(true);

      e.Property(p => p.NameDe).IsUnicode(true);

      e.Property(p => p.NameFr).IsUnicode(true);

      e.Property(p => p.NameIt).IsUnicode(true);

      e.Property(p => p.Description).IsUnicode(true);

      e.Property(p => p.DescriptionDe).IsUnicode(true);

      e.Property(p => p.DescriptionFr).IsUnicode(true);

      e.Property(p => p.DescriptionIt).IsUnicode(true);

      e.Property(p => p.Ordering);

      e.Property(p => p.Flags).IsUnicode(true);

      e.Property(p => p.ExtId).IsUnicode(true);

      e.Property(p => p.RefParents).IsUnicode(false);

      e.Property(p => p.RefChildren).IsUnicode(false);

    });

    OnAfterMapping(builder);
  }

  public DbSet<Datasheet> Datasheets { get; set; }
  public DbSet<Currency> Currencies { get; set; }
  public DbSet<Company> Companies { get; set; }
  public DbSet<AssetClass> AssetClasses { get; set; }
}


[Table("TCompanies")]
public partial class Company : ICloneable<Company>
{
  public string Xid { get; set; }
  public DateTime ValidFrom { get; set; }
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public DateTime CreateTime { get; set; }
  public DateTime? DeleteTime { get; set; }
  public int Status { get; set; }
  [ConcurrencyCheck, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
  public DateTime Ts { get; set; }
  [Key]
  public string Id { get; set; }
  public string Name { get; set; }
  public string? Description { get; set; }

  [UseColumnAttribute, Column("Description_De")]
  public string? DescriptionDe { get; set; }

  [UseColumnAttribute, Column("Description_Fr")]
  public string? DescriptionFr { get; set; }

  [UseColumnAttribute, Column("Description_It")]
  public string? DescriptionIt { get; set; }

  [System.Xml.Serialization.XmlIgnore]
  public string? LDescription => LString.Get(Description, "de", DescriptionDe, "fr", DescriptionFr, "it", DescriptionIt);
  public string? Flags { get; set; }
  public string? LEI { get; set; }
  public string? BIC { get; set; }
  public string? BBG { get; set; }
  public int Type { get; set; }
  public string? ExtId { get; set; }
  public string? Tags { get; set; }

  [UseColumnAttribute, Column("Parents")]
  public string? RefParents { get; set; }

  [UseColumnAttribute, Column("Children")]
  public string? RefChildren { get; set; }

  public override string ToString() => Name!;


  public Company Clone() => (Company)MemberwiseClone();
}


[Table("TCurrencies")]
public partial class Currency : ICloneable<Currency>
{
  [Key]
  public string Id { get; set; }
  public string Xid { get; set; }
  public DateTime ValidFrom { get; set; }
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public DateTime CreateTime { get; set; }
  public DateTime? DeleteTime { get; set; }
  public int Status { get; set; }
  [ConcurrencyCheck, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
  public DateTime Ts { get; set; }
  public string Name { get; set; }

  [UseColumnAttribute, Column("Name_De")]
  public string? NameDe { get; set; }

  [UseColumnAttribute, Column("Name_Fr")]
  public string? NameFr { get; set; }

  [UseColumnAttribute, Column("Name_It")]
  public string? NameIt { get; set; }

  [System.Xml.Serialization.XmlIgnore]
  public string LName => LString.Get(Name, "de", NameDe, "fr", NameFr, "it", NameIt)!;
  public string? Description { get; set; }

  [UseColumnAttribute, Column("Description_De")]
  public string? DescriptionDe { get; set; }

  [UseColumnAttribute, Column("Description_Fr")]
  public string? DescriptionFr { get; set; }

  [UseColumnAttribute, Column("Description_It")]
  public string? DescriptionIt { get; set; }

  [System.Xml.Serialization.XmlIgnore]
  public string? LDescription => LString.Get(Description, "de", DescriptionDe, "fr", DescriptionFr, "it", DescriptionIt);
  public string? Flags { get; set; }
  public double? Ordering { get; set; }
  public int Type { get; set; }
  public string? ExtId { get; set; }

  [UseColumnAttribute, Column("Parents")]
  public string? RefParents { get; set; }

  [UseColumnAttribute, Column("Children")]
  public string? RefChildren { get; set; }

  public override string ToString() => LName!;


  public Currency Clone() => (Currency)MemberwiseClone();
}

[Table("TAssetClasses")]
public partial class AssetClass : ICloneable<AssetClass>
{
  [Key]
  public string Id { get; set; }
  public string Xid { get; set; }
  public DateTime ValidFrom { get; set; }
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public DateTime CreateTime { get; set; }
  public DateTime? DeleteTime { get; set; }
  public int Status { get; set; }
  [ConcurrencyCheck, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
  public DateTime Ts { get; set; }
  public string Name { get; set; }

  [UseColumnAttribute, Column("Name_De")]
  public string? NameDe { get; set; }

  [UseColumnAttribute, Column("Name_Fr")]
  public string? NameFr { get; set; }

  [UseColumnAttribute, Column("Name_It")]
  public string? NameIt { get; set; }

  [System.Xml.Serialization.XmlIgnore]
  public string LName => LString.Get(Name, "de", NameDe, "fr", NameFr, "it", NameIt)!;
  public string? Description { get; set; }

  [UseColumnAttribute, Column("Description_De")]
  public string? DescriptionDe { get; set; }

  [UseColumnAttribute, Column("Description_Fr")]
  public string? DescriptionFr { get; set; }

  [UseColumnAttribute, Column("Description_It")]
  public string? DescriptionIt { get; set; }

  [System.Xml.Serialization.XmlIgnore]
  public string? LDescription => LString.Get(Description, "de", DescriptionDe, "fr", DescriptionFr, "it", DescriptionIt);
  public double? Ordering { get; set; }
  public string? Flags { get; set; }
  public string? ExtId { get; set; }

  [UseColumnAttribute, Column("Parents")]
  public string? RefParents { get; set; }

  [UseColumnAttribute, Column("Children")]
  public string? RefChildren { get; set; }

  public override string ToString() => LName!;


  public AssetClass Clone() => (AssetClass)MemberwiseClone();
}

[Table("TDatasheets")]
public partial class Datasheet : ICloneable<Datasheet>
{
  public string Xid { get; set; }
  public DateTime ValidFrom { get; set; }
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public DateTime CreateTime { get; set; }
  public DateTime? DeleteTime { get; set; }
  public int Status { get; set; }
  [ConcurrencyCheck, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
  public DateTime Ts { get; set; }
  [Key]
  public string Id { get; set; }
  public string Name { get; set; }
  public string? Description { get; set; }

  [UseColumnAttribute, Column("ProductClass")]
  public string? ProductClassId { get; set; }

  [UseColumnAttribute, Column("PublicDef")]
  public string? ViewDefinition { get; set; }

  [UseColumnAttribute, Column("Validator")]
  public string? ValidatorDefinition { get; set; }
  public string? Flags { get; set; }

  public override string ToString() => Name!;


  public Datasheet Clone() => (Datasheet)MemberwiseClone();
}

