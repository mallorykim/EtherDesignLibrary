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

    private static async Task<InputVerification> VerifyInputAsync(
        FrameworkElement themeRoot,
        EtherInput defaultInput,
        EtherInput input)
    {
        defaultInput.ApplyTemplate();
        input.ApplyTemplate();
        defaultInput.UpdateLayout();
        input.UpdateLayout();

        // The proof no longer overrides alignment/width, so the shipped width contract is directly
        // assertable on this instance: no MinWidth floor (0), Left alignment from the style, and a
        // 280 px default width produced by MeasureOverride while Width is Auto (DesiredSize.Width is
        // the measured output, independent of arrange timing).
        if (defaultInput.MinWidth != 0d
            || defaultInput.HorizontalAlignment != HorizontalAlignment.Left
            || Math.Abs(defaultInput.DesiredSize.Width - 280d) > 0.5
            || defaultInput.FontSize != 14d
            || defaultInput.UseSystemFocusVisuals
            || defaultInput.Template is null)
        {
            throw new InvalidOperationException(
                $"The keyed EtherInput style did not apply its default width contract (no MinWidth floor, 280 px default, Left): " +
                $"MinWidth={defaultInput.MinWidth}, DesiredWidth={defaultInput.DesiredSize.Width}, HorizontalAlignment={defaultInput.HorizontalAlignment}.");
        }

        var layoutRoot = GetTemplatePart<Grid>(input, "LayoutRoot", nameof(EtherInput));
        var borderElement = GetTemplatePart<Border>(input, "BorderElement", nameof(EtherInput));
        var placeholder = GetTemplatePart<TextBlock>(input, "PlaceholderTextContentPresenter", nameof(EtherInput));
        GetTemplatePart<ScrollViewer>(input, "ContentElement", nameof(EtherInput));
        var templateParts = new[] { "LayoutRoot", "BorderElement", "PlaceholderTextContentPresenter", "ContentElement" };
        var commonStates = new List<string>();

        input.IsHitTestVisible = false;
        input.IsTabStop = false;

        VisualStateManager.GoToState(input, "Normal", false);
        input.UpdateLayout();
        var normalFill = GetSolidBrushColor(borderElement.Background, "fill", themeRoot.ActualTheme, nameof(EtherInput));
        var normalBorder = GetSolidBrushColor(borderElement.BorderBrush, "border", themeRoot.ActualTheme, nameof(EtherInput));
        commonStates.Add("Normal");

        var lightTemplateBrushColors = await GetInputPlaceholderBrushColorsAsync(
            themeRoot, input, placeholder, ElementTheme.Light);
        var darkTemplateBrushColors = await GetInputPlaceholderBrushColorsAsync(
            themeRoot, input, placeholder, ElementTheme.Dark);
        if (lightTemplateBrushColors.SequenceEqual(darkTemplateBrushColors, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("EtherInput Light and Dark template brushes did not re-resolve to distinct values.");
        }

        VisualStateManager.GoToState(input, "PointerOver", false);
        input.UpdateLayout();
        var hoverFill = GetSolidBrushColor(borderElement.Background, "hover fill", themeRoot.ActualTheme, nameof(EtherInput));
        if (hoverFill == normalFill)
        {
            throw new InvalidOperationException("The 'PointerOver' CommonStates transition did not apply the hover fill on EtherInput.");
        }

        commonStates.Add("PointerOver");

        VisualStateManager.GoToState(input, "Focused", false);
        input.UpdateLayout();
        var focusedBorder = GetSolidBrushColor(borderElement.BorderBrush, "focus border", themeRoot.ActualTheme, nameof(EtherInput));
        if (focusedBorder == normalBorder)
        {
            throw new InvalidOperationException("The 'Focused' CommonStates transition did not apply the focus border on EtherInput.");
        }

        commonStates.Add("Focused");

        input.IsEnabled = false;
        VisualStateManager.GoToState(input, "Disabled", false);
        input.UpdateLayout();
        if (layoutRoot.Opacity != 0.5)
        {
            throw new InvalidOperationException("The 'Disabled' CommonStates transition did not apply 50% opacity on EtherInput.");
        }

        commonStates.Add("Disabled");

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(input)
            ?? throw new InvalidOperationException("EtherInput did not create an automation peer.");
        if (peer.GetName() != "Package input")
        {
            throw new InvalidOperationException("EtherInput did not expose the required automation name.");
        }

        return new InputVerification(
            true,
            defaultInput.MinWidth,
            defaultInput.FontSize,
            defaultInput.UseSystemFocusVisuals,
            templateParts,
            commonStates.ToArray(),
            layoutRoot.Opacity == 0.5,
            peer.GetName(),
            lightTemplateBrushColors,
            darkTemplateBrushColors);
    }

    private static async Task<string[]> GetInputPlaceholderBrushColorsAsync(
        FrameworkElement themeRoot,
        EtherInput input,
        TextBlock placeholder,
        ElementTheme theme)
    {
        themeRoot.RequestedTheme = theme;
        await WaitForAppliedThemeAsync(themeRoot, theme);
        input.UpdateLayout();

        return new[]
        {
            GetSolidBrushColor(placeholder.Foreground, "placeholder", theme, nameof(EtherInput)),
        };
    }
}
