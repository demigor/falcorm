namespace Istarion.Generators;

/// <summary>
/// Helper for marking readonly fields in PatchDTO projection.
/// Fields marked with ReadOnly.Value() are excluded from the generated PatchDTO.
/// </summary>
public static class Helpers
{
  public static T Value<T>(T value) => value;
}

/// <summary>
/// Helper for explicitly marking writable fields in PatchDTO projection.
/// Fields marked with Writable.Value() generate regular auto-properties {get; set;} with their own value,
/// even if the field is computed.
/// </summary>
public static class Writable
{
  public static T Value<T>(T value) => value;
}
