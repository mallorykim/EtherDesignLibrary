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

    private static async Task<SteeringBarVerification> VerifySteeringBarAsync(
        FrameworkElement themeRoot,
        EtherSteeringBar defaultSteeringBar,
        EtherSteeringBar steeringBar)
    {
        defaultSteeringBar.ApplyTemplate();
        steeringBar.ApplyTemplate();
        defaultSteeringBar.UpdateLayout();
        steeringBar.UpdateLayout();

        var defaultStyleResolved = defaultSteeringBar.Minimum == 0d &&
            defaultSteeringBar.Maximum == 100d &&
            defaultSteeringBar.Value == 0d &&
            defaultSteeringBar.ShowTitle &&
            defaultSteeringBar.ShowValue &&
            !defaultSteeringBar.UseSystemFocusVisuals &&
            defaultSteeringBar.IsTabStop &&
            defaultSteeringBar.Template is not null;
        if (!defaultStyleResolved)
        {
            throw new InvalidOperationException("The keyed EtherSteeringBar style did not apply its default range, labels, UseSystemFocusVisuals=False, IsTabStop, and template.");
        }

        var interactionSurface = GetTemplatePart<Grid>(steeringBar, "InteractionSurface", nameof(EtherSteeringBar));
        var fillBorder = GetTemplatePart<Border>(steeringBar, "FillBorder", nameof(EtherSteeringBar));
        GetTemplatePart<Grid>(steeringBar, "ThumbHost", nameof(EtherSteeringBar));
        var labelRow = GetTemplatePart<Grid>(steeringBar, "LabelRow", nameof(EtherSteeringBar));
        var titleText = GetTemplatePart<ContentPresenter>(steeringBar, "TitleText", nameof(EtherSteeringBar));
        var valueLabel = GetTemplatePart<ContentPresenter>(steeringBar, "ValueLabel", nameof(EtherSteeringBar));
        var trackBackground = GetTemplatePart<Border>(steeringBar, "TrackBackground", nameof(EtherSteeringBar));

        var templateParts = new[] { "InteractionSurface", "FillBorder", "ThumbHost", "LabelRow", "TitleText", "ValueLabel" };
        var labelStates = new List<string>();
        AssertSteeringBarLabelState(steeringBar, labelRow, titleText, valueLabel, true, true, "BothLabelsVisible", labelStates);
        AssertSteeringBarLabelState(steeringBar, labelRow, titleText, valueLabel, true, false, "TitleOnly", labelStates);
        AssertSteeringBarLabelState(steeringBar, labelRow, titleText, valueLabel, false, true, "ValueOnly", labelStates);
        AssertSteeringBarLabelState(steeringBar, labelRow, titleText, valueLabel, false, false, "LabelsHidden", labelStates);
        steeringBar.ShowTitle = true;
        steeringBar.ShowValue = true;

        steeringBar.Value = 65d;
        steeringBar.UpdateLayout();
        if (interactionSurface.ActualWidth <= 0d || fillBorder.ActualWidth <= 0d)
        {
            throw new InvalidOperationException("The steering-bar template did not receive a measurable track and fill layout.");
        }

        var fillRatio = fillBorder.ActualWidth / interactionSurface.ActualWidth;
        if (Math.Abs(fillRatio - 0.65d) > 0.02d)
        {
            throw new InvalidOperationException($"Expected a 65% steering-bar fill, but observed {fillRatio:P2}.");
        }

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(steeringBar)
            ?? throw new InvalidOperationException("EtherSteeringBar did not create an automation peer.");
        var rangeValue = peer.GetPattern(PatternInterface.RangeValue) as IRangeValueProvider
            ?? throw new InvalidOperationException("EtherSteeringBar automation peer GetPattern(RangeValue) did not return IRangeValueProvider.");
        if (peer is not IRangeValueProvider)
        {
            throw new InvalidOperationException("EtherSteeringBar automation peer did not expose IRangeValueProvider.");
        }

        var fallbackPeer = FrameworkElementAutomationPeer.CreatePeerForElement(defaultSteeringBar)
            ?? throw new InvalidOperationException("Default EtherSteeringBar did not create an automation peer.");

        if (peer.GetAutomationControlType() != Microsoft.UI.Xaml.Automation.Peers.AutomationControlType.Slider ||
            peer.GetClassName() != nameof(EtherSteeringBar) ||
            peer.GetName() != "Package steering bar" ||
            fallbackPeer.GetName() != "Default package steering bar" ||
            rangeValue.IsReadOnly ||
            rangeValue.SmallChange != 1d ||
            rangeValue.LargeChange != 10d ||
            rangeValue.Minimum != 0d ||
            rangeValue.Maximum != 100d ||
            rangeValue.Value != 65d)
        {
            throw new InvalidOperationException("EtherSteeringBar did not expose the required interactive Slider RangeValue automation contract.");
        }

        rangeValue.SetValue(70d);
        var setValueAccepted = steeringBar.Value == 70d;
        if (!setValueAccepted)
        {
            throw new InvalidOperationException("The interactive steering bar rejected a UI Automation SetValue request.");
        }

        steeringBar.Value = 64d;
        var observedAfterFirstChange = rangeValue.Value;
        steeringBar.Value = 65d;
        var observedAfterSecondChange = rangeValue.Value;
        var valueChangeExercised = observedAfterFirstChange == 64d && observedAfterSecondChange == 65d;
        if (!valueChangeExercised)
        {
            throw new InvalidOperationException("The RangeValue provider from GetPattern did not track owner Value assignments.");
        }

        steeringBar.PreviewStatus = SteeringBarPreviewStatus.Default;
        var previewStatusLocksAutomation = false;
        try
        {
            rangeValue.SetValue(12d);
        }
        catch (InvalidOperationException)
        {
            previewStatusLocksAutomation = steeringBar.Value == 65d && rangeValue.IsReadOnly;
        }

        if (!previewStatusLocksAutomation)
        {
            throw new InvalidOperationException("PreviewStatus did not lock the steering bar RangeValue provider.");
        }

        steeringBar.PreviewStatus = SteeringBarPreviewStatus.None;

        var lightGradient = await GetSteeringBarGradientAsync(themeRoot, steeringBar, fillBorder, ElementTheme.Light);
        var darkGradient = await GetSteeringBarGradientAsync(themeRoot, steeringBar, fillBorder, ElementTheme.Dark);
        var lightTemplateBrushColors = await GetSteeringBarTemplateBrushColorsAsync(
            themeRoot, steeringBar, trackBackground, titleText, valueLabel, ElementTheme.Light);
        var darkTemplateBrushColors = await GetSteeringBarTemplateBrushColorsAsync(
            themeRoot, steeringBar, trackBackground, titleText, valueLabel, ElementTheme.Dark);
        AssertSteeringBarGradient(lightGradient, "Light");
        AssertSteeringBarGradient(darkGradient, "Dark");
        if (lightTemplateBrushColors.SequenceEqual(darkTemplateBrushColors, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("EtherSteeringBar Light and Dark template brushes did not re-resolve to distinct values.");
        }

        return new SteeringBarVerification(
            defaultStyleResolved,
            defaultSteeringBar.UseSystemFocusVisuals,
            templateParts,
            labelStates.ToArray(),
            fillRatio,
            peer.GetName(),
            fallbackPeer.GetName(),
            peer.GetClassName(),
            peer.GetAutomationControlType().ToString(),
            rangeValue.IsReadOnly,
            rangeValue.SmallChange,
            rangeValue.LargeChange,
            rangeValue.Minimum,
            rangeValue.Maximum,
            rangeValue.Value,
            setValueAccepted,
            previewStatusLocksAutomation,
            valueChangeExercised,
            lightGradient,
            darkGradient,
            lightTemplateBrushColors,
            darkTemplateBrushColors);
    }

    private static void AssertSteeringBarLabelState(
        EtherSteeringBar steeringBar,
        FrameworkElement labelRow,
        FrameworkElement titleText,
        FrameworkElement valueLabel,
        bool showTitle,
        bool showValue,
        string expectedState,
        List<string> observedStates)
    {
        steeringBar.ShowTitle = showTitle;
        steeringBar.ShowValue = showValue;

        if (labelRow.Visibility != (showTitle || showValue ? Visibility.Visible : Visibility.Collapsed) ||
            titleText.Visibility != (showTitle ? Visibility.Visible : Visibility.Collapsed) ||
            valueLabel.Visibility != (showValue ? Visibility.Visible : Visibility.Collapsed))
        {
            throw new InvalidOperationException($"The '{expectedState}' LabelStates transition did not apply the expected visibility on EtherSteeringBar.");
        }

        observedStates.Add(expectedState);
    }

    private static async Task<string[]> GetSteeringBarGradientAsync(
        FrameworkElement themeRoot,
        EtherSteeringBar steeringBar,
        Border fill,
        ElementTheme theme)
    {
        themeRoot.RequestedTheme = theme;
        await WaitForAppliedThemeAsync(themeRoot, theme);
        steeringBar.UpdateLayout();

        if (fill.Background is not LinearGradientBrush brush)
        {
            throw new InvalidOperationException($"EtherSteeringBar fill did not resolve a LinearGradientBrush in {theme} theme.");
        }

        return brush.GradientStops.Select(stop => FormatColor(stop.Color)).ToArray();
    }

    private static void AssertSteeringBarGradient(IReadOnlyList<string> colors, string theme)
    {
        if (!colors.SequenceEqual(ExpectedProgressGradient, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"EtherSteeringBar {theme} gradient did not re-resolve to the expected component resources: {string.Join(", ", colors)}.");
        }
    }

    private static async Task<string[]> GetSteeringBarTemplateBrushColorsAsync(
        FrameworkElement themeRoot,
        EtherSteeringBar steeringBar,
        Border trackBackground,
        ContentPresenter titleText,
        ContentPresenter valueLabel,
        ElementTheme theme)
    {
        themeRoot.RequestedTheme = theme;
        await WaitForAppliedThemeAsync(themeRoot, theme);
        steeringBar.UpdateLayout();

        return new[]
        {
            GetSolidBrushColor(trackBackground.Background, "track", theme, nameof(EtherSteeringBar)),
            GetSolidBrushColor(titleText.Foreground, "title", theme, nameof(EtherSteeringBar)),
            GetSolidBrushColor(valueLabel.Foreground, "value", theme, nameof(EtherSteeringBar)),
        };
    }
}
