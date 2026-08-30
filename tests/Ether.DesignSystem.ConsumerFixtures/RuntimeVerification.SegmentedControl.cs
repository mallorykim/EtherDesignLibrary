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

    private static async Task<SegmentedControlVerification> VerifySegmentedControlAsync(
        FrameworkElement themeRoot,
        EtherSegmentedControl defaultSegmentedControl,
        EtherSegmentedControl segmentedControl)
    {
        defaultSegmentedControl.ApplyTemplate();
        segmentedControl.ApplyTemplate();
        defaultSegmentedControl.UpdateLayout();
        segmentedControl.UpdateLayout();

        if (defaultSegmentedControl.Padding != new Thickness(4) ||
            defaultSegmentedControl.HorizontalAlignment != HorizontalAlignment.Stretch ||
            defaultSegmentedControl.HorizontalContentAlignment != HorizontalAlignment.Stretch ||
            defaultSegmentedControl.VerticalContentAlignment != VerticalAlignment.Center ||
            defaultSegmentedControl.IsTabStop ||
            defaultSegmentedControl.UseSystemFocusVisuals ||
            defaultSegmentedControl.Template is null)
        {
            throw new InvalidOperationException("The keyed EtherSegmentedControl style did not apply its default Padding, stretch alignment, IsTabStop=False, UseSystemFocusVisuals=False, and template.");
        }

        GetTemplatePart<Border>(segmentedControl, "ShadowHost", nameof(EtherSegmentedControl));
        var trackSurface = GetTemplatePart<Border>(segmentedControl, "TrackSurface", nameof(EtherSegmentedControl));
        var templateParts = new[] { "ShadowHost", "TrackSurface" };
        var segments = GetSegmentRadioButtons(segmentedControl);
        if (segments.Length < 2)
        {
            throw new InvalidOperationException("EtherSegmentedControl did not host at least two EtherSegment radio buttons.");
        }

        foreach (var segment in segments)
        {
            segment.ApplyTemplate();
            segment.UpdateLayout();
            if (segment.Style is null)
            {
                throw new InvalidOperationException("EtherSegmentedControl segments did not resolve the keyed EtherSegment style.");
            }
        }

        var selectionChanges = new List<SegmentedSelectionChangedEventArgs>();
        segmentedControl.SelectionChanged += (_, args) => selectionChanges.Add(args);
        var expectedSelectedValue = segments[^1].Content;
        // SetValue mirrors the dependency-property write path used by a TwoWay binding.
        segmentedControl.SetValue(EtherSegmentedControl.SelectedValueProperty, expectedSelectedValue);
        if (!Equals(segmentedControl.SelectedValue, expectedSelectedValue) ||
            segments[^1].IsChecked != true ||
            segments.Count(segment => segment.IsChecked == true) != 1 ||
            selectionChanges.Count != 1 ||
            !Equals(selectionChanges[0].NewValue, expectedSelectedValue))
        {
            throw new InvalidOperationException("EtherSegmentedControl did not expose a stable SelectedValue and SelectionChanged contract for a binding-driven selection.");
        }

        var proofSegment = segments[0];
        var checkStates = new List<string>();

        var lightTemplateBrushColors = await GetSegmentedControlTrackBrushColorsAsync(
            themeRoot, segmentedControl, trackSurface, ElementTheme.Light);
        var darkTemplateBrushColors = await GetSegmentedControlTrackBrushColorsAsync(
            themeRoot, segmentedControl, trackSurface, ElementTheme.Dark);
        if (lightTemplateBrushColors.SequenceEqual(darkTemplateBrushColors, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("EtherSegmentedControl Light and Dark template brushes did not re-resolve to distinct values.");
        }

        proofSegment.IsChecked = false;
        proofSegment.UpdateLayout();
        VisualStateManager.GoToState(proofSegment, "Normal", false);
        proofSegment.UpdateLayout();
        if (GetCurrentVisualStateName(proofSegment, "CommonStates", nameof(EtherSegmentedControl)) != "Normal")
        {
            throw new InvalidOperationException("The 'Normal' EtherSegment CommonStates transition did not become current after unchecking.");
        }

        checkStates.Add("Unchecked");

        proofSegment.IsChecked = true;
        proofSegment.UpdateLayout();
        VisualStateManager.GoToState(proofSegment, "Checked", false);
        proofSegment.UpdateLayout();
        if (GetCurrentVisualStateName(proofSegment, "CommonStates", nameof(EtherSegmentedControl)) != "Checked")
        {
            throw new InvalidOperationException("The 'Checked' EtherSegment CommonStates transition did not become current after checking.");
        }

        checkStates.Add("Checked");

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(segmentedControl)
            ?? throw new InvalidOperationException("EtherSegmentedControl did not create an automation peer.");
        if (peer.GetName() != "Package segmented control")
        {
            throw new InvalidOperationException("EtherSegmentedControl did not expose the required automation name.");
        }

        return new SegmentedControlVerification(
            true,
            defaultSegmentedControl.Padding.Left,
            templateParts,
            segments.Length,
            checkStates.ToArray(),
            peer.GetName(),
            lightTemplateBrushColors,
            darkTemplateBrushColors);
    }

    private static async Task<string[]> GetSegmentedControlTrackBrushColorsAsync(
        FrameworkElement themeRoot,
        EtherSegmentedControl segmentedControl,
        Border trackSurface,
        ElementTheme theme)
    {
        themeRoot.RequestedTheme = theme;
        await WaitForAppliedThemeAsync(themeRoot, theme);
        segmentedControl.UpdateLayout();

        return new[]
        {
            GetSolidBrushColor(trackSurface.Background, "track background", theme, nameof(EtherSegmentedControl)),
        };
    }
}
