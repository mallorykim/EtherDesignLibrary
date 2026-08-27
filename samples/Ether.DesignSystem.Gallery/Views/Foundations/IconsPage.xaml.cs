using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

namespace EtherSandbox.Views.Foundations;

public sealed partial class IconsPage : Page
{
    private const int ColumnsPerRow = 12;

    /// <summary>Sample icon used to preview the IconSize scale — any icon works, this one's
    /// just simple and recognizable at every size. Kept as raw path data (not looked up from
    /// EtherIconGeometries.xaml) because each size row needs its OWN Geometry instance — sharing
    /// one Geometry/Style object across multiple simultaneously-rendered PathIcon siblings causes
    /// only the first to render, so a fresh XamlReader.Load() per row is required.</summary>
    private const string SizeSampleIconData =
        "F1 M16 8.94012L14.33 7.15012L8 0.370117L1.7 7.12012L0 8.94012L0.74 9.64012L1.66 8.64012V15.6401H6.52V10.0001H9.52V15.6601H14.37V8.66012L15.3 9.66012L16.05 8.96012L16 8.94012ZM13.31 14.6101H10.49V9.00012H5.49V14.6601H2.68V7.66012L8 1.86012L13.31 7.55012V14.6101Z";

    public IconsPage()
    {
        this.InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var iconStyles = MergedResourceDictionaries.Find(
            Application.Current.Resources, "EtherIconGeometries.xaml", "IconAddCir");
        var primitives = MergedResourceDictionaries.Find(
            Application.Current.Resources, "EtherPrimitives.xaml", "IconSizeMd");

        BuildSizes(primitives);
        BuildGallery(iconStyles);
    }

    private void BuildSizes(ResourceDictionary? primitives)
    {
        if (primitives is null)
        {
            SizeItems.ItemsSource = Array.Empty<IconSizeItem>();
            return;
        }

        var sizes = new List<IconSizeItem>();
        foreach (var key in primitives.Keys)
        {
            if (key is not string name || primitives[key] is not double value)
                continue;

            if (name.StartsWith("IconSize", StringComparison.Ordinal))
                sizes.Add(new IconSizeItem(name["IconSize".Length..].ToLowerInvariant(), value, LoadSampleGeometry()));
        }

        SizeItems.ItemsSource = sizes.OrderBy(i => i.Size).ToArray();
    }

    private static Geometry LoadSampleGeometry()
    {
        var xaml = $"<Geometry xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">{SizeSampleIconData}</Geometry>";
        return (Geometry)XamlReader.Load(xaml);
    }

    private void BuildGallery(ResourceDictionary? iconStyles)
    {
        if (iconStyles is null)
        {
            IconRows.ItemsSource = Array.Empty<IconGalleryRow>();
            CountText.Text = "0 icons";
            return;
        }

        var icons = new List<IconGalleryItem>();
        foreach (var key in iconStyles.Keys)
        {
            if (key is not string name || iconStyles[key] is not Style style)
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
}

public sealed record IconGalleryItem(string Name, Style IconStyle);

public sealed record IconGalleryRow(IReadOnlyList<IconGalleryItem> Items);

public sealed record IconSizeItem(string Name, double Size, Geometry IconData);
