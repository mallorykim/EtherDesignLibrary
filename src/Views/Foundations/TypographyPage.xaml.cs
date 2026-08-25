using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;

namespace EtherSandbox.Views.Foundations;

public sealed partial class TypographyPage : Page
{
    public TypographyPage()
    {
        this.InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PrimitiveGroups.ItemsSource = BuildGroups();
    }

    private const double PreviewSize = 20;
    private const string PreviewText = "Ag";

    private static IReadOnlyList<TypographyPrimitiveGroup> BuildGroups()
    {
        var primitives = FindPrimitiveDictionary(Application.Current.Resources);
        if (primitives is null)
            return Array.Empty<TypographyPrimitiveGroup>();

        var displayFamily = (FontFamily)Application.Current.Resources["InstrumentSans"];
        var bodyFamily = (FontFamily)Application.Current.Resources["InterFont"];
        var regularWeight = (FontWeight)Application.Current.Resources["WeightRegular"];

        var sizes = new List<TypographyPrimitiveItem>();
        var weights = new List<TypographyPrimitiveItem>();

        foreach (var key in primitives.Keys)
        {
            if (key is not string name)
                continue;

            if (name.StartsWith("Size", StringComparison.Ordinal) &&
                int.TryParse(name.AsSpan(4), out var size) &&
                primitives[key] is double)
            {
                sizes.Add(new TypographyPrimitiveItem(size.ToString(), size.ToString(), PreviewText, size, regularWeight, displayFamily));
            }
            else if (name.StartsWith("Weight", StringComparison.Ordinal) &&
                     primitives[key] is FontWeight weight)
            {
                weights.Add(new TypographyPrimitiveItem(name["Weight".Length..].ToLowerInvariant(), weight.Weight.ToString(), PreviewText, PreviewSize, weight, displayFamily));
            }
        }

        var families = new List<TypographyPrimitiveItem>
        {
            new("display", displayFamily.Source.Split('#').Last(), PreviewText, PreviewSize, regularWeight, displayFamily),
            new("body", bodyFamily.Source.Split('#').Last(), PreviewText, PreviewSize, regularWeight, bodyFamily),
        };

        return new[]
        {
            new TypographyPrimitiveGroup("Size", sizes.OrderBy(i => int.Parse(i.Name)).ToArray()),
            new TypographyPrimitiveGroup("Weight", weights.OrderBy(i => int.Parse(i.Value)).ToArray()),
            new TypographyPrimitiveGroup("Family", families),
        };
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

public sealed record TypographyPrimitiveItem(
    string Name,
    string Value,
    string PreviewText,
    double PreviewSize,
    FontWeight PreviewWeight,
    FontFamily PreviewFamily);

public sealed record TypographyPrimitiveGroup(string Header, IReadOnlyList<TypographyPrimitiveItem> Items);
