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

        if (defaultButton.MinWidth != 108d || defaultButton.MinHeight != 46d || defaultButton.FontSize != 14d || defaultButton.Template is null)
        {
            throw new InvalidOperationException("The keyed EtherButton style did not apply its default Figma dimensions, typography, and template.");
        }

        // Same-size variants must render at the same height regardless of border thickness:
        // MinHeight, vertical padding, and font size all contribute to the laid-out height.
        if (secondaryButton.MinHeight != defaultButton.MinHeight ||
            secondaryButton.Padding.Top != defaultButton.Padding.Top ||
            secondaryButton.Padding.Bottom != defaultButton.Padding.Bottom ||
            secondaryButton.FontSize != defaultButton.FontSize)
        {
            throw new InvalidOperationException("Same-size EtherButton variants diverged in height-affecting metrics (MinHeight/Padding/FontSize).");
        }

        if (secondaryButton.ActualHeight != defaultButton.ActualHeight)
        {
            throw new InvalidOperationException($"Same-size EtherButton variants rendered at different heights: Primary={defaultButton.ActualHeight}, Secondary={secondaryButton.ActualHeight}.");
        }

        var leftIcon = GetTemplatePart<ContentPresenter>(button, "LeftIcon", nameof(EtherButton));
        var rightIcon = GetTemplatePart<ContentPresenter>(button, "RightIcon", nameof(EtherButton));
        var secondaryBackground = GetTemplatePart<Border>(secondaryButton, "Bg", nameof(EtherButton));
        var secondaryStroke = GetTemplatePart<Border>(secondaryButton, "Stroke", nameof(EtherButton));
        var templateParts = new[] { "LeftIcon", "RightIcon" };
        var leftIconStates = new List<string>();
        var rightIconStates = new List<string>();

        button.LeftIcon = CreateLibraryChevron("IconChevronLeft", 20);
        button.UpdateLayout();
        if (leftIcon.Visibility != Visibility.Visible)
        {
            throw new InvalidOperationException("The 'LeftIconVisible' LeftIconStates transition did not show the leading icon slot.");
        }

        leftIconStates.Add("LeftIconVisible");

        button.LeftIcon = null;
        button.UpdateLayout();
        if (leftIcon.Visibility != Visibility.Collapsed)
        {
            throw new InvalidOperationException("The 'LeftIconCollapsed' LeftIconStates transition did not collapse the leading icon slot.");
        }

        leftIconStates.Add("LeftIconCollapsed");

        button.RightIcon = CreateLibraryChevron("IconChevronRight", 20);
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
            themeRoot, secondaryButton, secondaryBackground, secondaryStroke, ElementTheme.Light);
        var darkTemplateBrushColors = await GetButtonTemplateBrushColorsAsync(
            themeRoot, secondaryButton, secondaryBackground, secondaryStroke, ElementTheme.Dark);
        if (lightTemplateBrushColors.SequenceEqual(darkTemplateBrushColors, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("EtherButton Light and Dark template brushes did not re-resolve to distinct values.");
        }

        return new ButtonVerification(
            true,
            defaultButton.MinWidth,
            defaultButton.MinHeight,
            defaultButton.FontSize,
            templateParts,
            leftIconStates.ToArray(),
            rightIconStates.ToArray(),
            leftIcon.Visibility == Visibility.Collapsed,
            rightIcon.Visibility == Visibility.Collapsed,
            peer.GetName(),
            lightTemplateBrushColors,
            darkTemplateBrushColors);
    }

    private static PathIcon CreateLibraryChevron(string resourceKey, double size)
    {
        var iconStyle = FindResourceStyle(Application.Current.Resources, "EtherIconGeometries.xaml", resourceKey)
            ?? throw new InvalidOperationException($"Ether icon resource '{resourceKey}' was not found.");
        return new PathIcon { Style = iconStyle, Width = size, Height = size };
    }

    private static Style? FindResourceStyle(ResourceDictionary resources, string dictionaryName, string resourceKey)
    {
        foreach (var dictionary in resources.MergedDictionaries)
        {
            if (dictionary.Source?.OriginalString.EndsWith(dictionaryName, StringComparison.OrdinalIgnoreCase) == true &&
                dictionary.TryGetValue(resourceKey, out var value) && value is Style style)
                return style;

            var nested = FindResourceStyle(dictionary, dictionaryName, resourceKey);
            if (nested is not null)
                return nested;
        }

        return resources.TryGetValue(resourceKey, out var direct) && direct is Style directStyle
            ? directStyle
            : null;
    }

    private static async Task<string[]> GetButtonTemplateBrushColorsAsync(
        FrameworkElement themeRoot,
        EtherButton button,
        Border background,
        Border stroke,
        ElementTheme theme)
    {
        themeRoot.RequestedTheme = theme;
        await WaitForAppliedThemeAsync(themeRoot, theme);
        button.UpdateLayout();

        return new[]
        {
            GetSolidBrushColor(background.Background, "secondary background", theme, nameof(EtherButton)),
            GetSolidBrushColor(stroke.BorderBrush, "secondary border", theme, nameof(EtherButton)),
        };
    }
}
