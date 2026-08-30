using Ether.DesignSystem.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Ether.DesignSystem.ConsumerFixtures;

internal static partial class RuntimeVerification
{

    private static async Task<ProgressBarVerification> VerifyProgressBarAsync(
        FrameworkElement themeRoot,
        EtherProgressBar defaultProgressBar,
        EtherProgressBar progressBar)
    {
        defaultProgressBar.ApplyTemplate();
        progressBar.ApplyTemplate();
        progressBar.UpdateLayout();

        if (defaultProgressBar.Minimum != 0d || defaultProgressBar.Maximum != 100d ||
            defaultProgressBar.Value != 0d || !defaultProgressBar.ShowTitle || !defaultProgressBar.ShowValue ||
            defaultProgressBar.IsTabStop || defaultProgressBar.UseSystemFocusVisuals ||
            defaultProgressBar.Template is null)
        {
            throw new InvalidOperationException("The keyed EtherProgressBar style did not apply its default range, labels, and template.");
        }

        var labelRow = GetTemplatePart<Grid>(progressBar, "LabelRow");
        var titleText = GetTemplatePart<ContentPresenter>(progressBar, "TitleText");
        var valueLabel = GetTemplatePart<ContentPresenter>(progressBar, "ValueLabel");
        var track = GetTemplatePart<Grid>(progressBar, "ProgressTrack");
        var fill = GetTemplatePart<Border>(progressBar, "ProgressFill");
        var trackBackground = GetTemplatePart<Border>(progressBar, "ProgressTrackBackground");
        var fillGrid = fill.Parent as Grid
            ?? throw new InvalidOperationException("EtherProgressBar fill was not hosted by its two-column template grid.");
        if (fillGrid.ColumnDefinitions.Count != 2)
        {
            throw new InvalidOperationException("EtherProgressBar did not create FillColumn and RestColumn template columns.");
        }

        var templateParts = new[] { "LayoutRoot", "FillColumn", "RestColumn", "LabelRow", "TitleText", "ValueLabel" };
        var labelStates = new List<string>();
        AssertLabelState(progressBar, labelRow, titleText, valueLabel, true, true, "BothLabelsVisible", labelStates);
        AssertLabelState(progressBar, labelRow, titleText, valueLabel, true, false, "TitleOnly", labelStates);
        AssertLabelState(progressBar, labelRow, titleText, valueLabel, false, true, "ValueOnly", labelStates);
        AssertLabelState(progressBar, labelRow, titleText, valueLabel, false, false, "LabelsHidden", labelStates);
        progressBar.ShowTitle = true;
        progressBar.ShowValue = true;

        progressBar.Value = 65d;
        progressBar.UpdateLayout();
        if (track.ActualWidth <= 0d || fill.ActualWidth <= 0d)
        {
            throw new InvalidOperationException("The progress-bar template did not receive a measurable track and fill layout.");
        }

        var fillRatio = fill.ActualWidth / track.ActualWidth;
        if (Math.Abs(fillRatio - 0.65d) > 0.02d || fillGrid.ColumnDefinitions[0].ActualWidth <= 0d || fillGrid.ColumnDefinitions[1].ActualWidth <= 0d)
        {
            throw new InvalidOperationException($"Expected a 65% progress fill, but observed {fillRatio:P2}.");
        }

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(progressBar)
            ?? throw new InvalidOperationException("EtherProgressBar did not create an automation peer.");
        var rangeValue = peer.GetPattern(PatternInterface.RangeValue) as IRangeValueProvider
            ?? throw new InvalidOperationException("EtherProgressBar automation peer GetPattern(RangeValue) did not return IRangeValueProvider.");
        if (peer is not IRangeValueProvider)
        {
            throw new InvalidOperationException("EtherProgressBar automation peer did not expose IRangeValueProvider.");
        }

        var fallbackPeer = FrameworkElementAutomationPeer.CreatePeerForElement(defaultProgressBar)
            ?? throw new InvalidOperationException("Default EtherProgressBar did not create an automation peer.");

        if (peer.GetAutomationControlType() != Microsoft.UI.Xaml.Automation.Peers.AutomationControlType.ProgressBar ||
            peer.GetClassName() != nameof(EtherProgressBar) ||
            peer.GetName() != "Package download progress" ||
            fallbackPeer.GetName() != "Default package progress" ||
            !rangeValue.IsReadOnly || !double.IsNaN(rangeValue.SmallChange) || !double.IsNaN(rangeValue.LargeChange) ||
            rangeValue.Minimum != 0d || rangeValue.Maximum != 100d || rangeValue.Value != 65d)
        {
            throw new InvalidOperationException("EtherProgressBar did not expose the required read-only ProgressBar RangeValue automation contract.");
        }

        var setValueRejected = false;
        try
        {
            rangeValue.SetValue(12d);
        }
        catch (InvalidOperationException)
        {
            setValueRejected = true;
        }

        if (!setValueRejected || progressBar.Value != 65d)
        {
            throw new InvalidOperationException("The read-only progress bar accepted a UI Automation SetValue request.");
        }

        // WinUI exposes no public in-process subscription for AutomationPeer's protected
        // RaisePropertyChangedEvent. Observe the strongest equivalent public contract instead:
        // each public RangeBase.ValueChanged notification must expose that exact new value through
        // the public IRangeValueProvider obtained from the control's automation peer.
        var observedValueChangedValues = new List<double>();
        var observedAutomationValues = new List<double>();
        RangeBaseValueChangedEventHandler onValueChanged = (_, args) =>
        {
            observedValueChangedValues.Add(args.NewValue);
            observedAutomationValues.Add(rangeValue.Value);
        };
        progressBar.ValueChanged += onValueChanged;
        bool valueChangeExercised;
        bool valuePropertyChangedSubscribed;
        try
        {
            progressBar.Value = 64d;
            var observedAfterFirstChange = rangeValue.Value;
            progressBar.Value = 65d;
            var observedAfterSecondChange = rangeValue.Value;
            valueChangeExercised = observedAfterFirstChange == 64d && observedAfterSecondChange == 65d;
            valuePropertyChangedSubscribed = observedValueChangedValues.SequenceEqual(new[] { 64d, 65d }) &&
                observedAutomationValues.SequenceEqual(new[] { 64d, 65d });
            if (!valueChangeExercised)
            {
                throw new InvalidOperationException("The RangeValue provider from GetPattern did not track owner Value assignments.");
            }

            if (!valuePropertyChangedSubscribed)
            {
                throw new InvalidOperationException("Public RangeBase.ValueChanged notifications did not expose the matching 64 then 65 values through IRangeValueProvider.");
            }
        }
        finally
        {
            progressBar.ValueChanged -= onValueChanged;
        }

        var lightFillColors = await GetProgressFillColorsAsync(themeRoot, progressBar, fill, ElementTheme.Light);
        var darkFillColors = await GetProgressFillColorsAsync(themeRoot, progressBar, fill, ElementTheme.Dark);
        var lightTemplateBrushColors = await GetProgressTemplateBrushColorsAsync(
            themeRoot, progressBar, trackBackground, titleText, valueLabel, ElementTheme.Light);
        var darkTemplateBrushColors = await GetProgressTemplateBrushColorsAsync(
            themeRoot, progressBar, trackBackground, titleText, valueLabel, ElementTheme.Dark);
        AssertFill(lightFillColors, ExpectedProgressBarLightFill, "Light");
        AssertFill(darkFillColors, ExpectedProgressBarDarkFill, "Dark");
        if (lightTemplateBrushColors.SequenceEqual(darkTemplateBrushColors, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("EtherProgressBar Light and Dark template brushes did not re-resolve to distinct values.");
        }

        return new ProgressBarVerification(
            templateParts,
            labelStates.ToArray(),
            fillRatio,
            peer.GetName(),
            fallbackPeer.GetName(),
            peer.GetClassName(),
            peer.GetAutomationControlType().ToString(),
            rangeValue.IsReadOnly,
            double.IsNaN(rangeValue.SmallChange),
            double.IsNaN(rangeValue.LargeChange),
            rangeValue.Minimum,
            rangeValue.Maximum,
            rangeValue.Value,
            setValueRejected,
            valueChangeExercised,
            valuePropertyChangedSubscribed,
            lightFillColors,
            darkFillColors,
            lightTemplateBrushColors,
            darkTemplateBrushColors);
    }

    private static void AssertLabelState(
        EtherProgressBar progressBar,
        FrameworkElement labelRow,
        FrameworkElement titleText,
        FrameworkElement valueLabel,
        bool showTitle,
        bool showValue,
        string expectedState,
        List<string> observedStates)
    {
        progressBar.ShowTitle = showTitle;
        progressBar.ShowValue = showValue;

        if (labelRow.Visibility != (showTitle || showValue ? Visibility.Visible : Visibility.Collapsed) ||
            titleText.Visibility != (showTitle ? Visibility.Visible : Visibility.Collapsed) ||
            valueLabel.Visibility != (showValue ? Visibility.Visible : Visibility.Collapsed))
        {
            throw new InvalidOperationException($"The '{expectedState}' LabelStates transition did not apply the expected visibility.");
        }

        observedStates.Add(expectedState);
    }

    private static async Task<string[]> GetProgressFillColorsAsync(
        FrameworkElement themeRoot,
        EtherProgressBar progressBar,
        Border fill,
        ElementTheme theme)
    {
        themeRoot.RequestedTheme = theme;
        await WaitForAppliedThemeAsync(themeRoot, theme);
        progressBar.UpdateLayout();

        return new[] { GetSolidBrushColor(fill.Background, "fill", theme) };
    }

    private static void AssertFill(string[] colors, string expected, string theme)
    {
        if (colors.Length != 1 || !string.Equals(colors[0], expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"EtherProgressBar {theme} fill did not re-resolve to {expected}: {string.Join(", ", colors)}.");
        }
    }

    private static async Task<string[]> GetProgressTemplateBrushColorsAsync(
        FrameworkElement themeRoot,
        EtherProgressBar progressBar,
        Border trackBackground,
        ContentPresenter titleText,
        ContentPresenter valueLabel,
        ElementTheme theme)
    {
        themeRoot.RequestedTheme = theme;
        await WaitForAppliedThemeAsync(themeRoot, theme);
        progressBar.UpdateLayout();

        return new[]
        {
            GetSolidBrushColor(trackBackground.Background, "track", theme),
            GetSolidBrushColor(titleText.Foreground, "title", theme),
            GetSolidBrushColor(valueLabel.Foreground, "value", theme),
        };
    }
}
