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
            !defaultSlider.UseSystemFocusVisuals &&
            defaultSlider.IsTabStop &&
            defaultSlider.Template is not null;
        if (!defaultStyleResolved)
        {
            throw new InvalidOperationException("The keyed EtherSlider style did not apply its default range, change steps, UseSystemFocusVisuals=False, IsTabStop, and template.");
        }

        var valueText = GetTemplatePart<TextBlock>(slider, "ValueText", nameof(EtherSlider));
        var barCanvas = GetTemplatePart<Canvas>(slider, "BarCanvas", nameof(EtherSlider));
        GetTemplatePart<Microsoft.UI.Xaml.Shapes.Rectangle>(slider, "Knob", nameof(EtherSlider));
        var templateParts = new[] { "ValueText", "BarCanvas", "Knob" };

        slider.Value = 65d;
        slider.UpdateLayout();
        var highlightedBarCount = CountHighlightedBars(barCanvas);
        var fillRatio = highlightedBarCount / 63d;
        if (Math.Abs(fillRatio - 0.65d) > 0.02d || highlightedBarCount <= 0)
        {
            throw new InvalidOperationException($"Expected a 65% slider fill across 63 bars, but observed {highlightedBarCount} highlighted bars ({fillRatio:P2}).");
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
        catch (InvalidOperationException)
        {
            disabledLocksAutomation = slider.Value == 65d && rangeValue.IsReadOnly;
        }

        if (!disabledLocksAutomation)
        {
            throw new InvalidOperationException("Disabled EtherSlider did not lock the RangeValue provider.");
        }

        var disabledOpacityApplied = Math.Abs(barCanvas.Opacity - 0.4d) < 0.001d &&
            Math.Abs(valueText.Opacity - 0.4d) < 0.001d;
        if (!disabledOpacityApplied)
        {
            throw new InvalidOperationException("Disabled EtherSlider did not dim its ticks and value label.");
        }

        slider.IsEnabled = true;
        if (Math.Abs(barCanvas.Opacity - 1d) > 0.001d || Math.Abs(valueText.Opacity - 1d) > 0.001d)
        {
            throw new InvalidOperationException("Re-enabled EtherSlider did not restore its ticks and value label opacity.");
        }

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
            lightTemplateBrushColors,
            darkTemplateBrushColors);
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
