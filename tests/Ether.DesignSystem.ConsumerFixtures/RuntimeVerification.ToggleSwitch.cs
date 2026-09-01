using Ether.DesignSystem.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Ether.DesignSystem.ConsumerFixtures;

internal static partial class RuntimeVerification
{

    private static async Task<ToggleSwitchVerification> VerifyToggleSwitchAsync(
        FrameworkElement themeRoot,
        ToggleSwitch bareToggleSwitch,
        ToggleSwitch defaultToggleSwitch,
        ToggleSwitch toggleSwitch)
    {
        defaultToggleSwitch.ApplyTemplate();
        toggleSwitch.ApplyTemplate();
        bareToggleSwitch.ApplyTemplate();
        defaultToggleSwitch.UpdateLayout();
        toggleSwitch.UpdateLayout();
        bareToggleSwitch.UpdateLayout();

        if (!Application.Current.Resources.TryGetValue("EtherSwitch", out var keyedStyleObject) ||
            keyedStyleObject is not Style keyedStyle ||
            keyedStyle.TargetType != typeof(ToggleSwitch))
        {
            throw new InvalidOperationException("The keyed EtherSwitch style was not resolved from the Controls public merge graph.");
        }

        var keyedStyleResolved = defaultToggleSwitch.Style is not null &&
            ReferenceEquals(defaultToggleSwitch.Style, keyedStyle) &&
            ReferenceEquals(toggleSwitch.Style, keyedStyle) &&
            !defaultToggleSwitch.UseSystemFocusVisuals &&
            defaultToggleSwitch.Template is not null &&
            toggleSwitch.Template is not null &&
            TryGetNamedDescendant<Border>(defaultToggleSwitch, "KnobFrame");
        if (!keyedStyleResolved)
        {
            throw new InvalidOperationException("The keyed EtherSwitch style did not apply UseSystemFocusVisuals=False and EtherSwitchTemplate.");
        }

        var implicitStyleRejected = !TryGetNamedDescendant<Border>(bareToggleSwitch, "KnobFrame") &&
            !TryGetNamedDescendant<Rectangle>(bareToggleSwitch, "OnTrackBacking");
        if (!implicitStyleRejected)
        {
            throw new InvalidOperationException("A bare ToggleSwitch received EtherSwitch chrome; the style must stay keyed, not implicit.");
        }

        GetTemplatePart<Rectangle>(toggleSwitch, "OuterBorder", "EtherSwitch");
        GetTemplatePart<Rectangle>(toggleSwitch, "OnTrackBacking", "EtherSwitch");
        GetTemplatePart<Border>(toggleSwitch, "KnobFrame", "EtherSwitch");
        GetTemplatePart<Rectangle>(defaultToggleSwitch, "OuterBorder", "EtherSwitch");
        GetTemplatePart<Grid>(toggleSwitch, "SwitchAreaGrid", "EtherSwitch");

        var templateParts = new[] { "OuterBorder", "OnTrackBacking", "KnobFrame", "SwitchAreaGrid" };

        var toggleStates = new List<string>();
        defaultToggleSwitch.IsOn = false;
        defaultToggleSwitch.UpdateLayout();
        var defaultTrackOff = GetTemplatePart<Rectangle>(defaultToggleSwitch, "OuterBorder", "EtherSwitch");
        var defaultTrackOn = GetTemplatePart<Rectangle>(defaultToggleSwitch, "OnTrackBacking", "EtherSwitch");
        if (defaultTrackOff.Opacity != 1d || defaultTrackOn.Opacity != 0d)
        {
            throw new InvalidOperationException("EtherSwitch Off ToggleStates did not show OuterBorder and collapse OnTrackBacking.");
        }

        toggleStates.Add("Off");
        toggleSwitch.IsOn = true;
        toggleSwitch.UpdateLayout();
        await Task.Delay(150);
        var onTrackOff = GetTemplatePart<Rectangle>(toggleSwitch, "OuterBorder", "EtherSwitch");
        var onTrackOn = GetTemplatePart<Rectangle>(toggleSwitch, "OnTrackBacking", "EtherSwitch");
        if (onTrackOff.Opacity != 0d || onTrackOn.Opacity != 1d)
        {
            throw new InvalidOperationException("EtherSwitch On ToggleStates did not show OnTrackBacking and collapse OuterBorder.");
        }

        toggleStates.Add("On");

        toggleSwitch.IsEnabled = false;
        toggleSwitch.UpdateLayout();
        VisualStateManager.GoToState(toggleSwitch, "Disabled", false);
        var disabledTrack = GetTemplatePart<Grid>(toggleSwitch, "TrackVisuals", "EtherSwitch");
        var disabledTrackOpacityApplied = Math.Abs(disabledTrack.Opacity - 0.4d) < 0.01d;
        if (!disabledTrackOpacityApplied)
        {
            throw new InvalidOperationException("Disabled EtherSwitch did not fade TrackOn to 0.4 opacity.");
        }

        toggleSwitch.IsEnabled = true;
        toggleSwitch.IsOn = false;
        toggleSwitch.UpdateLayout();

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(toggleSwitch)
            ?? throw new InvalidOperationException("EtherSwitch-styled ToggleSwitch did not create an automation peer.");
        var defaultPeer = FrameworkElementAutomationPeer.CreatePeerForElement(defaultToggleSwitch)
            ?? throw new InvalidOperationException("Default EtherSwitch-styled ToggleSwitch did not create an automation peer.");
        if (peer.GetName() != "Package toggle switch" || defaultPeer.GetName() != "Default package toggle switch")
        {
            throw new InvalidOperationException("EtherSwitch fixture automation names were not applied.");
        }

        var lightTemplateBrushColors = await GetToggleSwitchTemplateBrushColorsAsync(themeRoot, toggleSwitch, onTrackOff, ElementTheme.Light);
        var darkTemplateBrushColors = await GetToggleSwitchTemplateBrushColorsAsync(themeRoot, toggleSwitch, onTrackOff, ElementTheme.Dark);
        if (lightTemplateBrushColors.SequenceEqual(darkTemplateBrushColors, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("EtherSwitch Light and Dark template brushes did not re-resolve to distinct values.");
        }

        return new ToggleSwitchVerification(
            keyedStyleResolved,
            implicitStyleRejected,
            defaultToggleSwitch.UseSystemFocusVisuals,
            templateParts,
            toggleStates.ToArray(),
            disabledTrackOpacityApplied,
            peer.GetName(),
            defaultPeer.GetName(),
            lightTemplateBrushColors,
            darkTemplateBrushColors);
    }

    private static async Task<string[]> GetToggleSwitchTemplateBrushColorsAsync(
        FrameworkElement themeRoot,
        ToggleSwitch toggleSwitch,
        Rectangle trackOff,
        ElementTheme theme)
    {
        themeRoot.RequestedTheme = theme;
        await WaitForAppliedThemeAsync(themeRoot, theme);
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (toggleSwitch.ActualTheme != theme && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25);
        }

        toggleSwitch.UpdateLayout();
        return new[]
        {
            GetSolidBrushColor(trackOff.Fill, "off track", theme, "EtherSwitch"),
        };
    }
}
