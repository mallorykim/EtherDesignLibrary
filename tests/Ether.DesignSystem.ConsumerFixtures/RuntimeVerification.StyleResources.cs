using System.Globalization;
using Ether.DesignSystem.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Ether.DesignSystem.ConsumerFixtures;

internal static partial class RuntimeVerification
{
    private static readonly string[] CardStyleKeys =
    [
        "EtherCardNormal",
        "EtherCardNormalBody",
        "EtherCardIntelligence",
        "EtherCardIntelligenceBody",
        "EtherCardCalloutShell",
        "EtherCardCalloutBody",
    ];

    private static async Task<StyleResourceVerification> VerifyStyleResourcesAsync(FrameworkElement themeRoot)
    {
        if (themeRoot is not Panel host)
            throw new InvalidOperationException("Style-resource probes require a panel-based theme root.");

        var cardStyles = CardStyleKeys.Select(key => (Key: key, Style: FindApplicationResource(key) as Style)).ToArray();
        if (cardStyles.Any(item => item.Style is null || item.Style.TargetType != typeof(Border)))
            throw new InvalidOperationException("The six EtherCard styles did not resolve as Border-targeted styles.");

        var switchStyle = FindApplicationResource("EtherSwitch") as Style;
        if (switchStyle is null || switchStyle.TargetType != typeof(ToggleSwitch))
            throw new InvalidOperationException("The keyed EtherSwitch style did not resolve as a ToggleSwitch-targeted style.");

        var scrollStyle = FindApplicationResource(typeof(ScrollBar)) as Style;
        if (scrollStyle is null || scrollStyle.TargetType != typeof(ScrollBar))
            throw new InvalidOperationException("The implicit EtherScrollBar style did not resolve as a ScrollBar-targeted style.");

        var scratch = new Canvas { Opacity = 0d, IsHitTestVisible = false };
        host.Children.Add(scratch);
        try
        {
            var cardBorders = cardStyles.Select(item => new Border { Style = item.Style }).ToArray();
            foreach (var border in cardBorders)
                scratch.Children.Add(border);

            var switchControl = new ToggleSwitch { Style = switchStyle };
            var scrollBar = new ScrollBar { Style = scrollStyle, Orientation = Orientation.Vertical, Maximum = 100, ViewportSize = 20 };
            scratch.Children.Add(switchControl);
            scratch.Children.Add(scrollBar);
            await WaitForAppliedThemeAsync(themeRoot, ElementTheme.Light);
            foreach (var border in cardBorders)
            {
                border.UpdateLayout();
                Assert(border.Style is not null && border.Background is Brush, "An EtherCard style did not apply its Border brush.");
            }
            switchControl.ApplyTemplate();
            switchControl.UpdateLayout();
            scrollBar.ApplyTemplate();
            scrollBar.UpdateLayout();
            GetTemplatePart<Rectangle>(switchControl, "OuterBorder", "EtherSwitch style fixture");
            GetTemplatePart<Grid>(scrollBar, "VerticalRoot", "EtherScrollBar style fixture");
            GetTemplatePart<Thumb>(scrollBar, "VerticalThumb", "EtherScrollBar style fixture");
            var lightBrushes = cardBorders.Select(border => DescribeBrush(border.Background)).ToArray();

            themeRoot.RequestedTheme = ElementTheme.Dark;
            await WaitForAppliedThemeAsync(themeRoot, ElementTheme.Dark);
            foreach (var border in cardBorders)
                border.UpdateLayout();
            switchControl.UpdateLayout();
            scrollBar.UpdateLayout();
            var darkBrushes = cardBorders.Select(border => DescribeBrush(border.Background)).ToArray();
            if (lightBrushes.SequenceEqual(darkBrushes, StringComparer.Ordinal))
                throw new InvalidOperationException("EtherCard themed brushes did not re-resolve between Light and Dark.");

            return new StyleResourceVerification(
                CardStyleKeys,
                true,
                true,
                lightBrushes,
                darkBrushes,
                false);
        }
        finally
        {
            host.Children.Remove(scratch);
        }
    }

    private static bool VerifyStyleResourcesInHighContrast(FrameworkElement themeRoot)
    {
        if (themeRoot is not Panel host)
            throw new InvalidOperationException("High-contrast style probes require a panel-based theme root.");

        var scratch = new Canvas { Opacity = 0d, IsHitTestVisible = false };
        host.Children.Add(scratch);
        try
        {
            var card = new Border { Style = FindApplicationResource("EtherCardNormalBody") as Style };
            var switchControl = new ToggleSwitch { Style = FindApplicationResource("EtherSwitch") as Style };
            var scrollBar = new ScrollBar
            {
                Style = FindApplicationResource(typeof(ScrollBar)) as Style,
                Orientation = Orientation.Vertical,
                Maximum = 100,
                ViewportSize = 20,
            };
            scratch.Children.Add(card);
            scratch.Children.Add(switchControl);
            scratch.Children.Add(scrollBar);
            card.UpdateLayout();
            switchControl.ApplyTemplate();
            switchControl.UpdateLayout();
            scrollBar.ApplyTemplate();
            scrollBar.UpdateLayout();

            Assert(card.Background is SolidColorBrush, "EtherCard did not resolve a High Contrast body brush.");
            var switchTrack = GetTemplatePart<Rectangle>(switchControl, "OuterBorder", "EtherSwitch High Contrast style fixture");
            Assert(switchTrack.Fill is SolidColorBrush, "EtherSwitch did not resolve a High Contrast track brush.");
            var thumb = GetTemplatePart<Thumb>(scrollBar, "VerticalThumb", "EtherScrollBar High Contrast style fixture");
            thumb.ApplyTemplate();
            thumb.UpdateLayout();
            var thumbFill = GetTemplatePart<Border>(thumb, "ThumbFill", "EtherScrollBar High Contrast style fixture");
            Assert(thumbFill.Background is SolidColorBrush, "EtherScrollBar did not resolve a High Contrast thumb brush.");
            return true;
        }
        finally
        {
            host.Children.Remove(scratch);
        }
    }

    private static object? FindApplicationResource(object key)
    {
        var resources = Application.Current?.Resources
            ?? throw new InvalidOperationException("Application resources are not available for style fixtures.");
        return FindResource(resources, key);
    }

    private static object? FindResource(ResourceDictionary resources, object key)
    {
        if (resources.TryGetValue(key, out var value))
            return value;

        foreach (var merged in resources.MergedDictionaries)
        {
            var nested = FindResource(merged, key);
            if (nested is not null)
                return nested;
        }

        return null;
    }

    private static string DescribeBrush(Brush? brush) => brush switch
    {
        SolidColorBrush solid => solid.Color.ToString(CultureInfo.InvariantCulture),
        LinearGradientBrush gradient => string.Join("|", gradient.GradientStops.Select(stop => stop.Color.ToString(CultureInfo.InvariantCulture))),
        _ => brush?.GetType().Name ?? string.Empty,
    };
}
