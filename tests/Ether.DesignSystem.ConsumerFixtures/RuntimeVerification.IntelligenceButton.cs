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

    private static async Task<IntelligenceButtonVerification> VerifyIntelligenceButtonAsync(
        FrameworkElement themeRoot,
        EtherIntelligenceButton defaultIntelligenceButton,
        EtherIntelligenceButton intelligenceButton)
    {
        defaultIntelligenceButton.ApplyTemplate();
        intelligenceButton.ApplyTemplate();
        defaultIntelligenceButton.UpdateLayout();
        intelligenceButton.UpdateLayout();

        if (defaultIntelligenceButton.FontSize != 14d ||
            defaultIntelligenceButton.UseSystemFocusVisuals ||
            defaultIntelligenceButton.Padding != new Thickness(8, 8, 12, 8) ||
            defaultIntelligenceButton.Template is null)
        {
            throw new InvalidOperationException("The keyed EtherIntelligenceButton style did not apply its default FontSize, padding, UseSystemFocusVisuals=False setter, and template.");
        }

        var background = GetTemplatePart<Border>(intelligenceButton, "Bg", nameof(EtherIntelligenceButton));
        var intelligenceBlue = GetTemplatePart<Border>(intelligenceButton, "BorderIntelligenceBlue", nameof(EtherIntelligenceButton));
        GetTemplatePart<Border>(intelligenceButton, "BorderIntelligenceGradient", nameof(EtherIntelligenceButton));
        var blueGlowOuter = GetTemplatePart<Border>(intelligenceButton, "BlueGlowOuter", nameof(EtherIntelligenceButton));
        var blueGlowMiddle = GetTemplatePart<Border>(intelligenceButton, "BlueGlowMiddle", nameof(EtherIntelligenceButton));
        var blueGlowCore = GetTemplatePart<Border>(intelligenceButton, "BlueGlowCore", nameof(EtherIntelligenceButton));
        var purpleGlow = GetTemplatePart<Border>(intelligenceButton, "PurpleGlow", nameof(EtherIntelligenceButton));
        GetTemplatePart<ContentPresenter>(intelligenceButton, "Cp", nameof(EtherIntelligenceButton));
        GetTemplatePart<Border>(intelligenceButton, "FocusRing", nameof(EtherIntelligenceButton));
        var templateParts = new[]
        {
            "Bg",
            "BorderIntelligenceBlue",
            "BorderIntelligenceGradient",
            "BlueGlowOuter",
            "BlueGlowMiddle",
            "BlueGlowCore",
            "PurpleGlow",
            "Cp",
            "FocusRing",
        };
        var commonStates = new List<string>();

        intelligenceButton.IsHitTestVisible = false;
        intelligenceButton.IsTabStop = false;

        VisualStateManager.GoToState(intelligenceButton, "Normal", false);
        intelligenceButton.UpdateLayout();
        if (intelligenceBlue.BorderBrush is not RadialGradientBrush)
        {
            throw new InvalidOperationException("The 'Normal' CommonStates transition did not keep the radial intelligence stroke on EtherIntelligenceButton.");
        }

        var normalGlow = GetSolidBrushColor(blueGlowCore.Background, "glow", themeRoot.ActualTheme, nameof(EtherIntelligenceButton));
        commonStates.Add("Normal");

        var lightTemplateBrushColors = await GetIntelligenceButtonForegroundBrushColorsAsync(
            themeRoot, intelligenceButton, ElementTheme.Light);
        var darkTemplateBrushColors = await GetIntelligenceButtonForegroundBrushColorsAsync(
            themeRoot, intelligenceButton, ElementTheme.Dark);
        if (lightTemplateBrushColors.SequenceEqual(darkTemplateBrushColors, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("EtherIntelligenceButton Light and Dark template brushes did not re-resolve to distinct values.");
        }

        VisualStateManager.GoToState(intelligenceButton, "PointerOver", false);
        intelligenceButton.UpdateLayout();
        if (intelligenceBlue.BorderBrush is not LinearGradientBrush)
        {
            throw new InvalidOperationException("The 'PointerOver' CommonStates transition did not apply the hover intelligence stroke on EtherIntelligenceButton.");
        }

        commonStates.Add("PointerOver");

        VisualStateManager.GoToState(intelligenceButton, "Pressed", false);
        intelligenceButton.UpdateLayout();
        var pressedGlow = GetSolidBrushColor(blueGlowCore.Background, "pressed glow", themeRoot.ActualTheme, nameof(EtherIntelligenceButton));
        if (pressedGlow == normalGlow)
        {
            throw new InvalidOperationException("The 'Pressed' CommonStates transition did not apply the pressed core glow on EtherIntelligenceButton.");
        }

        commonStates.Add("Pressed");

        VisualStateManager.GoToState(intelligenceButton, "Disabled", false);
        intelligenceButton.UpdateLayout();
        intelligenceButton.IsEnabled = false;
        intelligenceButton.UpdateLayout();
        if (Math.Abs(background.Opacity - 0.4) > 0.02 ||
            blueGlowOuter.Visibility != Visibility.Collapsed ||
            blueGlowMiddle.Visibility != Visibility.Collapsed ||
            blueGlowCore.Visibility != Visibility.Collapsed ||
            purpleGlow.Visibility != Visibility.Collapsed)
        {
            throw new InvalidOperationException("The 'Disabled' CommonStates transition did not apply 40% opacity and collapse glow layers on EtherIntelligenceButton.");
        }

        commonStates.Add("Disabled");

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(intelligenceButton)
            ?? throw new InvalidOperationException("EtherIntelligenceButton did not create an automation peer.");
        if (peer.GetName() != "Package intelligence button")
        {
            throw new InvalidOperationException("EtherIntelligenceButton did not expose the required automation name.");
        }

        return new IntelligenceButtonVerification(
            true,
            defaultIntelligenceButton.FontSize,
            defaultIntelligenceButton.UseSystemFocusVisuals,
            templateParts,
            commonStates.ToArray(),
            Math.Abs(background.Opacity - 0.4) <= 0.02,
            peer.GetName(),
            lightTemplateBrushColors,
            darkTemplateBrushColors);
    }

    private static async Task<string[]> GetIntelligenceButtonForegroundBrushColorsAsync(
        FrameworkElement themeRoot,
        EtherIntelligenceButton intelligenceButton,
        ElementTheme theme)
    {
        themeRoot.RequestedTheme = theme;
        await WaitForAppliedThemeAsync(themeRoot, theme);
        intelligenceButton.UpdateLayout();
        var content = GetTemplatePart<ContentPresenter>(intelligenceButton, "Cp", nameof(EtherIntelligenceButton));

        return new[]
        {
            GetSolidBrushColor(content.Foreground, "label", theme, nameof(EtherIntelligenceButton)),
        };
    }
}
