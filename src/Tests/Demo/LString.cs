using System.Diagnostics.CodeAnalysis;

namespace Entities;

public sealed class LText
{
  [return: NotNullIfNotNull(nameof(@default))]
  public static string? Get(string? @default, string? de = null, string? fr = null, string? it = null)
    => LString.CurrentLanguage.ToLower() switch { "en" => @default, "de" => de ?? @default, "fr" => fr ?? @default, "it" => it ?? @default, _ => @default };
}

public sealed class LString
{
  [return: NotNullIfNotNull(nameof(@default))]
  public static string? Get(string? @default, params string?[] translations) => LS.Get(Thread.CurrentThread.CurrentCulture.Name, @default, translations);

  public static string CurrentLanguage
  {
    get
    {
      var culture = Thread.CurrentThread.CurrentCulture.Name;
      var si = culture.IndexOf('-');
      return (si >= 0) ? culture[..si] : culture;
    }
  }
}

public sealed class LS
{
  [return: NotNullIfNotNull(nameof(@default))]
  public static string? Get(string culture, string? @default, params string?[] translations)
  {
    var si = culture.IndexOf('-');
    var main = default(string);
    if (si >= 0)
      main = culture[..si];

    for (var i = 0; i < translations.Length; i += 2)
    {
      var key = translations[i];
      if (key == culture || key == main)
      {
        var x = translations[i + 1];
        return string.IsNullOrEmpty(x) ? @default : x;
      }
    }

    return @default;
  }
}
