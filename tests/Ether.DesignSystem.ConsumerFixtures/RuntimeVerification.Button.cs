using System.Globalization;
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

        // Regression guard: the default EtherButton style must not install a ContentTemplate.
        // Shape content (icons and consumer-owned visuals) must remain raw ContentPresenter content.
        var contentPresenter = GetTemplatePart<ContentPresenter>(button, "Cp", nameof(EtherButton));
        var textContent = GetTemplatePart<TextBlock>(button, "TextContent", nameof(EtherButton));
        var originalContent = button.Content;
        var originalContentTemplate = button.ContentTemplate;
        var shapeContent = new Rectangle { Width = 10, Height = 10 };
        button.ContentTemplate = null;
        button.Content = shapeContent;
        button.UpdateLayout();
        if (!ReferenceEquals(contentPresenter.Content, shapeContent) ||
            contentPresenter.ContentTemplate is not null ||
            contentPresenter.Visibility != Visibility.Visible ||
            textContent.Visibility != Visibility.Collapsed)
        {
            throw new InvalidOperationException("EtherButton transformed non-string Shape content through a default ContentTemplate.");
        }

        button.Content = originalContent;
        button.ContentTemplate = originalContentTemplate;
        button.UpdateLayout();

        button.ContentTemplate = null;
        button.Content = "A deliberately long string used to verify the button text policy.";
        button.UpdateLayout();
        if (contentPresenter.Visibility != Visibility.Collapsed ||
            textContent.Visibility != Visibility.Visible ||
            textContent.TextTrimming != TextTrimming.CharacterEllipsis)
        {
            var diag = new System.Text.StringBuilder();
            diag.Append("DIAG presenterTemplate=").Append(contentPresenter.ContentTemplate is null ? "null" : "set");
            diag.Append(" presenterSelector=").Append(contentPresenter.ContentTemplateSelector is null ? "null" : contentPresenter.ContentTemplateSelector.GetType().Name);
            diag.Append(" buttonSelector=").Append(button.ContentTemplateSelector is null ? "null" : button.ContentTemplateSelector.GetType().Name);
            diag.Append(" presenterContent=").Append(contentPresenter.Content?.GetType().Name ?? "null");
            diag.Append(" childCount=").Append(VisualTreeHelper.GetChildrenCount(contentPresenter));
            diag.Append(" tree=[");
            DescribeTree(contentPresenter, diag, 0);
            diag.Append(']');
            diag.Append(" explicitText visibility=").Append(textContent.Visibility)
                .Append(" wrapping=").Append(textContent.TextWrapping)
                .Append(" trimming=").Append(textContent.TextTrimming)
                .Append(" text=").Append(textContent.Text is null ? "null" : "len" + textContent.Text.Length.ToString(CultureInfo.InvariantCulture));
            throw new InvalidOperationException("EtherButton string content did not receive CharacterEllipsis from the explicit single-line text path. " + diag);
        }

        // A consumer-supplied ContentTemplate must take precedence over the built-in string
        // path, including when Content itself is a string.
        var consumerContentTemplate = new DataTemplate();
        button.ContentTemplate = consumerContentTemplate;
        button.UpdateLayout();
        if (contentPresenter.Visibility != Visibility.Visible ||
            textContent.Visibility != Visibility.Collapsed ||
            !ReferenceEquals(contentPresenter.ContentTemplate, consumerContentTemplate))
        {
            throw new InvalidOperationException("EtherButton ignored a consumer ContentTemplate for string content.");
        }

        button.Content = originalContent;
        button.ContentTemplate = originalContentTemplate;
        button.UpdateLayout();

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

    private static void DescribeTree(DependencyObject root, System.Text.StringBuilder sb, int depth)
    {
        if (depth > 4)
            return;

        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            sb.Append(' ').Append(new string('>', depth + 1)).Append(child.GetType().Name);
            DescribeTree(child, sb, depth + 1);
        }
    }

    private static T? FindVisualDescendant<T>(DependencyObject root)
        where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match)
                return match;

            var descendant = FindVisualDescendant<T>(child);
            if (descendant is not null)
                return descendant;
        }

        return null;
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
