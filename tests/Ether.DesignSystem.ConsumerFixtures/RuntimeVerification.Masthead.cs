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

        var (hoverLightColor, hoverDarkColor) = await VerifyMastheadHoverThemeTrackingAsync(themeRoot, masthead);

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
            hoverLightColor,
            hoverDarkColor);
    }

    // Regression coverage for a bug where the caption icons' Fill/Stroke are assigned as
    // code-behind local values off PointerEntered/Exited/Pressed/Released/Canceled/CaptureLost
    // handlers (EtherMasthead.xaml.cs): once assigned procedurally, a Shape's Fill/Stroke stops
    // tracking its original {ThemeResource} XAML expression, so switching Light/Dark/HighContrast
    // theme while a caption button was (or had ever been) hovered used to leave the icon showing
    // the OLD theme's hover color until the next real hover re-synced it. The fix adds an
    // ActualThemeChanged handler that re-pushes each caption button's CURRENT CommonStates state
    // (which WinUI already tracks) back onto its icon using the freshly re-resolved swatch for
    // that state.
    //
    // This test cannot synthesize real OS pointer input to trigger PointerEntered. It uses only
    // public WinUI APIs to reproduce the same externally observable precondition instead:
    //   - VisualStateManager.GoToState(minimizeButton, "PointerOver", false) puts the button in
    //     the exact CommonStates state a real hover would establish.
    //   - The documented template parts MinimizeIcon and IconForegroundHoverSwatch are found
    //     through the public visual tree. Assigning the swatch's resolved Fill to the Shape's
    //     public Fill property reproduces the local-value effect of a real PointerEntered event.
    // This deliberately creates the stale-local-value condition the production
    // ActualThemeChanged handler must repair, without calling a non-public Ether member.
    // After priming while Light, the theme is switched to Dark WITHOUT calling either of those
    // again — i.e. without ever re-touching the pointer state — so the only thing that can update
    // the icon's Fill is the production ActualThemeChanged subscription itself. On the pre-fix
    // code (no ActualThemeChanged handler) this assertion fails because Fill stays frozen at
    // Light's hover color.
    private static async Task<(string LightHoverColor, string DarkHoverColor)> VerifyMastheadHoverThemeTrackingAsync(
        FrameworkElement themeRoot,
        EtherMasthead masthead)
    {
        var minimizeButton = GetTemplatePart<EtherButton>(masthead, "MinimizeButton", nameof(EtherMasthead));
        var minimizeIcon = GetTemplatePart<Shape>(minimizeButton, "MinimizeIcon", nameof(EtherMasthead));
        var hoverSwatch = GetTemplatePart<Shape>(masthead, "IconForegroundHoverSwatch", nameof(EtherMasthead));

        themeRoot.RequestedTheme = ElementTheme.Light;
        await WaitForAppliedThemeAsync(themeRoot, ElementTheme.Light);
        await WaitForDescendantActualThemeAsync(masthead, ElementTheme.Light);

        if (!VisualStateManager.GoToState(minimizeButton, "PointerOver", false) ||
            GetCurrentVisualStateName(minimizeButton, "CommonStates", nameof(EtherMasthead)) != "PointerOver")
        {
            throw new InvalidOperationException("EtherMasthead minimize caption button did not enter PointerOver through the public VisualStateManager API.");
        }

        minimizeIcon.Fill = hoverSwatch.Fill
            ?? throw new InvalidOperationException("EtherMasthead hover swatch did not resolve to a brush in Light theme.");
        minimizeButton.UpdateLayout();
        var lightHoverColor = GetSolidBrushColor(minimizeIcon.Fill, "minimize icon (hovered, Light)", ElementTheme.Light, nameof(EtherMasthead));
        var expectedLightHoverColor = GetSolidBrushColor(hoverSwatch.Fill, "hover swatch (Light)", ElementTheme.Light, nameof(EtherMasthead));
        if (!string.Equals(lightHoverColor, expectedLightHoverColor, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"EtherMasthead public hover setup produced {lightHoverColor}, expected the Light hover swatch {expectedLightHoverColor}.");
        }

        // Switch theme WITHOUT re-priming the hover color: only the production
        // ActualThemeChanged subscription can update Fill from here on. Propagation of the new
        // theme from themeRoot down to a deep descendant like masthead can lag behind
        // themeRoot.ActualTheme itself flipping (see GetMastheadTemplateBrushColorsAsync above,
        // which has the same extra wait for exactly this reason), so wait for masthead's own
        // ActualTheme — and therefore its ActualThemeChanged firing — before reading.
        themeRoot.RequestedTheme = ElementTheme.Dark;
        await WaitForAppliedThemeAsync(themeRoot, ElementTheme.Dark);
        await WaitForDescendantActualThemeAsync(masthead, ElementTheme.Dark);

        // EtherMasthead_ActualThemeChanged defers its swatch refresh by one DispatcherQueue tick
        // (the swatches' {ThemeResource} Fill has not actually re-resolved to the new theme yet
        // at the instant ActualThemeChanged fires — see the comment on that handler). Give the
        // dispatcher queue a few turns to actually run that deferred callback before reading.
        for (var pump = 0; pump < 10; pump++)
        {
            await Task.Delay(25);
        }

        minimizeButton.UpdateLayout();
        var darkHoverColor = GetSolidBrushColor(minimizeIcon.Fill, "minimize icon (hovered, Dark, automatic)", ElementTheme.Dark, nameof(EtherMasthead));
        var expectedDarkHoverColor = GetSolidBrushColor(hoverSwatch.Fill, "hover swatch (Dark)", ElementTheme.Dark, nameof(EtherMasthead));

        if (string.Equals(lightHoverColor, darkHoverColor, StringComparison.Ordinal) ||
            !string.Equals(darkHoverColor, expectedDarkHoverColor, StringComparison.Ordinal))
        {
            var stateName = GetCurrentVisualStateName(minimizeButton, "CommonStates", nameof(EtherMasthead));

            throw new InvalidOperationException(
                "EtherMasthead caption icon Fill did not change when the theme switched Light -> Dark while the caption " +
                "button stayed in PointerOver without being re-hovered. The icon color went stale on theme change " +
                "(EtherMasthead_ActualThemeChanged did not refresh it). DIAGNOSTIC: " +
                $"lightHoverColor={lightHoverColor}, darkHoverColor(automatic)={darkHoverColor}, " +
                $"expectedDarkHoverColor={expectedDarkHoverColor}, " +
                $"minimizeButton CommonStates.CurrentState={stateName}.");
        }

        VisualStateManager.GoToState(minimizeButton, "Normal", false);
        var defaultSwatch = GetTemplatePart<Shape>(masthead, "IconForegroundDefaultSwatch", nameof(EtherMasthead));
        minimizeIcon.Fill = defaultSwatch.Fill
            ?? throw new InvalidOperationException("EtherMasthead default swatch did not resolve to a brush during cleanup.");
        minimizeButton.UpdateLayout();

        return (lightHoverColor, darkHoverColor);
    }

    // themeRoot.ActualTheme flipping does not guarantee a deep descendant's ActualTheme (and
    // therefore its ActualThemeChanged event) has propagated yet — GetMastheadTemplateBrushColorsAsync
    // below already has to poll masthead.ActualTheme separately for the same reason. Shared here
    // so VerifyMastheadHoverThemeTrackingAsync's fresh ActualThemeChanged-driven assertion isn't
    // racing that same propagation lag.
    private static async Task WaitForDescendantActualThemeAsync(FrameworkElement descendant, ElementTheme theme)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (descendant.ActualTheme != theme && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25);
        }

        if (descendant.ActualTheme != theme)
        {
            throw new InvalidOperationException(
                $"EtherMasthead ActualTheme did not propagate to '{theme}' in time (still '{descendant.ActualTheme}').");
        }
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
