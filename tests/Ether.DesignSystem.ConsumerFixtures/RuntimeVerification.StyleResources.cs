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

    private static readonly string[] TooltipStyleKeys = [ "EtherTooltip" ];

    private static readonly string[] PanelTabsStyleKeys = [ "EtherPanelTabs", "EtherPanelTabSegment" ];

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

        var tooltipStyle = FindApplicationResource("EtherTooltip") as Style;
        if (tooltipStyle is null || tooltipStyle.TargetType != typeof(Border))
            throw new InvalidOperationException("The EtherTooltip style did not resolve as a Border-targeted style.");

        var panelTabsStyle = FindApplicationResource("EtherPanelTabs") as Style;
        if (panelTabsStyle is null || panelTabsStyle.TargetType != typeof(EtherSegmentedControl))
            throw new InvalidOperationException("The EtherPanelTabs style did not resolve as an EtherSegmentedControl-targeted style.");

        var panelTabSegmentStyle = FindApplicationResource("EtherPanelTabSegment") as Style;
        if (panelTabSegmentStyle is null || panelTabSegmentStyle.TargetType != typeof(RadioButton))
            throw new InvalidOperationException("The EtherPanelTabSegment style did not resolve as a RadioButton-targeted style.");

        Assert(panelTabSegmentStyle.Setters.OfType<Setter>().Any(s => s.Property == Control.TemplateProperty),
            "EtherPanelTabSegment style did not carry a Template setter.");

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
            var tooltip = new Border { Style = tooltipStyle };
            var panelTabsHost = new EtherSegmentedControl { Style = panelTabsStyle };
            scratch.Children.Add(tooltip);
            scratch.Children.Add(panelTabsHost);
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
            tooltip.UpdateLayout();
            panelTabsHost.UpdateLayout();
            Assert(tooltip.Background is Brush, "EtherTooltip style did not apply its themed Background brush.");
            Assert(panelTabsHost.Background is Brush, "EtherPanelTabs style did not apply its themed track Background brush.");
            var tooltipLight = DescribeBrush(tooltip.Background);
            var panelTabsLight = DescribeBrush(panelTabsHost.Background);
            var lightBrushes = cardBorders.Select(border => DescribeBrush(border.Background)).ToArray();

            themeRoot.RequestedTheme = ElementTheme.Dark;
            await WaitForAppliedThemeAsync(themeRoot, ElementTheme.Dark);
            foreach (var border in cardBorders)
                border.UpdateLayout();
            switchControl.UpdateLayout();
            scrollBar.UpdateLayout();
            tooltip.UpdateLayout();
            panelTabsHost.UpdateLayout();
            var darkBrushes = cardBorders.Select(border => DescribeBrush(border.Background)).ToArray();
            if (lightBrushes.SequenceEqual(darkBrushes, StringComparer.Ordinal))
                throw new InvalidOperationException("EtherCard themed brushes did not re-resolve between Light and Dark.");
            var tooltipDark = DescribeBrush(tooltip.Background);
            var panelTabsDark = DescribeBrush(panelTabsHost.Background);
            if (string.Equals(tooltipLight, tooltipDark, StringComparison.Ordinal))
                throw new InvalidOperationException("EtherTooltip themed brush did not re-resolve between Light and Dark.");
            if (string.Equals(panelTabsLight, panelTabsDark, StringComparison.Ordinal))
                throw new InvalidOperationException("EtherPanelTabs themed track brush did not re-resolve between Light and Dark.");

            return new StyleResourceVerification(
                CardStyleKeys,
                true,
                true,
                lightBrushes,
                darkBrushes,
                false,
                TooltipStyleKeys,
                true,
                PanelTabsStyleKeys,
                true);
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

            var tooltipHc = new Border { Style = FindApplicationResource("EtherTooltip") as Style };
            var panelTabsHc = new EtherSegmentedControl { Style = FindApplicationResource("EtherPanelTabs") as Style };
            scratch.Children.Add(tooltipHc);
            scratch.Children.Add(panelTabsHc);
            tooltipHc.UpdateLayout();
            Assert(tooltipHc.Background is SolidColorBrush, "EtherTooltip did not resolve a High Contrast background brush.");
            Assert(panelTabsHc.Background is SolidColorBrush, "EtherPanelTabs did not resolve a High Contrast track brush.");
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
