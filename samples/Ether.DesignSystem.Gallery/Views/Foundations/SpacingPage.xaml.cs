using System;
using System.Collections.Generic;
using System.Globalization;
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
        SemanticGroups.ItemsSource = BuildSemanticGroups();
        PrimitiveGroups.ItemsSource = BuildGroups();
    }

    // Semantic space tokens (EtherSpacing.xaml), each aliased to a primitive Spacing* value.
    // (name shown, primitive it links to, resource key). The value is read back from the
    // resolved resource so it can never drift from the token / the alias it points at.
    private static readonly (string Name, string Primitive, string Key)[] SemanticSpace =
    {
        ("3xs", "spacing/2",  "space/3xs"),
        ("2xs", "spacing/4",  "space/2xs"),
        ("xs",  "spacing/8",  "space/xs"),
        ("sm",  "spacing/12", "space/sm"),
        ("md",  "spacing/16", "space/md"),
        ("lg",  "spacing/24", "space/lg"),
        ("xl",  "spacing/32", "space/xl"),
        ("2xl", "spacing/48", "space/2xl"),
        ("3xl", "spacing/64", "space/3xl"),
        ("4xl", "spacing/96", "space/4xl"),
    };

    private static SpacingSemanticGroup[] BuildSemanticGroups()
    {
        var res = Application.Current.Resources;
        var items = new List<SpacingSemanticItem>();

        foreach (var (name, primitive, key) in SemanticSpace)
        {
            if (res.TryGetValue(key, out var raw) && raw is double value)
                items.Add(new SpacingSemanticItem(name, $"{primitive} = {value} epx", value));
        }

        return new[] { new SpacingSemanticGroup("Space", items) };
    }

    private static SpacingPrimitiveGroup[] BuildGroups()
    {
        var primitives = MergedResourceDictionaries.Find(
            Application.Current.Resources, "EtherPrimitives.xaml", "Spacing8");
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
                    name["Spacing".Length..], value.ToString(CultureInfo.CurrentCulture), value, 16, new CornerRadius(0)));
            }
        }

        return new[]
        {
            new SpacingPrimitiveGroup("Spacing", spacing.OrderBy(i => double.Parse(i.Value, CultureInfo.CurrentCulture)).ToArray()),
        };
    }
}

public sealed record SpacingPrimitiveItem(
    string Name,
    string Value,
    double PreviewWidth,
    double PreviewHeight,
    CornerRadius PreviewRadius);

public sealed record SpacingPrimitiveGroup(string Header, IReadOnlyList<SpacingPrimitiveItem> Items);

public sealed record SpacingSemanticItem(string Name, string Link, double PreviewWidth);

public sealed record SpacingSemanticGroup(string Header, IReadOnlyList<SpacingSemanticItem> Items);
