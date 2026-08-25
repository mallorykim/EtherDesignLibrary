using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Views.Foundations;

public sealed partial class RadiusPage : Page
{
    public RadiusPage()
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

        var radius = new List<SpacingPrimitiveItem>();

        foreach (var key in primitives.Keys)
        {
            if (key is not string name || primitives[key] is not CornerRadius cornerRadius)
                continue;

            if (name.StartsWith("Radius", StringComparison.Ordinal))
            {
                var value = cornerRadius.TopLeft;
                radius.Add(new SpacingPrimitiveItem(
                    name["Radius".Length..].ToLowerInvariant(), value.ToString(), 40, 40, cornerRadius));
            }
        }

        return new[]
        {
            new SpacingPrimitiveGroup("Radius", radius.OrderBy(i => double.Parse(i.Value)).ToArray()),
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
