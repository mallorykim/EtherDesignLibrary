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

    private static async Task<RadioButtonVerification> VerifyRadioButtonAsync(
        FrameworkElement themeRoot,
        EtherRadioButton defaultRadioButton,
        EtherRadioButton radioButton)
    {
        defaultRadioButton.ApplyTemplate();
        radioButton.ApplyTemplate();
        defaultRadioButton.UpdateLayout();
        radioButton.UpdateLayout();

        if (defaultRadioButton.IsThreeState || defaultRadioButton.Template is null)
        {
            throw new InvalidOperationException("The keyed EtherRadioButton style did not apply its default IsThreeState=False setter and template.");
        }

        GetTemplatePart<Grid>(radioButton, "LayoutRoot", nameof(EtherRadioButton));
        GetTemplatePart<Border>(radioButton, "UncheckedFill", nameof(EtherRadioButton));
        GetTemplatePart<Border>(radioButton, "UncheckedStroke", nameof(EtherRadioButton));
        var uncheckedFace = GetTemplatePart<Grid>(radioButton, "UncheckedFace", nameof(EtherRadioButton));
        var checkedFace = GetTemplatePart<Grid>(radioButton, "CheckedFace", nameof(EtherRadioButton));
        GetTemplatePart<Border>(radioButton, "CheckedFill", nameof(EtherRadioButton));
        GetTemplatePart<Border>(radioButton, "CheckedStroke", nameof(EtherRadioButton));
        GetTemplatePart<Microsoft.UI.Xaml.Shapes.Ellipse>(radioButton, "Dot", nameof(EtherRadioButton));
        var label = GetTemplatePart<ContentPresenter>(radioButton, "Label", nameof(EtherRadioButton));
        var templateParts = new[] { "LayoutRoot", "UncheckedFill", "UncheckedStroke", "UncheckedFace", "CheckedFace", "CheckedFill", "CheckedStroke", "Dot", "Label" };
        var checkStates = new List<string>();

        radioButton.IsChecked = false;
        radioButton.UpdateLayout();
        AssertCheckFaces(uncheckedFace, checkedFace, false, "Unchecked", nameof(EtherRadioButton));
        checkStates.Add("Unchecked");

        var lightTemplateBrushColors = await GetToggleLabelBrushColorsAsync(
            themeRoot, radioButton, label, ElementTheme.Light, nameof(EtherRadioButton));
        var darkTemplateBrushColors = await GetToggleLabelBrushColorsAsync(
            themeRoot, radioButton, label, ElementTheme.Dark, nameof(EtherRadioButton));
        if (lightTemplateBrushColors.SequenceEqual(darkTemplateBrushColors, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("EtherRadioButton Light and Dark template brushes did not re-resolve to distinct values.");
        }

        radioButton.IsChecked = true;
        radioButton.UpdateLayout();
        AssertCheckFaces(uncheckedFace, checkedFace, true, "Checked", nameof(EtherRadioButton));
        checkStates.Add("Checked");

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(radioButton)
            ?? throw new InvalidOperationException("EtherRadioButton did not create an automation peer.");
        if (peer.GetName() != "Package radio button")
        {
            throw new InvalidOperationException("EtherRadioButton did not expose the required automation name.");
        }

        return new RadioButtonVerification(
            true,
            defaultRadioButton.IsThreeState,
            templateParts,
            checkStates.ToArray(),
            peer.GetName(),
            lightTemplateBrushColors,
            darkTemplateBrushColors);
    }
}
