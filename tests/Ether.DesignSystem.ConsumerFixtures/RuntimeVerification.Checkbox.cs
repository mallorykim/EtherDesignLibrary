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

    private static async Task<CheckboxVerification> VerifyCheckboxAsync(
        FrameworkElement themeRoot,
        EtherCheckbox defaultCheckbox,
        EtherCheckbox checkbox)
    {
        defaultCheckbox.ApplyTemplate();
        checkbox.ApplyTemplate();
        defaultCheckbox.UpdateLayout();
        checkbox.UpdateLayout();

        if (defaultCheckbox.IsThreeState || defaultCheckbox.Template is null)
        {
            throw new InvalidOperationException("The keyed EtherCheckbox style did not apply its default IsThreeState=False setter and template.");
        }

        GetTemplatePart<Grid>(checkbox, "LayoutRoot", nameof(EtherCheckbox));
        GetTemplatePart<Border>(checkbox, "UncheckedFill", nameof(EtherCheckbox));
        GetTemplatePart<Border>(checkbox, "UncheckedStroke", nameof(EtherCheckbox));
        var uncheckedFace = GetTemplatePart<Grid>(checkbox, "UncheckedFace", nameof(EtherCheckbox));
        var checkedFace = GetTemplatePart<Grid>(checkbox, "CheckedFace", nameof(EtherCheckbox));
        GetTemplatePart<Border>(checkbox, "CheckedFill", nameof(EtherCheckbox));
        GetTemplatePart<Border>(checkbox, "CheckedStroke", nameof(EtherCheckbox));
        GetTemplatePart<Microsoft.UI.Xaml.Shapes.Path>(checkbox, "Glyph", nameof(EtherCheckbox));
        var label = GetTemplatePart<ContentPresenter>(checkbox, "Label", nameof(EtherCheckbox));
        var templateParts = new[] { "LayoutRoot", "UncheckedFill", "UncheckedStroke", "UncheckedFace", "CheckedFace", "CheckedFill", "CheckedStroke", "Glyph", "Label" };
        var checkStates = new List<string>();

        checkbox.IsChecked = false;
        checkbox.UpdateLayout();
        AssertCheckFaces(uncheckedFace, checkedFace, false, "Unchecked", nameof(EtherCheckbox));
        checkStates.Add("Unchecked");

        var lightTemplateBrushColors = await GetToggleLabelBrushColorsAsync(
            themeRoot, checkbox, label, ElementTheme.Light, nameof(EtherCheckbox));
        var darkTemplateBrushColors = await GetToggleLabelBrushColorsAsync(
            themeRoot, checkbox, label, ElementTheme.Dark, nameof(EtherCheckbox));
        if (lightTemplateBrushColors.SequenceEqual(darkTemplateBrushColors, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("EtherCheckbox Light and Dark template brushes did not re-resolve to distinct values.");
        }

        checkbox.IsChecked = true;
        checkbox.UpdateLayout();
        AssertCheckFaces(uncheckedFace, checkedFace, true, "Checked", nameof(EtherCheckbox));
        checkStates.Add("Checked");

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(checkbox)
            ?? throw new InvalidOperationException("EtherCheckbox did not create an automation peer.");
        if (peer.GetName() != "Package checkbox")
        {
            throw new InvalidOperationException("EtherCheckbox did not expose the required automation name.");
        }

        var isThreeStateRejected = VerifyIsThreeStateRejected();

        return new CheckboxVerification(
            true,
            defaultCheckbox.IsThreeState,
            templateParts,
            checkStates.ToArray(),
            peer.GetName(),
            lightTemplateBrushColors,
            darkTemplateBrushColors,
            isThreeStateRejected);
    }

    /// <summary>
    /// Regression coverage for the IsThreeState interception: DefaultEtherCheckboxStyle has no
    /// Indeterminate visual, so flipping IsThreeState to true must fail loudly at the moment it
    /// is set, and must not leave the property sitting at true. Uses a throwaway, off-tree
    /// instance since this is pure dependency-property behavior - no template or layout needed.
    /// </summary>
    private static bool VerifyIsThreeStateRejected()
    {
        var probe = new EtherCheckbox();
        var threw = false;
        try
        {
            probe.IsThreeState = true;
        }
        catch (NotSupportedException)
        {
            threw = true;
        }

        if (!threw || probe.IsThreeState)
        {
            throw new InvalidOperationException("EtherCheckbox.IsThreeState=true did not throw NotSupportedException and reset back to false.");
        }

        return true;
    }
}
