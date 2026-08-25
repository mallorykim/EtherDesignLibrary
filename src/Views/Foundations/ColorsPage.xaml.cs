using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace EtherSandbox.Views.Foundations;

public sealed partial class ColorsPage : Page
{
    public ColorsPage()
    {
        this.InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PrimitiveGroups.ItemsSource = BuildGroups();
    }

    private static IReadOnlyList<PrimitiveColorGroup> BuildGroups()
    {
        var primitives = FindPrimitiveDictionary(Application.Current.Resources);
        if (primitives is null)
            return Array.Empty<PrimitiveColorGroup>();

        var groups = new Dictionary<string, List<PrimitiveColorItem>>(StringComparer.OrdinalIgnoreCase);
        var orderedGroups = new List<string>();

        foreach (var key in primitives.Keys)
        {
            if (key is not string name || primitives[key] is not Color color)
                continue;

            var series = GetSeriesName(name);
            if (!groups.TryGetValue(series, out var items))
            {
                items = new List<PrimitiveColorItem>();
                groups.Add(series, items);
                orderedGroups.Add(series);
            }

            items.Add(new PrimitiveColorItem(name, color));
        }

        return orderedGroups
            .Select(group => new PrimitiveColorGroup(
                group,
                groups[group]
                    .OrderByDescending(item => GetDisplayedBrightness(item.Color))
                    .ThenByDescending(item => item.Color.A)
                    .ThenBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                    .ToArray()))
            .ToArray();
    }

    private static string GetSeriesName(string key)
    {
        int split = 0;
        while (split < key.Length && char.IsLetter(key[split]))
            split++;

        return split == 0 ? "Other" : key[..split];
    }

    private static double GetDisplayedBrightness(Color color)
    {
        double alpha = color.A / 255.0;
        double r = color.R * alpha + 255.0 * (1 - alpha);
        double g = color.G * alpha + 255.0 * (1 - alpha);
        double b = color.B * alpha + 255.0 * (1 - alpha);
        return 0.299 * r + 0.587 * g + 0.114 * b;
    }

    private static ResourceDictionary? FindPrimitiveDictionary(ResourceDictionary root)
    {
        foreach (var merged in root.MergedDictionaries)
        {
            if (merged.Source is not null &&
                merged.Source.OriginalString.EndsWith("EtherPrimitives.xaml", StringComparison.OrdinalIgnoreCase))
            {
                return merged;
            }

            if (FindPrimitiveDictionary(merged) is { } found)
                return found;
        }

        return null;
    }
}

public sealed class PrimitiveColorItem(string key, Color color)
{
    public string Key { get; } = key;
    public Color Color { get; } = color;
    public Brush Brush { get; } = new SolidColorBrush(color);
    public string Hex { get; } = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
}

public sealed record PrimitiveColorGroup(string Header, IReadOnlyList<PrimitiveColorItem> Items);
