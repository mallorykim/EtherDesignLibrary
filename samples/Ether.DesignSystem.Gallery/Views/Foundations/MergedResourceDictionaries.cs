using System;
using System.IO;
using Microsoft.UI.Xaml;

namespace EtherSandbox.Views.Foundations;

/// <summary>
/// Locates a merged token dictionary after Foundation compiled the graph into a class library.
/// Nested <see cref="ResourceDictionary.Source"/> is often null or an <c>.xbf</c> URI, so a
/// filename suffix match is not enough; fall back to a key that lives on that dictionary.
/// </summary>
internal static class MergedResourceDictionaries
{
    public static ResourceDictionary? Find(ResourceDictionary root, string fileName, string localKey)
    {
        return FindByFileName(root, fileName) ?? FindByLocalKey(root, localKey);
    }

    private static ResourceDictionary? FindByFileName(ResourceDictionary root, string fileName)
    {
        foreach (var merged in root.MergedDictionaries)
        {
            if (SourceMatches(merged.Source, fileName))
                return merged;

            if (FindByFileName(merged, fileName) is { } nested)
                return nested;
        }

        return null;
    }

    private static ResourceDictionary? FindByLocalKey(ResourceDictionary root, string localKey)
    {
        if (HasLocalKey(root, localKey))
            return root;

        foreach (var merged in root.MergedDictionaries)
        {
            if (FindByLocalKey(merged, localKey) is { } nested)
                return nested;
        }

        return null;
    }

    private static bool HasLocalKey(ResourceDictionary dictionary, string localKey)
    {
        foreach (var key in dictionary.Keys)
        {
            if (key is string name && string.Equals(name, localKey, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static bool SourceMatches(Uri? source, string fileName)
    {
        if (source is null)
            return false;

        var path = source.OriginalString;
        if (string.IsNullOrEmpty(path))
            return false;

        if (path.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
            return true;

        var xbfName = Path.ChangeExtension(fileName, ".xbf");
        return !string.IsNullOrEmpty(xbfName) &&
               path.EndsWith(xbfName, StringComparison.OrdinalIgnoreCase);
    }
}
