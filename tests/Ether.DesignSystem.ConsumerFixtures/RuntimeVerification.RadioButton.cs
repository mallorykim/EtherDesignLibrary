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

        var twoStateCoercionVerified = VerifyRadioButtonTwoStateCoercion();
        var groupNameMutualExclusionVerified = await VerifyRadioButtonGroupNameMutualExclusionAsync(themeRoot);

        return new RadioButtonVerification(
            true,
            defaultRadioButton.IsThreeState,
            templateParts,
            checkStates.ToArray(),
            peer.GetName(),
            lightTemplateBrushColors,
            darkTemplateBrushColors,
            twoStateCoercionVerified,
            groupNameMutualExclusionVerified);
    }

    /// <summary>
    /// Regression coverage for native GroupName mutual exclusion (EtherRadioButton.cs:10-13):
    /// hosts two isolated EtherRadioButton instances that share a GroupName under a common parent
    /// (a scratch Canvas attached to themeRoot - not the shared fixture radioButton/
    /// defaultRadioButton instances, which are unrelated controls with no GroupName set) and
    /// proves checking the second automatically unchecks the first. Async so it can pump the
    /// dispatcher a few turns after parenting the probes, in case native GroupName registration is
    /// deferred rather than synchronous with the Children.Add call - the same defensive pattern
    /// EtherMasthead's theme-tracking probe uses for its own deferred-callback wait
    /// (RuntimeVerification.Masthead.cs:164-171).
    /// </summary>
    private static async Task<bool> VerifyRadioButtonGroupNameMutualExclusionAsync(FrameworkElement themeRoot)
    {
        if (themeRoot is not Panel hostPanel)
        {
            throw new InvalidOperationException("EtherRadioButton GroupName probe requires a panel-based theme root to host an isolated scratch container.");
        }

        var scratch = new Canvas { Opacity = 0d, IsHitTestVisible = false };
        hostPanel.Children.Add(scratch);
        try
        {
            const string ProbeGroupName = "EtherRadioButtonGroupNameProbe";
            var first = new EtherRadioButton { GroupName = ProbeGroupName };
            var second = new EtherRadioButton { GroupName = ProbeGroupName };
            scratch.Children.Add(first);
            scratch.Children.Add(second);
            first.ApplyTemplate();
            second.ApplyTemplate();
            scratch.UpdateLayout();

            for (var pump = 0; pump < 5; pump++)
            {
                await Task.Delay(10);
            }

            first.IsChecked = true;
            scratch.UpdateLayout();
            if (first.IsChecked != true || second.IsChecked == true)
            {
                throw new InvalidOperationException("EtherRadioButton did not check the first same-GroupName instance as expected before exercising mutual exclusion.");
            }

            second.IsChecked = true;
            scratch.UpdateLayout();
            if (second.IsChecked != true || first.IsChecked == true)
            {
                throw new InvalidOperationException("EtherRadioButton GroupName did not enforce mutual exclusion across peers: checking the second same-GroupName instance did not auto-uncheck the first.");
            }

            return true;
        }
        finally
        {
            scratch.Children.Clear();
            hostPanel.Children.Remove(scratch);
        }
    }

    /// <summary>
    /// Regression coverage for the intentional two-state contract. Nullable IsChecked input is
    /// coerced to false and IsThreeState=true is coerced back to false. Uses a throwaway,
    /// off-tree instance because this is dependency-property behavior.
    /// </summary>
    private static bool VerifyRadioButtonTwoStateCoercion()
    {
        var probe = new EtherRadioButton();
        probe.IsChecked = null;
        if (probe.IsChecked != false)
            throw new InvalidOperationException("EtherRadioButton.IsChecked=null was not coerced to false.");

        probe.IsThreeState = true;
        if (probe.IsThreeState)
            throw new InvalidOperationException("EtherRadioButton.IsThreeState=true was not coerced back to false.");

        return true;
    }
}
