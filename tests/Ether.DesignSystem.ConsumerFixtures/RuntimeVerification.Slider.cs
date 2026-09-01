using Ether.DesignSystem.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace Ether.DesignSystem.ConsumerFixtures;

internal static partial class RuntimeVerification
{

    private static async Task<SliderVerification> VerifySliderAsync(
        FrameworkElement themeRoot,
        EtherSlider defaultSlider,
        EtherSlider slider)
    {
        defaultSlider.ApplyTemplate();
        slider.ApplyTemplate();
        defaultSlider.UpdateLayout();
        slider.UpdateLayout();

        var defaultStyleResolved = defaultSlider.Minimum == 0d &&
            defaultSlider.Maximum == 100d &&
            defaultSlider.Value == 50d &&
            defaultSlider.SmallChange == 1d &&
            defaultSlider.LargeChange == 10d &&
            defaultSlider.ShowTitle &&
            defaultSlider.ShowLabels &&
            defaultSlider.HorizontalAlignment == HorizontalAlignment.Stretch &&
            !defaultSlider.UseSystemFocusVisuals &&
            defaultSlider.IsTabStop &&
            defaultSlider.Template is not null;
        if (!defaultStyleResolved)
        {
            throw new InvalidOperationException("The keyed EtherSlider style did not apply its default range, chrome, Stretch alignment, change steps, UseSystemFocusVisuals=False, IsTabStop, and template.");
        }

        // EtherSlider.MaxLabelCount, MinBarCount, and MaxBarCount are private
        // implementation details by design - not part of the public contract.
        // Rather than reading them, this fixture drives the rendered behavior
        // they govern (the tick-label cap and the even tick-bar floor/ceiling)
        // through isolated probe instances hosted in an unconstrained scratch
        // Canvas, and checks the outcome against the same values the control's
        // public XML docs describe (see the EtherSlider <remarks> and the
        // <see cref="EtherSlider.Labels"/> doc comment).
        const int ExpectedMaxLabelCount = 11;
        const int ExpectedMinBarCount = 8;
        const int ExpectedMaxBarCount = 512;

        var overflowLabels = Enumerable.Range(0, ExpectedMaxLabelCount * 2).Select(i => i.ToString());
        var renderedLabels = GetRenderedLabels(themeRoot, overflowLabels);
        if (renderedLabels.Length != ExpectedMaxLabelCount ||
            renderedLabels[0] != "0" ||
            renderedLabels[^1] != (ExpectedMaxLabelCount - 1).ToString())
        {
            throw new InvalidOperationException($"Expected EtherSlider to render at most {ExpectedMaxLabelCount} tick labels ('0'..'{ExpectedMaxLabelCount - 1}') when given {ExpectedMaxLabelCount * 2} Labels, but observed [{string.Join(", ", renderedLabels)}].");
        }

        var narrowBarCount = GetRenderedBarCount(themeRoot, width: 10d);
        if (narrowBarCount != ExpectedMinBarCount)
        {
            throw new InvalidOperationException($"Expected EtherSlider to floor its even tick-bar count at {ExpectedMinBarCount} for a narrow track, but observed {narrowBarCount}.");
        }

        var wideBarCount = GetRenderedBarCount(themeRoot, width: 4000d);
        if (wideBarCount != ExpectedMaxBarCount)
        {
            throw new InvalidOperationException($"Expected EtherSlider to ceiling its even tick-bar count at {ExpectedMaxBarCount} for a wide track, but observed {wideBarCount}.");
        }

        var valueText = GetTemplatePart<TextBlock>(slider, "ValueText", nameof(EtherSlider));
        var barCanvas = GetTemplatePart<Canvas>(slider, "BarCanvas", nameof(EtherSlider));
        var labelRow = GetTemplatePart<Grid>(slider, "LabelRow", nameof(EtherSlider));
        GetTemplatePart<Microsoft.UI.Xaml.Shapes.Rectangle>(slider, "Knob", nameof(EtherSlider));
        var templateParts = new[] { "ValueText", "BarCanvas", "Knob", "LabelRow" };

        var visibleLabels = labelRow.Children.OfType<TextBlock>().Where(label => label.Visibility == Visibility.Visible).ToArray();
        if (visibleLabels.Length != 6 ||
            visibleLabels[0].Text != "0" ||
            visibleLabels[1].Text != "20" ||
            visibleLabels[5].Text != "100")
        {
            throw new InvalidOperationException("EtherSlider did not generate even auto tick labels from Minimum–Maximum (0, 20, …, 100).");
        }

        slider.Value = 65d;
        slider.UpdateLayout();
        var totalBarCount = barCanvas.Children.OfType<Microsoft.UI.Xaml.Shapes.Rectangle>().Count(rectangle => rectangle.Height == 40d);
        if (totalBarCount < ExpectedMinBarCount || totalBarCount % 2 != 0)
        {
            throw new InvalidOperationException($"Expected an even tick count of at least {ExpectedMinBarCount}, but observed {totalBarCount}.");
        }

        var highlightedBarCount = CountHighlightedBars(barCanvas);
        var expectedHighlighted = (int)Math.Round(0.65d * totalBarCount);
        var fillRatio = highlightedBarCount / (double)totalBarCount;
        if (highlightedBarCount != expectedHighlighted || highlightedBarCount <= 0)
        {
            throw new InvalidOperationException($"Expected {expectedHighlighted} highlighted bars of {totalBarCount} even ticks (65%), but observed {highlightedBarCount} ({fillRatio:P2}).");
        }

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(slider)
            ?? throw new InvalidOperationException("EtherSlider did not create an automation peer.");
        var rangeValue = peer.GetPattern(PatternInterface.RangeValue) as IRangeValueProvider
            ?? throw new InvalidOperationException("EtherSlider automation peer GetPattern(RangeValue) did not return IRangeValueProvider.");
        if (peer is not IRangeValueProvider)
        {
            throw new InvalidOperationException("EtherSlider automation peer did not expose IRangeValueProvider.");
        }

        var defaultPeer = FrameworkElementAutomationPeer.CreatePeerForElement(defaultSlider)
            ?? throw new InvalidOperationException("Default EtherSlider did not create an automation peer.");

        if (peer.GetAutomationControlType() != Microsoft.UI.Xaml.Automation.Peers.AutomationControlType.Slider ||
            peer.GetClassName() != nameof(EtherSlider) ||
            peer.GetName() != "Package slider" ||
            defaultPeer.GetName() != "Default package slider" ||
            rangeValue.IsReadOnly ||
            rangeValue.SmallChange != 1d ||
            rangeValue.LargeChange != 10d ||
            rangeValue.Minimum != 0d ||
            rangeValue.Maximum != 100d ||
            rangeValue.Value != 65d)
        {
            throw new InvalidOperationException("EtherSlider did not expose the required interactive Slider RangeValue automation contract.");
        }

        rangeValue.SetValue(70d);
        var setValueAccepted = slider.Value == 70d;
        if (!setValueAccepted)
        {
            throw new InvalidOperationException("The interactive slider rejected a UI Automation SetValue request.");
        }

        slider.Value = 64d;
        var observedAfterFirstChange = rangeValue.Value;
        slider.Value = 65d;
        var observedAfterSecondChange = rangeValue.Value;
        var valueChangeExercised = observedAfterFirstChange == 64d && observedAfterSecondChange == 65d;
        if (!valueChangeExercised)
        {
            throw new InvalidOperationException("The RangeValue provider from GetPattern did not track owner Value assignments.");
        }

        var formattedValue = slider.FormatValue(slider.Value);
        if (formattedValue != "65")
        {
            throw new InvalidOperationException($"EtherSlider.FormatValue did not return the expected public-API string. Observed '{formattedValue}'.");
        }

        slider.IsEnabled = false;
        var disabledLocksAutomation = false;
        try
        {
            rangeValue.SetValue(12d);
        }
        catch (Exception ex) when (ex is InvalidOperationException or Microsoft.UI.Xaml.Automation.ElementNotEnabledException)
        {
            disabledLocksAutomation = slider.Value == 65d && rangeValue.IsReadOnly;
        }

        if (!disabledLocksAutomation)
        {
            throw new InvalidOperationException("Disabled EtherSlider did not lock the RangeValue provider.");
        }

        var disabledOpacityApplied = Math.Abs(barCanvas.Opacity - 0.4d) < 0.001d &&
            Math.Abs(valueText.Opacity - 0.4d) < 0.001d &&
            Math.Abs(labelRow.Opacity - 0.4d) < 0.001d;
        if (!disabledOpacityApplied)
        {
            throw new InvalidOperationException("Disabled EtherSlider did not dim its ticks and value label.");
        }

        slider.IsEnabled = true;
        if (Math.Abs(barCanvas.Opacity - 1d) > 0.001d ||
            Math.Abs(valueText.Opacity - 1d) > 0.001d ||
            Math.Abs(labelRow.Opacity - 1d) > 0.001d)
        {
            throw new InvalidOperationException("Re-enabled EtherSlider did not restore its ticks and value label opacity.");
        }

        var setValueNoOpAccepted = false;
        try
        {
            rangeValue.SetValue(65.4d);
            setValueNoOpAccepted = slider.Value == 65d;
        }
        catch (Exception ex) when (ex is InvalidOperationException or Microsoft.UI.Xaml.Automation.ElementNotEnabledException)
        {
            setValueNoOpAccepted = false;
        }

        if (!setValueNoOpAccepted)
        {
            throw new InvalidOperationException("Enabled EtherSlider rejected a no-op RangeValue SetValue after integer rounding.");
        }

        slider.Labels!.Clear();
        foreach (var name in new[] { "Off", "Low", "Mid", "High", "Max" })
            slider.Labels.Add(name);
        slider.UpdateLayout();
        var namedLabels = labelRow.Children.OfType<TextBlock>().Where(label => label.Visibility == Visibility.Visible).ToArray();
        var namedLabelsApplied = namedLabels.Length == 5 &&
            namedLabels[0].Text == "Off" &&
            namedLabels[2].Text == "Mid" &&
            namedLabels[4].Text == "Max";
        if (!namedLabelsApplied)
        {
            throw new InvalidOperationException("EtherSlider did not render named Labels content (Off–Max).");
        }

        slider.Value = 47d;
        slider.SnapToStops = true;
        var snapCoerced = slider.Value == 50d;
        if (!snapCoerced)
        {
            throw new InvalidOperationException($"Enabling SnapToStops with five named labels should coerce 47 to 50. Observed {slider.Value}.");
        }

        var movedNext = slider.MoveToNextStop() && slider.Value == 75d;
        var movedPrevious = slider.MoveToPreviousStop() && slider.Value == 50d;
        var moveToStopExercised = movedNext && movedPrevious;
        if (!moveToStopExercised)
        {
            throw new InvalidOperationException($"EtherSlider stop stepping failed. Observed {slider.Value} after MoveToNextStop/MoveToPreviousStop.");
        }

        slider.SnapToStops = false;
        slider.Stops = new DoubleCollection { 0, 10, 50, 90, 100 };
        slider.Value = 47d;
        slider.SnapToStops = true;
        if (slider.Value != 50d)
        {
            throw new InvalidOperationException($"Explicit Stops with SnapToStops should coerce 47 to 50. Observed {slider.Value}.");
        }

        slider.SnapToStops = false;
        slider.Stops = null;
        slider.Labels!.Clear();
        slider.Value = 65d;
        slider.UpdateLayout();

        var lightTemplateBrushColors = await GetSliderTemplateBrushColorsAsync(themeRoot, slider, valueText, barCanvas, ElementTheme.Light);
        var darkTemplateBrushColors = await GetSliderTemplateBrushColorsAsync(themeRoot, slider, valueText, barCanvas, ElementTheme.Dark);
        if (lightTemplateBrushColors.SequenceEqual(darkTemplateBrushColors, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("EtherSlider Light and Dark template brushes did not re-resolve to distinct values.");
        }

        return new SliderVerification(
            defaultStyleResolved,
            defaultSlider.UseSystemFocusVisuals,
            templateParts,
            fillRatio,
            highlightedBarCount,
            peer.GetName(),
            defaultPeer.GetName(),
            peer.GetClassName(),
            peer.GetAutomationControlType().ToString(),
            rangeValue.IsReadOnly,
            rangeValue.SmallChange,
            rangeValue.LargeChange,
            rangeValue.Minimum,
            rangeValue.Maximum,
            rangeValue.Value,
            setValueAccepted,
            disabledLocksAutomation,
            disabledOpacityApplied,
            valueChangeExercised,
            formattedValue,
            setValueNoOpAccepted,
            namedLabelsApplied,
            snapCoerced,
            moveToStopExercised,
            lightTemplateBrushColors,
            darkTemplateBrushColors);
    }

    /// <summary>
    /// Renders a disposable, isolated <see cref="EtherSlider"/> at the given
    /// pixel width and returns how many even tick bars it actually produced.
    /// Used to observe the floor/ceiling that EtherSlider's private
    /// MinBarCount/MaxBarCount constants enforce, without reading those
    /// constants.
    /// </summary>
    private static int GetRenderedBarCount(FrameworkElement themeRoot, double width)
        => MeasureIsolatedSlider(
            themeRoot,
            probe => probe.Width = width,
            probe =>
            {
                var barCanvas = GetTemplatePart<Canvas>(probe, "BarCanvas", nameof(EtherSlider));
                return barCanvas.Children.OfType<Microsoft.UI.Xaml.Shapes.Rectangle>().Count(rectangle => rectangle.Height == 40d);
            });

    /// <summary>
    /// Renders a disposable, isolated <see cref="EtherSlider"/> with the given
    /// candidate tick labels and returns the text of every label that actually
    /// rendered visible. Used to observe the cap that EtherSlider's private
    /// MaxLabelCount constant enforces, without reading that constant.
    /// </summary>
    private static string[] GetRenderedLabels(FrameworkElement themeRoot, IEnumerable<string> labels)
        => MeasureIsolatedSlider(
            themeRoot,
            probe =>
            {
                probe.Width = 600d;
                probe.Labels!.Clear();
                foreach (var label in labels)
                    probe.Labels.Add(label);
            },
            probe =>
            {
                var labelRow = GetTemplatePart<Grid>(probe, "LabelRow", nameof(EtherSlider));
                return labelRow.Children.OfType<TextBlock>()
                    .Where(label => label.Visibility == Visibility.Visible)
                    .Select(label => label.Text)
                    .ToArray();
            });

    /// <summary>
    /// Hosts a brand-new <see cref="EtherSlider"/> inside an invisible, non-hit-testable
    /// scratch <see cref="Canvas"/> attached to <paramref name="themeRoot"/>, configures
    /// and lays it out, lets <paramref name="observe"/> read its rendered public state,
    /// then tears the scratch container back down. A Canvas always measures its children
    /// at infinite available size regardless of the Canvas's own size or any ancestor's
    /// viewport (including the fixture window's ScrollViewer), so an explicit Width set on
    /// the probe is exactly what it renders at - independent of the live window's real size.
    /// This keeps the probe from disturbing the shared, already-verified slider instances
    /// or anything else in the fixture's visual tree.
    /// </summary>
    private static T MeasureIsolatedSlider<T>(FrameworkElement themeRoot, Action<EtherSlider> configure, Func<EtherSlider, T> observe)
    {
        if (themeRoot is not Panel hostPanel)
        {
            throw new InvalidOperationException("EtherSlider boundary probe requires a panel-based theme root to host an isolated scratch container.");
        }

        var scratch = new Canvas { Opacity = 0d, IsHitTestVisible = false };
        hostPanel.Children.Add(scratch);
        try
        {
            var probe = new EtherSlider();
            configure(probe);
            scratch.Children.Add(probe);
            probe.ApplyTemplate();
            probe.UpdateLayout();
            return observe(probe);
        }
        finally
        {
            scratch.Children.Clear();
            hostPanel.Children.Remove(scratch);
        }
    }

    private static int CountHighlightedBars(Canvas canvas)
    {
        var knob = canvas.Children.OfType<Microsoft.UI.Xaml.Shapes.Rectangle>().FirstOrDefault(rectangle => rectangle.Height == 45d)
            ?? throw new InvalidOperationException("EtherSlider BarCanvas did not contain a knob rectangle.");
        var knobLeft = Canvas.GetLeft(knob);
        return canvas.Children.OfType<Microsoft.UI.Xaml.Shapes.Rectangle>().Count(rectangle => rectangle.Height == 40d && Canvas.GetLeft(rectangle) < knobLeft);
    }

    private static async Task<string[]> GetSliderTemplateBrushColorsAsync(
        FrameworkElement themeRoot,
        EtherSlider slider,
        TextBlock valueText,
        Canvas barCanvas,
        ElementTheme theme)
    {
        themeRoot.RequestedTheme = theme;
        await WaitForAppliedThemeAsync(themeRoot, theme);
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (slider.ActualTheme != theme && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25);
        }

        slider.UpdateLayout();
        var inactiveBar = barCanvas.Children.OfType<Microsoft.UI.Xaml.Shapes.Rectangle>().LastOrDefault(rectangle => rectangle.Height == 40d)
            ?? throw new InvalidOperationException($"EtherSlider did not generate an inactive bar in {theme} theme.");

        return new[]
        {
            GetSolidBrushColor(inactiveBar.Fill, "inactive bar", theme, nameof(EtherSlider)),
            GetSolidBrushColor(valueText.Foreground, "value", theme, nameof(EtherSlider)),
        };
    }
}
