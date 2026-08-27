using EtherSandbox.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace Ether.DesignSystem.ConsumerFixtures;

// Helpers shared by controls whose templates expose WinUI visual states.
internal static partial class RuntimeVerification
{
    private static RadioButton[] GetSegmentRadioButtons(EtherSegmentedControl control)
    {
        if (control.Content is not Panel panel)
        {
            throw new InvalidOperationException("EtherSegmentedControl content must be a panel of EtherSegment radio buttons.");
        }

        return panel.Children.OfType<RadioButton>().ToArray();
    }

    private static string GetCurrentVisualStateName(FrameworkElement control, string groupName, string ownerName)
    {
        if (VisualTreeHelper.GetChildrenCount(control) < 1 ||
            VisualTreeHelper.GetChild(control, 0) is not FrameworkElement root)
        {
            throw new InvalidOperationException($"{ownerName} has no template root for visual-state inspection.");
        }

        var group = VisualStateManager.GetVisualStateGroups(root)
            .OfType<VisualStateGroup>()
            .FirstOrDefault(candidate => candidate.Name == groupName)
            ?? throw new InvalidOperationException($"{ownerName} is missing VisualStateGroup '{groupName}'.");

        return group.CurrentState?.Name
            ?? throw new InvalidOperationException($"{ownerName} VisualStateGroup '{groupName}' has no current state.");
    }

    private static void AssertCheckFaces(FrameworkElement uncheckedFace, FrameworkElement checkedFace, bool isChecked, string expectedState, string ownerName)
    {
        var expectedUnchecked = isChecked ? Visibility.Collapsed : Visibility.Visible;
        var expectedChecked = isChecked ? Visibility.Visible : Visibility.Collapsed;
        if (uncheckedFace.Visibility != expectedUnchecked || checkedFace.Visibility != expectedChecked)
        {
            throw new InvalidOperationException($"The '{expectedState}' CheckStates transition did not apply the expected face visibility on {ownerName}.");
        }
    }

    private static T GetTemplatePart<T>(FrameworkElement root, string name, string ownerName = "EtherProgressBar")
        where T : class
    {
        if (root is T rootPart && root.Name == name)
        {
            return rootPart;
        }

        if (root is Popup popup && popup.Child is FrameworkElement popupChild)
        {
            try
            {
                return GetTemplatePart<T>(popupChild, name, ownerName);
            }
            catch (InvalidOperationException)
            {
                // Search the rest of the closed visual tree, then fail if still missing.
            }
        }

        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            if (VisualTreeHelper.GetChild(root, index) is FrameworkElement child)
            {
                try
                {
                    return GetTemplatePart<T>(child, name, ownerName);
                }
                catch (InvalidOperationException)
                {
                    // Search the next branch of the visual tree.
                }
            }
        }

        throw new InvalidOperationException($"{ownerName} template part '{name}' was missing or had the wrong type.");
    }

    private static async Task<string[]> GetToggleLabelBrushColorsAsync(
        FrameworkElement themeRoot,
        FrameworkElement control,
        ContentPresenter label,
        ElementTheme theme,
        string ownerName)
    {
        themeRoot.RequestedTheme = theme;
        await WaitForAppliedThemeAsync(themeRoot, theme);
        control.UpdateLayout();

        return new[]
        {
            GetSolidBrushColor(label.Foreground, "label", theme, ownerName),
        };
    }

    private static bool TryGetNamedDescendant<T>(FrameworkElement root, string name)
        where T : class
    {
        try
        {
            _ = GetTemplatePart<T>(root, name, "probe");
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static T? FindDescendant<T>(DependencyObject root)
        where T : DependencyObject
    {
        if (root is T match)
        {
            return match;
        }

        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var found = FindDescendant<T>(VisualTreeHelper.GetChild(root, index));
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private static T? FindAncestor<T>(DependencyObject start)
        where T : DependencyObject
    {
        var current = VisualTreeHelper.GetParent(start);
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static void BringControlIntoView(FrameworkElement control)
    {
        var viewer = FindAncestor<ScrollViewer>(control);
        if (viewer?.Content is UIElement content)
        {
            var origin = control.TransformToVisual(content).TransformPoint(new Windows.Foundation.Point(0, 0));
            viewer.ChangeView(null, Math.Max(0, origin.Y), null, disableAnimation: true);
            viewer.UpdateLayout();
        }

        control.StartBringIntoView(new BringIntoViewOptions { AnimationDesired = false });
        control.UpdateLayout();
    }
}
