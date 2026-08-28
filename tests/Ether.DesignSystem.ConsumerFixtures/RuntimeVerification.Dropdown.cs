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

    private static async Task<DropdownVerification> VerifyDropdownAsync(
        FrameworkElement themeRoot,
        EtherDropdown defaultDropdown,
        EtherDropdown dropdown)
    {
        defaultDropdown.ApplyTemplate();
        dropdown.ApplyTemplate();
        defaultDropdown.UpdateLayout();
        dropdown.UpdateLayout();

        if (defaultDropdown.MaxVisibleItems != 6 ||
            defaultDropdown.UseSystemFocusVisuals ||
            defaultDropdown.Template is null ||
            defaultDropdown.ItemContainerStyle is null)
        {
            throw new InvalidOperationException("The keyed EtherDropdown style did not apply its default MaxVisibleItems, UseSystemFocusVisuals=False setter, item style, and template.");
        }

        GetTemplatePart<Grid>(dropdown, "LayoutRoot", nameof(EtherDropdown));
        var triggerText = GetTemplatePart<TextBlock>(dropdown, "TriggerText", nameof(EtherDropdown));
        GetTemplatePart<FrameworkElement>(dropdown, "Arrow", nameof(EtherDropdown));
        var contentPresenter = GetTemplatePart<ContentPresenter>(dropdown, "ContentPresenter", nameof(EtherDropdown));
        var activeStroke = GetTemplatePart<Border>(dropdown, "ActiveStroke", nameof(EtherDropdown));
        var stateFill = GetTemplatePart<Border>(dropdown, "StateFill", nameof(EtherDropdown));
        var openFill = GetTemplatePart<Border>(dropdown, "OpenFill", nameof(EtherDropdown));
        GetTemplatePart<Border>(dropdown, "FocusRing", nameof(EtherDropdown));
        // PopupBorder and ScrollViewer live inside ComboBox's popup host, which is not
        // parented into the closed visual tree. Walk Popup.Child so the closed menu
        // parts are still reachable without opening the live popup.
        var popup = GetTemplatePart<Microsoft.UI.Xaml.Controls.Primitives.Popup>(dropdown, "Popup", nameof(EtherDropdown));
        if (popup.IsOpen)
        {
            throw new InvalidOperationException("EtherDropdown popup must stay closed while proving closed-tree popup parts.");
        }

        GetTemplatePart<Border>(dropdown, "PopupBorder", nameof(EtherDropdown));
        GetTemplatePart<ScrollViewer>(dropdown, "ScrollViewer", nameof(EtherDropdown));
        var templateParts = new[] { "LayoutRoot", "StateFill", "OpenFill", "TriggerText", "Arrow", "ActiveStroke", "FocusRing", "Popup", "PopupBorder", "ScrollViewer", "ContentPresenter" };

        if (contentPresenter.Visibility != Visibility.Collapsed)
        {
            throw new InvalidOperationException("EtherDropdown ContentPresenter must stay collapsed so ComboBox can own it while TriggerText shows the selection.");
        }

        if (triggerText.Text != "10 Minutes")
        {
            throw new InvalidOperationException($"EtherDropdown TriggerText did not show the selected item. Observed '{triggerText.Text}'.");
        }

        dropdown.IsHitTestVisible = false;
        dropdown.IsTabStop = false;

        var dropDownStates = new List<string>();
        VisualStateManager.GoToState(dropdown, "Opened", false);
        dropdown.UpdateLayout();
        if (activeStroke.Visibility != Visibility.Visible ||
            stateFill.Visibility != Visibility.Collapsed ||
            openFill.Visibility != Visibility.Visible)
        {
            throw new InvalidOperationException("The 'Opened' DropDownStates transition did not swap to OpenFill and show the active stroke on EtherDropdown.");
        }

        dropDownStates.Add("Opened");

        var lightTemplateBrushColors = await GetDropdownForegroundBrushColorsAsync(
            themeRoot, dropdown, triggerText, ElementTheme.Light);
        var darkTemplateBrushColors = await GetDropdownForegroundBrushColorsAsync(
            themeRoot, dropdown, triggerText, ElementTheme.Dark);
        if (lightTemplateBrushColors.SequenceEqual(darkTemplateBrushColors, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("EtherDropdown Light and Dark template brushes did not re-resolve to distinct values.");
        }

        VisualStateManager.GoToState(dropdown, "Closed", false);
        dropdown.UpdateLayout();
        if (activeStroke.Visibility != Visibility.Collapsed ||
            stateFill.Visibility != Visibility.Visible ||
            openFill.Visibility != Visibility.Collapsed)
        {
            throw new InvalidOperationException("The 'Closed' DropDownStates transition did not restore StateFill and hide the active stroke on EtherDropdown.");
        }

        dropDownStates.Add("Closed");

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(dropdown)
            ?? throw new InvalidOperationException("EtherDropdown did not create an automation peer.");
        if (peer.GetName() != "Package dropdown")
        {
            throw new InvalidOperationException("EtherDropdown did not expose the required automation name.");
        }

        return new DropdownVerification(
            true,
            defaultDropdown.MaxVisibleItems,
            defaultDropdown.UseSystemFocusVisuals,
            templateParts,
            dropDownStates.ToArray(),
            triggerText.Text == "10 Minutes",
            contentPresenter.Visibility == Visibility.Collapsed,
            true,
            activeStroke.Visibility == Visibility.Collapsed,
            peer.GetName(),
            lightTemplateBrushColors,
            darkTemplateBrushColors);
    }

    private static async Task<string[]> GetDropdownForegroundBrushColorsAsync(
        FrameworkElement themeRoot,
        EtherDropdown dropdown,
        TextBlock triggerText,
        ElementTheme theme)
    {
        themeRoot.RequestedTheme = theme;
        await WaitForAppliedThemeAsync(themeRoot, theme);
        dropdown.UpdateLayout();

        return new[]
        {
            GetSolidBrushColor(triggerText.Foreground, "trigger foreground", theme, nameof(EtherDropdown)),
        };
    }
}
