using EtherSandbox.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace Ether.DesignSystem.ConsumerFixtures;

internal static partial class RuntimeVerification
{

    private static async Task<ScrollBarVerification> VerifyScrollBarAsync(
        FrameworkElement themeRoot,
        ScrollBar scrollBar,
        ScrollViewer scrollViewer)
    {
        scrollBar.ApplyTemplate();
        scrollViewer.ApplyTemplate();
        scrollViewer.UpdateLayout();
        scrollBar.UpdateLayout();

        var verticalRoot = GetTemplatePart<Grid>(scrollBar, "VerticalRoot", "EtherScrollBar");
        var horizontalRoot = GetTemplatePart<Grid>(scrollBar, "HorizontalRoot", "EtherScrollBar");
        var verticalThumb = GetTemplatePart<Thumb>(scrollBar, "VerticalThumb", "EtherScrollBar");
        GetTemplatePart<Thumb>(scrollBar, "HorizontalThumb", "EtherScrollBar");
        var verticalSmallDecrease = GetTemplatePart<RepeatButton>(scrollBar, "VerticalSmallDecrease", "EtherScrollBar");
        var verticalSmallIncrease = GetTemplatePart<RepeatButton>(scrollBar, "VerticalSmallIncrease", "EtherScrollBar");
        verticalThumb.ApplyTemplate();
        verticalThumb.UpdateLayout();
        GetTemplatePart<Border>(verticalThumb, "ThumbFill", "EtherScrollBar");

        var implicitStyleApplied = scrollBar.Template is not null &&
            verticalRoot.Width == 6d &&
            horizontalRoot.Height == 6d;
        if (!implicitStyleApplied)
        {
            throw new InvalidOperationException("The implicit Ether ScrollBar style did not apply its 6 px vertical and horizontal templates.");
        }

        var viewerScrollBar = FindDescendant<ScrollBar>(scrollViewer);
        if (viewerScrollBar is null || viewerScrollBar.Template is null)
        {
            throw new InvalidOperationException("ScrollViewer did not receive an implicitly styled ScrollBar.");
        }

        var arrowsCollapsed = verticalSmallDecrease.Visibility == Visibility.Collapsed &&
            verticalSmallIncrease.Visibility == Visibility.Collapsed;
        if (!arrowsCollapsed)
        {
            throw new InvalidOperationException("Ether ScrollBar stepper arrows were visible.");
        }

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(scrollBar)
            ?? throw new InvalidOperationException("Implicit ScrollBar did not create an automation peer.");
        if (peer.GetName() != "Package scroll bar")
        {
            throw new InvalidOperationException("Implicit ScrollBar fixture automation name was not applied.");
        }

        var lightTemplateBrushColors = await GetScrollBarTemplateBrushColorsAsync(themeRoot, scrollBar, verticalThumb, ElementTheme.Light);
        var darkTemplateBrushColors = await GetScrollBarTemplateBrushColorsAsync(themeRoot, scrollBar, verticalThumb, ElementTheme.Dark);
        // Figma Light spec 62110:22970 uses Gray400 Default; Dark spec 62124:312 uses Gray600,
        // matching the scrollbar/background/default token already recorded in EtherColors.xaml.
        // This must diverge like every other control's Light/Dark template-brush check below; a
        // previous version of this assertion required them to match, which had been silently
        // encoding EtherScrollBar's Dark dictionary duplicating the Light Gray400/Gray500 values
        // instead of using Gray600/Gray700 as a passing "contract" rather than catching it.
        if (lightTemplateBrushColors.SequenceEqual(darkTemplateBrushColors, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("EtherScrollBar Light and Dark template brushes did not re-resolve to distinct values.");
        }

        return new ScrollBarVerification(
            implicitStyleApplied,
            true,
            true,
            verticalRoot.Width,
            verticalThumb.MinHeight,
            arrowsCollapsed,
            peer.GetName(),
            lightTemplateBrushColors,
            darkTemplateBrushColors);
    }

    private static async Task<string[]> GetScrollBarTemplateBrushColorsAsync(
        FrameworkElement themeRoot,
        ScrollBar scrollBar,
        Thumb verticalThumb,
        ElementTheme theme)
    {
        // Unlike every other control's theme-color helper, this used to also pin
        // scrollBar.RequestedTheme directly on the live element. That forces ActualTheme to
        // update immediately for this probe, but an explicit RequestedTheme is sticky: it stops
        // the element from inheriting themeRoot's theme afterward. Because this helper is called
        // Light then Dark, the live ScrollBarProof instance was left permanently pinned to Dark,
        // so the later screenshot capture (which only toggles themeRoot.RequestedTheme, like
        // every other control) rendered it as Dark for both the "light" and "dark" control PNGs.
        // Rely on inheritance plus the poll below, exactly like the other controls' helpers do.
        themeRoot.RequestedTheme = theme;
        await WaitForAppliedThemeAsync(themeRoot, theme);
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (scrollBar.ActualTheme != theme && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25);
        }

        verticalThumb.ApplyTemplate();
        scrollBar.UpdateLayout();
        var thumbFill = GetTemplatePart<Border>(verticalThumb, "ThumbFill", "EtherScrollBar");
        return new[]
        {
            GetSolidBrushColor(thumbFill.Background, "thumb", theme, "EtherScrollBar"),
        };
    }
}
