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
            TryGetNamedDescendant<Border>(defaultToggleSwitch, "KnobFill");
        if (!keyedStyleResolved)
        {
            throw new InvalidOperationException("The keyed EtherSwitch style did not apply UseSystemFocusVisuals=False and EtherSwitchTemplate.");
        }

        var implicitStyleRejected = !TryGetNamedDescendant<Border>(bareToggleSwitch, "KnobFill") &&
            !TryGetNamedDescendant<Border>(bareToggleSwitch, "TrackOn");
        if (!implicitStyleRejected)
        {
            throw new InvalidOperationException("A bare ToggleSwitch received EtherSwitch chrome; the style must stay keyed, not implicit.");
        }

        var trackOff = GetTemplatePart<Border>(toggleSwitch, "TrackOff", "EtherSwitch");
        GetTemplatePart<Border>(toggleSwitch, "TrackOn", "EtherSwitch");
        GetTemplatePart<Border>(toggleSwitch, "KnobFill", "EtherSwitch");
        GetTemplatePart<Border>(defaultToggleSwitch, "TrackOff", "EtherSwitch");
        var templateParts = new[] { "TrackOff", "TrackOn", "KnobFill" };

        var toggleStates = new List<string>();
        defaultToggleSwitch.IsOn = false;
        defaultToggleSwitch.UpdateLayout();
        var defaultTrackOff = GetTemplatePart<Border>(defaultToggleSwitch, "TrackOff", "EtherSwitch");
        var defaultTrackOn = GetTemplatePart<Border>(defaultToggleSwitch, "TrackOn", "EtherSwitch");
        if (defaultTrackOff.Visibility != Visibility.Visible || defaultTrackOn.Visibility != Visibility.Collapsed)
        {
            throw new InvalidOperationException("EtherSwitch Off ToggleStates did not show TrackOff and collapse TrackOn.");
        }

        toggleStates.Add("Off");
        toggleSwitch.IsOn = true;
        toggleSwitch.UpdateLayout();
        var onTrackOff = GetTemplatePart<Border>(toggleSwitch, "TrackOff", "EtherSwitch");
        var onTrackOn = GetTemplatePart<Border>(toggleSwitch, "TrackOn", "EtherSwitch");
        if (onTrackOff.Visibility != Visibility.Collapsed || onTrackOn.Visibility != Visibility.Visible)
        {
            throw new InvalidOperationException("EtherSwitch On ToggleStates did not show TrackOn and collapse TrackOff.");
        }

        toggleStates.Add("On");

        toggleSwitch.IsEnabled = false;
        toggleSwitch.UpdateLayout();
        VisualStateManager.GoToState(toggleSwitch, "Disabled", false);
        var disabledTrackOpacityApplied = Math.Abs(onTrackOff.Opacity - 0.4d) < 0.01d;
        if (!disabledTrackOpacityApplied)
        {
            throw new InvalidOperationException("Disabled EtherSwitch did not fade TrackOff to 0.4 opacity while keeping the knob opaque.");
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

        var lightTemplateBrushColors = await GetToggleSwitchTemplateBrushColorsAsync(themeRoot, toggleSwitch, trackOff, ElementTheme.Light);
        var darkTemplateBrushColors = await GetToggleSwitchTemplateBrushColorsAsync(themeRoot, toggleSwitch, trackOff, ElementTheme.Dark);
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
        Border trackOff,
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
            GetSolidBrushColor(trackOff.Background, "off track", theme, "EtherSwitch"),
        };
    }
}
