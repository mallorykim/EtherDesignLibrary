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
        SemanticGroups.ItemsSource = BuildSemanticGroups();
        PrimitiveGroups.ItemsSource = BuildGroups();
    }

    // Semantic radius tokens (EtherSpacing.xaml), each aliased to a primitive Radius* value.
    // (name shown, primitive it links to, resource key). The CornerRadius is read back from
    // the resolved resource so the preview and value can't drift from the token / its alias.
    private static readonly (string Name, string Primitive, string Key)[] SemanticRadius =
    {
        ("control-sm", "radius/sm",   "radius/control-sm"),
        ("control",    "radius/md",   "radius/control"),
        ("control-lg", "radius/lg",   "radius/control-lg"),
        ("surface",    "radius/lg",   "radius/surface"),
        ("surface-lg", "radius/xl",   "radius/surface-lg"),
        ("pill",       "radius/full", "radius/pill"),
    };

    private static IReadOnlyList<SpacingPrimitiveGroup> BuildSemanticGroups()
    {
        var res = Application.Current.Resources;
        var items = new List<SpacingPrimitiveItem>();

        foreach (var (name, primitive, key) in SemanticRadius)
        {
            if (res.TryGetValue(key, out var raw) && raw is CornerRadius cr)
            {
                var v = cr.TopLeft;
                var label = v >= 9999 ? primitive : $"{primitive} = {v} epx";
                items.Add(new SpacingPrimitiveItem(name, label, 40, 40, cr));
            }
        }

        return new[] { new SpacingPrimitiveGroup("Radius", items) };
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
