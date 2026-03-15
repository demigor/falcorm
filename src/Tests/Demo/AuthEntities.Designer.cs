//------------------------------------------------------------------------------
// This is auto-generated code.
//------------------------------------------------------------------------------
#nullable enable
#pragma warning disable CS8618
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Istarion.Falcorm;

namespace Entities;

[Table("TUsers")]
public partial class User : ICloneable<User>
{
  [Key]
  public string Id { get; set; }
  [ConcurrencyCheck, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
  public DateTime Ts { get; set; }
  public string Name { get; set; }
  public string? Description { get; set; }
  public string? ExtId { get; set; }
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public DateTime CreateTime { get; set; }
  public string? FullName { get; set; }
  public string? DisplayName { get; set; }
  public string? Email { get; set; }
  public string? EmailValidation { get; set; }
  public bool IsEnabled { get; set; }
  public bool IsLocked { get; set; }
  public DateTime? LastLoginTime { get; set; }
  public DateTime? LastPasswordChangeTime { get; set; }
  public string? Password { get; set; }
  public int PasswordType { get; set; }
  public int FailedLoginCount { get; set; }

  [Column("Roles")]
  public string? RefRoles { get; set; }

  [Column("MemberOf")]
  public string? RefMemberOf { get; set; }
  public int Status { get; set; }
  public string? LinkId { get; set; }
  public string? Updater { get; set; }
  public DateTime? UpdateTime { get; set; }

  public User Clone() => (User)MemberwiseClone();
}

[Table("TGroups")]
public partial class Group : ICloneable<Group>
{
  [Key]
  public string Id { get; set; }
  [ConcurrencyCheck]
  [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
  public DateTime Ts { get; set; }
  public string Name { get; set; }
  public string? Description { get; set; }
  public string? ExtId { get; set; }
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public DateTime CreateTime { get; set; }
  public bool IsEnabled { get; set; } = true;

  [Column("Roles")]
  public string? RefRoles { get; set; }

  [Column("MemberOf")]
  public string? RefMemberOf { get; set; }

  [Column("MemberUsers")]
  public string? RefMemberUsers { get; set; }

  [Column("MemberGroups")]
  public string? RefMemberGroups { get; set; }
  public int Status { get; set; }
  public string? LinkId { get; set; }
  public string? Updater { get; set; }
  public DateTime? UpdateTime { get; set; }

  public Group Clone() => (Group)MemberwiseClone();
}

[Table("TRoles")]
public partial class Role : ICloneable<Role>
{
  [Key]
  public string Id { get; set; }
  [ConcurrencyCheck, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
  public DateTime Ts { get; set; }
  public string? Name { get; set; }
  public string? Description { get; set; }

  [Column("Resources")]
  public string? RefResources { get; set; }

  public Role Clone() => (Role)MemberwiseClone();
}

[Table("TPermissions")]
public partial class Permission : ICloneable<Permission>
{
  [Key]
  public string RoleId { get; set; }
  [Key]
  public string ResourceId { get; set; }
  [ConcurrencyCheck, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
  public DateTime Ts { get; set; }
  public long PermissionSet { get; set; }

  public Permission Clone() => (Permission)MemberwiseClone();
}

[Table("TResources")]
public partial class Resource : ICloneable<Resource>
{
  [Key]
  public string Id { get; set; }
  [ConcurrencyCheck, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
  public DateTime Ts { get; set; }
  public string Name { get; set; }
  public string? Description { get; set; }
  public string? Class { get; set; }
  public string TypeId { get; set; }
  public int FilterType { get; set; }
  public string? Filter { get; set; }

  public Resource Clone() => (Resource)MemberwiseClone();
}

[Table("TResourceTypes")]
public partial class ResourceType : ICloneable<ResourceType>
{
  [Key]
  public string Id { get; set; }
  [ConcurrencyCheck, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
  public DateTime Ts { get; set; }
  public string Name { get; set; }
  public string? Description { get; set; }
  public string? BaseTypeId { get; set; }
  public long PermissionSet { get; set; }

  [Column("Resources")]
  public string? RefResources { get; set; }

  public ResourceType Clone() => (ResourceType)MemberwiseClone();
}

[Table("UserProfiles")]
public partial class UserProfile : ICloneable<UserProfile>
{
  [Key]
  public string UserId { get; set; }
  [Key]
  public string Key { get; set; }
  [ConcurrencyCheck, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
  public DateTime Ts { get; set; }
  public string? Value { get; set; }

  public UserProfile Clone() => (UserProfile)MemberwiseClone();
}

[Table("UserPasswords")]
public partial class UserPassword : ICloneable<UserPassword>
{
  [Key]
  public string UserId { get; set; }
  [ConcurrencyCheck, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
  public DateTime Ts { get; set; }
  [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public DateTime CreateTime { get; set; }
  public string? Password { get; set; }
  public int PasswordType { get; set; }

  public UserPassword Clone() => (UserPassword)MemberwiseClone();
}

[Table("Log")]
public partial class Log : ICloneable<Log>
{
  [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public int Id { get; set; }
  [ConcurrencyCheck, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
  public DateTime Ts { get; set; }
  public int Operation { get; set; }
  public string? Message { get; set; }
  public string? Data1 { get; set; }
  public string? Data2 { get; set; }

  public Log Clone() => (Log)MemberwiseClone();
}

[Table("Sessions")]
public partial class Session : ICloneable<Session>
{
  [Key]
  public string Id { get; set; }
  [ConcurrencyCheck, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
  public DateTime Ts { get; set; }
  public string Username { get; set; }
  public string Claims { get; set; }
  public DateTime Expiration { get; set; }

  public Session Clone() => (Session)MemberwiseClone();
}


