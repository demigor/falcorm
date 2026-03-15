namespace Istarion.Falcorm;

/// <summary>
/// Defines a generic contract for creating a deep or shallow copy of an instance.
/// </summary>
/// <typeparam name="T">The type of the object to clone.</typeparam>
public interface ICloneable<T>
{
  /// <summary>
  /// Creates a copy of the current instance.
  /// </summary>
  /// <returns>A new instance of type <typeparamref name="T"/> that is a copy of this object.</returns>
  T Clone();
}

