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
    // Plain object with a public property, standing in for a consumer's view model. Its type
    // name ("DisplayMemberPathProbeItem") is intentionally distinctive so the assertion below
    // can tell "resolved the bound property" apart from "fell back to ToString() on the item".
    private sealed record DisplayMemberPathProbeItem(string Label);

    private static async Task<DropdownVerification> VerifyDropdownAsync(
        FrameworkElement themeRoot,
        EtherDropdown defaultDropdown,
        EtherDropdown dropdown)
    {
        defaultDropdown.ApplyTemplate();
        dropdown.ApplyTemplate();
        defaultDropdown.UpdateLayout();
        dropdown.UpdateLayout();

        if (defaultDropdown.MinWidth != 130d ||
            defaultDropdown.MaxVisibleItems != 6 ||
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

        dropdown.PlaceholderText = "Select duration";
        dropdown.SelectedIndex = -1;
        dropdown.UpdateLayout();
        if (triggerText.Text != "Select duration")
        {
            throw new InvalidOperationException($"EtherDropdown PlaceholderText was not rendered while unselected. Observed '{triggerText.Text}'.");
        }
        var placeholderTextShowsWhenUnselected = true;

        dropdown.SelectedIndex = 0;
        dropdown.UpdateLayout();
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

        var displayMemberPathTriggerText = VerifyDisplayMemberPath(themeRoot);
        var (maxDropDownHeightConstrainsPopup, maxDropDownHeightSmallerCap, maxDropDownHeightLargerCap) =
            VerifyMaxDropDownHeightConstrainsPopup(themeRoot);
        VerifyDropdownMenuPositionStableAcrossSelection(themeRoot);

        return new DropdownVerification(
            true,
            defaultDropdown.MinWidth,
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
            darkTemplateBrushColors,
            placeholderTextShowsWhenUnselected,
            displayMemberPathTriggerText,
            maxDropDownHeightConstrainsPopup,
            maxDropDownHeightSmallerCap,
            maxDropDownHeightLargerCap);
    }

    /// <summary>
    /// Regression coverage for <see cref="EtherDropdown.MaxDropDownHeight"/> (inherited from
    /// <see cref="ComboBox"/>): <c>EtherDropdown.ApplyMaxVisibleHeight</c> now composes it with
    /// <see cref="EtherDropdown.MaxVisibleItems"/> as an additional pixel ceiling on the popup's
    /// ScrollViewer (EtherDropdown.cs). Sets <c>MaxVisibleItems = 0</c> (unbounded by item count)
    /// on an isolated probe so only MaxDropDownHeight can constrain the popup, actually opens the
    /// popup (not just a VisualStateManager state, unlike the DropDownStates proof above - opening
    /// for real is what runs ApplyMaxVisibleHeight, since it is driven by the framework's own
    /// OnDropDownOpened override) with two distinct, deliberately small MaxDropDownHeight values,
    /// and asserts each opens the popup's ScrollViewer at exactly that height. Runs on an isolated,
    /// invisible probe hosted in a scratch Canvas so it does not disturb the shared, already-
    /// verified dropdown instance or its visual baseline - the same pattern VerifyDisplayMemberPath
    /// above uses.
    /// </summary>
    private static (bool Constrains, double SmallerCap, double LargerCap) VerifyMaxDropDownHeightConstrainsPopup(
        FrameworkElement themeRoot)
    {
        if (themeRoot is not Panel hostPanel)
        {
            throw new InvalidOperationException("EtherDropdown MaxDropDownHeight probe requires a panel-based theme root to host an isolated scratch container.");
        }

        var scratch = new Canvas { Opacity = 0d, IsHitTestVisible = false };
        hostPanel.Children.Add(scratch);
        try
        {
            var probe = new EtherDropdown
            {
                ItemsSource = Enumerable.Range(0, 20).Select(index => $"Item {index}").ToArray(),
                MaxVisibleItems = 0,
            };
            scratch.Children.Add(probe);
            probe.ApplyTemplate();
            probe.UpdateLayout();

            var scrollViewer = GetTemplatePart<ScrollViewer>(probe, "ScrollViewer", nameof(EtherDropdown));

            const double SmallerCap = 80d;
            const double LargerCap = 400d;
            var smallerObserved = OpenDropdownAndReadPopupMaxHeight(probe, scrollViewer, SmallerCap);
            var largerObserved = OpenDropdownAndReadPopupMaxHeight(probe, scrollViewer, LargerCap);

            if (double.IsInfinity(smallerObserved) || Math.Abs(smallerObserved - SmallerCap) > 0.5 ||
                double.IsInfinity(largerObserved) || Math.Abs(largerObserved - LargerCap) > 0.5 ||
                !(smallerObserved < largerObserved))
            {
                throw new InvalidOperationException(
                    $"EtherDropdown.MaxDropDownHeight did not constrain the popup. Expected the ScrollViewer " +
                    $"MaxHeight to match each MaxDropDownHeight value exactly ({SmallerCap}, {LargerCap}), but " +
                    $"observed {smallerObserved} and {largerObserved}.");
            }

            return (true, smallerObserved, largerObserved);
        }
        finally
        {
            scratch.Children.Clear();
            hostPanel.Children.Remove(scratch);
        }
    }

    private static double OpenDropdownAndReadPopupMaxHeight(EtherDropdown probe, ScrollViewer scrollViewer, double maxDropDownHeight)
    {
        probe.MaxDropDownHeight = maxDropDownHeight;
        probe.IsDropDownOpen = true;
        probe.UpdateLayout();
        var observed = scrollViewer.MaxHeight;
        probe.IsDropDownOpen = false;
        probe.UpdateLayout();
        return observed;
    }

    /// <summary>
    /// Regression coverage for EtherDropdown popup placement. The stock ComboBox anchors its popup on
    /// the <b>selected item</b>, so the popup's base vertical offset moves ~one row per selected index.
    /// EtherDropdown.PositionMenu corrects that to the trigger with a RenderTransform, but only if it
    /// measures the base <i>after</i> the framework has placed the popup for this open; that is why
    /// EtherDropdown.OnDropDownOpened forces the placement pass to finish before PositionMenu runs.
    /// Without it, re-opening after changing the selection dropped the menu ~30px x delta-index off the
    /// trigger (it could cover the trigger). Opens an isolated probe at several SelectedIndex values and
    /// asserts the popup keeps a constant offset from the trigger. Actually opens the popup (which runs
    /// the framework's OnDropDownOpened override, unlike a VisualStateManager state); isolated invisible
    /// scratch host, same pattern as VerifyMaxDropDownHeightConstrainsPopup.
    /// </summary>
    private static void VerifyDropdownMenuPositionStableAcrossSelection(FrameworkElement themeRoot)
    {
        if (themeRoot is not Panel hostPanel)
        {
            throw new InvalidOperationException("EtherDropdown menu-position probe requires a panel-based theme root to host an isolated scratch container.");
        }

        var scratch = new Canvas { Opacity = 0d, IsHitTestVisible = false };
        hostPanel.Children.Add(scratch);
        try
        {
            var probe = new EtherDropdown
            {
                ItemsSource = Enumerable.Range(0, 5).Select(index => $"Item {index}").ToArray(),
            };
            scratch.Children.Add(probe);
            probe.ApplyTemplate();
            probe.UpdateLayout();

            var popupBorder = GetTemplatePart<Border>(probe, "PopupBorder", nameof(EtherDropdown));
            var reference = probe.XamlRoot?.Content as FrameworkElement ?? hostPanel;

            double? baseline = null;
            foreach (var index in new[] { 0, 2, 4, 0, 3 })
            {
                probe.SelectedIndex = index;
                probe.UpdateLayout();
                probe.IsDropDownOpen = true;
                probe.UpdateLayout();

                var triggerBottom = probe.TransformToVisual(reference).TransformPoint(new Windows.Foundation.Point(0, 0)).Y + probe.ActualHeight;
                var menuTop = popupBorder.TransformToVisual(reference).TransformPoint(new Windows.Foundation.Point(0, 0)).Y;
                var offset = menuTop - triggerBottom;

                probe.IsDropDownOpen = false;
                probe.UpdateLayout();

                baseline ??= offset;
                if (Math.Abs(offset - baseline.Value) > 2d)
                {
                    throw new InvalidOperationException(
                        $"EtherDropdown popup position drifted with the selection: SelectedIndex={index} placed the menu {offset:0.#} DIPs from the trigger vs {baseline.Value:0.#} DIPs for the first selection. The menu must anchor to the trigger regardless of the selected item - EtherDropdown.OnDropDownOpened must force the framework placement pass before PositionMenu measures.");
                }
            }
        }
        finally
        {
            scratch.Children.Clear();
            hostPanel.Children.Remove(scratch);
        }
    }

    /// <summary>
    /// Regression coverage for DisplayMemberPath: binds an object data source (not a string or
    /// ComboBoxItem) and asserts the trigger text shows the bound property's value, not the
    /// item's .NET type name. Runs on an isolated, invisible probe hosted in a scratch Canvas so
    /// it does not disturb the shared, already-verified dropdown instances or their visual
    /// baselines - the same pattern EtherSlider's boundary probes use.
    /// </summary>
    private static string VerifyDisplayMemberPath(FrameworkElement themeRoot)
    {
        if (themeRoot is not Panel hostPanel)
        {
            throw new InvalidOperationException("EtherDropdown DisplayMemberPath probe requires a panel-based theme root to host an isolated scratch container.");
        }

        var scratch = new Canvas { Opacity = 0d, IsHitTestVisible = false };
        hostPanel.Children.Add(scratch);
        try
        {
            var probe = new EtherDropdown
            {
                ItemsSource = new[]
                {
                    new DisplayMemberPathProbeItem("Widget A"),
                    new DisplayMemberPathProbeItem("Widget B"),
                },
                DisplayMemberPath = nameof(DisplayMemberPathProbeItem.Label),
            };
            scratch.Children.Add(probe);
            probe.ApplyTemplate();
            probe.SelectedIndex = 0;
            probe.UpdateLayout();

            var triggerText = GetTemplatePart<TextBlock>(probe, "TriggerText", nameof(EtherDropdown));
            var text = triggerText.Text;
            if (text != "Widget A" || text.Contains(nameof(DisplayMemberPathProbeItem), StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"EtherDropdown did not resolve DisplayMemberPath for an object data source. Observed trigger text '{text}'.");
            }

            return text;
        }
        finally
        {
            scratch.Children.Clear();
            hostPanel.Children.Remove(scratch);
        }
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
