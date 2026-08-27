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

    private static async Task<ButtonVerification> VerifyButtonAsync(
        FrameworkElement themeRoot,
        EtherButton defaultButton,
        EtherButton button,
        EtherButton secondaryButton)
    {
        defaultButton.ApplyTemplate();
        button.ApplyTemplate();
        secondaryButton.ApplyTemplate();
        defaultButton.UpdateLayout();
        button.UpdateLayout();
        secondaryButton.UpdateLayout();

        if (defaultButton.MinHeight != 40d || defaultButton.FontSize != 14d || defaultButton.Template is null)
        {
            throw new InvalidOperationException("The keyed EtherButton style did not apply its default Primary medium setters and template.");
        }

        var rightIcon = GetTemplatePart<ContentPresenter>(button, "RightIcon", nameof(EtherButton));
        var secondaryBackground = GetTemplatePart<Border>(secondaryButton, "Bg", nameof(EtherButton));
        var templateParts = new[] { "RightIcon" };
        var rightIconStates = new List<string>();

        button.RightIcon = new FontIcon { Glyph = "\uE72A", FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 10 };
        button.UpdateLayout();
        if (rightIcon.Visibility != Visibility.Visible)
        {
            throw new InvalidOperationException("The 'RightIconVisible' RightIconStates transition did not show the trailing icon slot.");
        }

        rightIconStates.Add("RightIconVisible");

        button.RightIcon = null;
        button.UpdateLayout();
        if (rightIcon.Visibility != Visibility.Collapsed)
        {
            throw new InvalidOperationException("The 'RightIconCollapsed' RightIconStates transition did not collapse the trailing icon slot.");
        }

        rightIconStates.Add("RightIconCollapsed");

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button)
            ?? throw new InvalidOperationException("EtherButton did not create an automation peer.");
        if (peer.GetName() != "Package button")
        {
            throw new InvalidOperationException("EtherButton did not expose the required automation name.");
        }

        var lightTemplateBrushColors = await GetButtonTemplateBrushColorsAsync(
            themeRoot, secondaryButton, secondaryBackground, ElementTheme.Light);
        var darkTemplateBrushColors = await GetButtonTemplateBrushColorsAsync(
            themeRoot, secondaryButton, secondaryBackground, ElementTheme.Dark);
        if (lightTemplateBrushColors.SequenceEqual(darkTemplateBrushColors, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("EtherButton Light and Dark template brushes did not re-resolve to distinct values.");
        }

        return new ButtonVerification(
            true,
            defaultButton.MinHeight,
            defaultButton.FontSize,
            templateParts,
            rightIconStates.ToArray(),
            rightIcon.Visibility == Visibility.Collapsed,
            peer.GetName(),
            lightTemplateBrushColors,
            darkTemplateBrushColors);
    }

    private static async Task<string[]> GetButtonTemplateBrushColorsAsync(
        FrameworkElement themeRoot,
        EtherButton button,
        Border background,
        ElementTheme theme)
    {
        themeRoot.RequestedTheme = theme;
        await WaitForAppliedThemeAsync(themeRoot, theme);
        button.UpdateLayout();

        return new[]
        {
            GetSolidBrushColor(background.Background, "secondary background", theme, nameof(EtherButton)),
            GetSolidBrushColor(background.BorderBrush, "secondary border", theme, nameof(EtherButton)),
        };
    }
}
