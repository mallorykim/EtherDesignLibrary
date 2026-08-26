using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Views.Foundations;

public sealed partial class IconsPage : Page
{
    private const int ColumnsPerRow = 12;

    public IconsPage()
    {
        this.InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var styles = FindIconDictionary(Application.Current.Resources);
        if (styles is null)
        {
            IconRows.ItemsSource = Array.Empty<IconGalleryRow>();
            CountText.Text = "0 icons";
            return;
        }

        var icons = new List<IconGalleryItem>();
        foreach (var key in styles.Keys)
        {
            if (key is not string name || styles[key] is not Style style)
                continue;

            if (name.StartsWith("Icon", StringComparison.Ordinal))
                icons.Add(new IconGalleryItem(name["Icon".Length..], style));
        }

        var sorted = icons.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase).ToArray();

        var rows = new List<IconGalleryRow>();
        for (var i = 0; i < sorted.Length; i += ColumnsPerRow)
            rows.Add(new IconGalleryRow(sorted.Skip(i).Take(ColumnsPerRow).ToArray()));

        IconRows.ItemsSource = rows;
        CountText.Text = $"{sorted.Length} icons";
    }

    private static ResourceDictionary? FindIconDictionary(ResourceDictionary root)
    {
        foreach (var merged in root.MergedDictionaries)
        {
            if (merged.Source is not null &&
                merged.Source.OriginalString.EndsWith("EtherIconGeometries.xaml", StringComparison.OrdinalIgnoreCase))
            {
                return merged;
            }

            if (FindIconDictionary(merged) is { } found)
                return found;
        }

        return null;
    }
}

public sealed record IconGalleryItem(string Name, Style IconStyle);

public sealed record IconGalleryRow(IReadOnlyList<IconGalleryItem> Items);
