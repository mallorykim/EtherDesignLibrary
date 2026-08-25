using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Views.Foundations;

public sealed partial class SpacingPage : Page
{
    public SpacingPage()
    {
        this.InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PrimitiveGroups.ItemsSource = BuildGroups();
    }

    private static IReadOnlyList<SpacingPrimitiveGroup> BuildGroups()
    {
        var primitives = FindPrimitiveDictionary(Application.Current.Resources);
        if (primitives is null)
            return Array.Empty<SpacingPrimitiveGroup>();

        var spacing = new List<SpacingPrimitiveItem>();

        foreach (var key in primitives.Keys)
        {
            if (key is not string name || primitives[key] is not double value)
                continue;

            if (name.StartsWith("Spacing", StringComparison.Ordinal))
            {
                spacing.Add(new SpacingPrimitiveItem(
                    name["Spacing".Length..], value.ToString(), value, 16, new CornerRadius(0)));
            }
        }

        return new[]
        {
            new SpacingPrimitiveGroup("Spacing", spacing.OrderBy(i => double.Parse(i.Value)).ToArray()),
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

public sealed record SpacingPrimitiveItem(
    string Name,
    string Value,
    double PreviewWidth,
    double PreviewHeight,
    CornerRadius PreviewRadius);

public sealed record SpacingPrimitiveGroup(string Header, IReadOnlyList<SpacingPrimitiveItem> Items);
