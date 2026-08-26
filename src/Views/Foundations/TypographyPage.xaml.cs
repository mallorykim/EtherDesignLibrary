using System;
using System.Collections.Generic;
using System.Globalization;
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
        SemanticGroups.ItemsSource = BuildSemanticGroups();
        PrimitiveGroups.ItemsSource = BuildGroups();
    }

    private const double PreviewSize = 20;
    private const string PreviewText = "Ag";
    private const string SemanticPreview = "The quick brown fox";

    // Semantic text styles from EtherTypography.xaml, grouped as in the Text styles panel.
    // Each row previews the live Style resource so it can never drift from the token.
    private static TypographySemanticGroup[] BuildSemanticGroups()
    {
        var res = Application.Current.Resources;
        TypographySemanticItem Item(string name, string spec, string styleKey) =>
            new(name, spec, SemanticPreview, (Style)res[styleKey]);

        return new[]
        {
            new TypographySemanticGroup("Display", new[]
            {
                Item("Display 96", "Instrument Sans / 96 epx / Bold", "display/96"),
                Item("Display 48", "Instrument Sans / 48 epx / Bold", "display/48"),
            }),
            new TypographySemanticGroup("Headers", new[]
            {
                Item("H1", "Instrument Sans / 24 epx / SemiBold", "headers/h1"),
                Item("H2", "Instrument Sans / 20 epx / SemiBold", "headers/h2"),
                Item("H3", "Instrument Sans / 18 epx / SemiBold", "headers/h3"),
                Item("H4", "Instrument Sans / 16 epx / SemiBold", "headers/h4"),
                Item("H5", "Instrument Sans / 14 epx / SemiBold", "headers/h5"),
                Item("H6", "Instrument Sans / 12 epx / SemiBold", "headers/h6"),
            }),
            new TypographySemanticGroup("Body", new[]
            {
                Item("Body XL Regular", "Inter / 18 epx / Regular", "body/xl-regular"),
                Item("Body XL SemiBold", "Inter / 18 epx / SemiBold", "body/xl-semibold"),
                Item("Body L Regular", "Inter / 16 epx / Regular", "body/l-regular"),
                Item("Body L SemiBold", "Inter / 16 epx / SemiBold", "body/l-semibold"),
                Item("Body M Regular", "Inter / 14 epx / Regular", "body/m-regular"),
                Item("Body M SemiBold", "Inter / 14 epx / SemiBold", "body/m-semibold"),
                Item("Body S Regular", "Inter / 12 epx / Regular", "body/s-regular"),
                Item("Body S SemiBold", "Inter / 12 epx / SemiBold", "body/s-semibold"),
            }),
            new TypographySemanticGroup("Micro", new[]
            {
                Item("Micro Regular", "Inter / 11 epx / Regular", "micro/regular"),
                Item("Micro SemiBold", "Inter / 11 epx / SemiBold", "micro/semibold"),
            }),
        };
    }

    private static TypographyPrimitiveGroup[] BuildGroups()
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
                sizes.Add(new TypographyPrimitiveItem(size.ToString(CultureInfo.CurrentCulture), $"{size} epx", PreviewText, size, regularWeight, displayFamily));
            }
            else if (name.StartsWith("Weight", StringComparison.Ordinal) &&
                     primitives[key] is FontWeight weight)
            {
                weights.Add(new TypographyPrimitiveItem(name["Weight".Length..].ToLowerInvariant(), weight.Weight.ToString(CultureInfo.CurrentCulture), PreviewText, PreviewSize, weight, displayFamily));
            }
        }

        var families = new List<TypographyPrimitiveItem>
        {
            new("display", displayFamily.Source.Split('#').Last(), PreviewText, PreviewSize, regularWeight, displayFamily),
            new("body", bodyFamily.Source.Split('#').Last(), PreviewText, PreviewSize, regularWeight, bodyFamily),
        };

        return new[]
        {
            new TypographyPrimitiveGroup("Size", sizes.OrderBy(i => int.Parse(i.Name, CultureInfo.CurrentCulture)).ToArray()),
            new TypographyPrimitiveGroup("Weight", weights.OrderBy(i => int.Parse(i.Value, CultureInfo.CurrentCulture)).ToArray()),
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

public sealed record TypographySemanticItem(
    string Name,
    string Spec,
    string PreviewText,
    Style PreviewStyle);

public sealed record TypographySemanticGroup(string Header, IReadOnlyList<TypographySemanticItem> Items);
