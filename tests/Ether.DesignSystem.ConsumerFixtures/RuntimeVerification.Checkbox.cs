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

        GetTemplatePart<Border>(checkbox, "UncheckedFill", nameof(EtherCheckbox));
        var uncheckedFace = GetTemplatePart<Grid>(checkbox, "UncheckedFace", nameof(EtherCheckbox));
        var checkedFace = GetTemplatePart<Grid>(checkbox, "CheckedFace", nameof(EtherCheckbox));
        GetTemplatePart<Microsoft.UI.Xaml.Shapes.Path>(checkbox, "Glyph", nameof(EtherCheckbox));
        var label = GetTemplatePart<ContentPresenter>(checkbox, "Label", nameof(EtherCheckbox));
        var templateParts = new[] { "UncheckedFill", "UncheckedFace", "CheckedFace", "Glyph", "Label" };
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

        return new CheckboxVerification(
            true,
            defaultCheckbox.IsThreeState,
            templateParts,
            checkStates.ToArray(),
            peer.GetName(),
            lightTemplateBrushColors,
            darkTemplateBrushColors);
    }
}
