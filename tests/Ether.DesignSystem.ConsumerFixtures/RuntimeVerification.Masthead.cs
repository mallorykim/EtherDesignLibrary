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

    private static async Task<MastheadVerification> VerifyMastheadAsync(
        FrameworkElement themeRoot,
        EtherMasthead defaultMasthead,
        EtherMasthead masthead)
    {
        defaultMasthead.ApplyTemplate();
        masthead.ApplyTemplate();
        defaultMasthead.UpdateLayout();
        masthead.UpdateLayout();

        var defaultStyleResolved = defaultMasthead.ShowSettings &&
            !defaultMasthead.ShowSearch &&
            !defaultMasthead.ShowMenuIcon &&
            !defaultMasthead.ShowChevron &&
            !defaultMasthead.UseSystemFocusVisuals &&
            defaultMasthead.Template is not null;
        if (!defaultStyleResolved)
        {
            throw new InvalidOperationException("The keyed EtherMasthead style did not apply its default optional-icon metadata, UseSystemFocusVisuals=False, and template.");
        }

        var defaultSearchSlot = GetTemplatePart<Grid>(defaultMasthead, "SearchIconSlot", nameof(EtherMasthead));
        var defaultSettingsSlot = GetTemplatePart<Grid>(defaultMasthead, "SettingsButton", nameof(EtherMasthead));
        GetTemplatePart<EtherButton>(defaultMasthead, "MinimizeButton", nameof(EtherMasthead));
        GetTemplatePart<EtherButton>(defaultMasthead, "MaximizeRestoreButton", nameof(EtherMasthead));
        GetTemplatePart<EtherButton>(defaultMasthead, "CloseButton", nameof(EtherMasthead));

        var searchSlot = GetTemplatePart<Grid>(masthead, "SearchIconSlot", nameof(EtherMasthead));
        var settingsSlot = GetTemplatePart<Grid>(masthead, "SettingsButton", nameof(EtherMasthead));
        var templateParts = new[] { "SearchIconSlot", "SettingsButton", "MinimizeButton", "MaximizeRestoreButton", "CloseButton" };

        if (defaultSearchSlot.Visibility != Visibility.Collapsed || defaultSettingsSlot.Visibility != Visibility.Visible)
        {
            throw new InvalidOperationException("Default EtherMasthead did not apply SearchCollapsed and SettingsVisible.");
        }

        var optionalIconStates = new List<string>();
        if (searchSlot.Visibility != Visibility.Collapsed)
        {
            throw new InvalidOperationException("The 'SearchCollapsed' SearchIconStates transition did not hide the search slot.");
        }

        optionalIconStates.Add("SearchCollapsed");
        masthead.ShowSearch = true;
        masthead.UpdateLayout();
        if (searchSlot.Visibility != Visibility.Visible)
        {
            throw new InvalidOperationException("The 'SearchVisible' SearchIconStates transition did not show the search slot.");
        }

        optionalIconStates.Add("SearchVisible");

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(masthead)
            ?? throw new InvalidOperationException("EtherMasthead did not create an automation peer.");
        var defaultPeer = FrameworkElementAutomationPeer.CreatePeerForElement(defaultMasthead)
            ?? throw new InvalidOperationException("Default EtherMasthead did not create an automation peer.");
        if (peer.GetName() != "Package masthead" || defaultPeer.GetName() != "Default package masthead")
        {
            throw new InvalidOperationException("EtherMasthead gallery-equivalent automation names were not applied on the fixture instances.");
        }

        var settingsIcon = GetNamedSlotIcon(settingsSlot, "settings");
        var lightTemplateBrushColors = await GetMastheadTemplateBrushColorsAsync(themeRoot, masthead, settingsIcon, ElementTheme.Light);
        var darkTemplateBrushColors = await GetMastheadTemplateBrushColorsAsync(themeRoot, masthead, settingsIcon, ElementTheme.Dark);
        if (lightTemplateBrushColors.SequenceEqual(darkTemplateBrushColors, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("EtherMasthead Light and Dark template brushes did not re-resolve to distinct values.");
        }

        var (captionGlyph, captionPointerOverStateApplied) = VerifyMastheadCaptionGlyph(masthead);
        var (minimizeButtonInvokeExposed, closeButtonInvokeExposed) = VerifyMastheadCaptionButtonInvokePatterns(masthead);

        return new MastheadVerification(
            defaultStyleResolved,
            defaultMasthead.UseSystemFocusVisuals,
            defaultMasthead.ShowSettings,
            defaultMasthead.ShowSearch,
            templateParts,
            optionalIconStates.ToArray(),
            searchSlot.Visibility == Visibility.Visible,
            peer.GetName(),
            defaultPeer.GetName(),
            lightTemplateBrushColors,
            darkTemplateBrushColors,
            captionGlyph,
            captionPointerOverStateApplied,
            minimizeButtonInvokeExposed,
            closeButtonInvokeExposed);
    }

    /// <summary>
    /// Extends the composite-peer/name check above with per-caption-button automation proof:
    /// MinimizeButton and CloseButton (both EtherButton template parts, EtherMasthead.xaml.cs:63-65)
    /// must each expose PatternInterface.Invoke as IInvokeProvider, the same contract
    /// MaximizeRestoreButton is proven to have as a side effect of actually being clicked in
    /// VerifyStandardInteractionAdapters (RuntimeVerification.PropertyConsumption.cs:392-400,
    /// 432-441). This deliberately does NOT call IInvokeProvider.Invoke() on either button:
    /// invoking them would minimize or close the fixture's real host window, which would abort the
    /// rest of this runtime verification run rather than merely proving the pattern is exposed.
    /// </summary>
    private static (bool MinimizeButtonInvokeExposed, bool CloseButtonInvokeExposed) VerifyMastheadCaptionButtonInvokePatterns(
        EtherMasthead masthead)
    {
        var minimizeButton = GetTemplatePart<EtherButton>(masthead, "MinimizeButton", nameof(EtherMasthead));
        var minimizeButtonPeer = FrameworkElementAutomationPeer.CreatePeerForElement(minimizeButton)
            ?? throw new InvalidOperationException("EtherMasthead MinimizeButton did not create an automation peer.");
        if (minimizeButtonPeer.GetPattern(PatternInterface.Invoke) is not IInvokeProvider)
        {
            throw new InvalidOperationException("EtherMasthead MinimizeButton did not expose UIA Invoke.");
        }

        var closeButton = GetTemplatePart<EtherButton>(masthead, "CloseButton", nameof(EtherMasthead));
        var closeButtonPeer = FrameworkElementAutomationPeer.CreatePeerForElement(closeButton)
            ?? throw new InvalidOperationException("EtherMasthead CloseButton did not create an automation peer.");
        if (closeButtonPeer.GetPattern(PatternInterface.Invoke) is not IInvokeProvider)
        {
            throw new InvalidOperationException("EtherMasthead CloseButton did not expose UIA Invoke.");
        }

        return (true, true);
    }

    // Regression coverage for the caption buttons' Segoe Fluent Icons glyph and its hover/pressed
    // recolor: the FontIcon glyph is set once in XAML (EtherMasthead.xaml, MinimizeButton/
    // MaximizeRestoreButton/CloseButton Content) and EtherMastheadCaptionButtonStyle's own
    // VisualStateManager recolors the "Glyph" FontIcon part directly on PointerOver/Pressed
    // (Glyph.Foreground setters) — no code-behind icon-recoloring machinery is involved anymore.
    // This proves (a) the minimize caption button's Glyph FontIcon part exists and carries a
    // non-empty glyph, and (b) the button's CommonStates group can be driven into PointerOver
    // through the public VisualStateManager API (the same state EtherMastheadCaptionButtonStyle's
    // template uses to swap Glyph.Foreground).
    private static (string Glyph, bool PointerOverStateApplied) VerifyMastheadCaptionGlyph(
        EtherMasthead masthead)
    {
        var minimizeButton = GetTemplatePart<EtherButton>(masthead, "MinimizeButton", nameof(EtherMasthead));
        var glyph = GetTemplatePart<FontIcon>(minimizeButton, "Glyph", nameof(EtherMasthead));
        if (string.IsNullOrEmpty(glyph.Glyph))
        {
            throw new InvalidOperationException("EtherMasthead minimize caption button's Glyph FontIcon part did not carry a glyph.");
        }

        if (!VisualStateManager.GoToState(minimizeButton, "PointerOver", false) ||
            GetCurrentVisualStateName(minimizeButton, "CommonStates", nameof(EtherMasthead)) != "PointerOver")
        {
            throw new InvalidOperationException("EtherMasthead minimize caption button did not enter PointerOver through the public VisualStateManager API.");
        }

        VisualStateManager.GoToState(minimizeButton, "Normal", false);

        return (glyph.Glyph, true);
    }

    private static FontIcon GetNamedSlotIcon(Grid slot, string role)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(slot); index++)
        {
            if (VisualTreeHelper.GetChild(slot, index) is FontIcon icon)
            {
                return icon;
            }
        }

        throw new InvalidOperationException($"EtherMasthead {role} slot did not contain a FontIcon.");
    }

    private static async Task<string[]> GetMastheadTemplateBrushColorsAsync(
        FrameworkElement themeRoot,
        EtherMasthead masthead,
        FontIcon settingsIcon,
        ElementTheme theme)
    {
        themeRoot.RequestedTheme = theme;
        await WaitForAppliedThemeAsync(themeRoot, theme);
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (masthead.ActualTheme != theme && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25);
        }

        masthead.UpdateLayout();
        return new[]
        {
            GetSolidBrushColor(settingsIcon.Foreground, "settings icon", theme, nameof(EtherMasthead)),
        };
    }
}
