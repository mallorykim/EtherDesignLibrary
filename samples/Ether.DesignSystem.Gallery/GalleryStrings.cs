using System.Globalization;
using Windows.ApplicationModel.Resources;

namespace EtherSandbox;

/// <summary>
/// Gallery-only string lookup. XAML copy uses <c>x:Uid</c>; code-behind catalog names,
/// theme labels, and output format strings go through this helper so they share the same
/// <c>Strings/en-US/Resources.resw</c> source.
/// </summary>
internal static class GalleryStrings
{
    public static string Get(string key, string fallback)
    {
        try
        {
            var value = ResourceLoader.GetForViewIndependentUse().GetString(key);
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
        catch (Exception)
        {
            return fallback;
        }
    }

    public static string Format(string key, string fallbackFormat, params object[] args)
    {
        return string.Format(CultureInfo.CurrentCulture, Get(key, fallbackFormat), args);
    }

    public static string CatalogKey(string fallbackName) =>
        "Catalog." + fallbackName.Replace(" ", string.Empty, StringComparison.Ordinal);
}
