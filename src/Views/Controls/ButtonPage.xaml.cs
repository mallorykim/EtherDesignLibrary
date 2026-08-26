using EtherSandbox.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace EtherSandbox.Views.Controls;

public sealed partial class ButtonPage : Page
{
    public ButtonPage()
    {
        this.InitializeComponent();
        this.Loaded += (_, _) =>
        {
            // Apply the initial icon state from the checkbox (defaults to checked/with-icon).
            ApplyIcons(IconToggle?.IsChecked ?? false);

            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (PrimaryHoverState is not null)
                    VisualStateManager.GoToState(PrimaryHoverState, "PointerOver", false);
                if (PrimaryPressedState is not null)
                    VisualStateManager.GoToState(PrimaryPressedState, "Pressed", false);
                if (PrimarySmallHoverState is not null)
                    VisualStateManager.GoToState(PrimarySmallHoverState, "PointerOver", false);
                if (PrimarySmallPressedState is not null)
                    VisualStateManager.GoToState(PrimarySmallPressedState, "Pressed", false);
                if (SecondaryHoverState is not null)
                    VisualStateManager.GoToState(SecondaryHoverState, "PointerOver", false);
                if (SecondaryPressedState is not null)
                    VisualStateManager.GoToState(SecondaryPressedState, "Pressed", false);
                if (SecondarySmallHoverState is not null)
                    VisualStateManager.GoToState(SecondarySmallHoverState, "PointerOver", false);
                if (SecondarySmallPressedState is not null)
                    VisualStateManager.GoToState(SecondarySmallPressedState, "Pressed", false);
                if (TertiaryHoverState is not null)
                    VisualStateManager.GoToState(TertiaryHoverState, "PointerOver", false);
                if (TertiaryPressedState is not null)
                    VisualStateManager.GoToState(TertiaryPressedState, "Pressed", false);
                if (TertiarySmallHoverState is not null)
                    VisualStateManager.GoToState(TertiarySmallHoverState, "PointerOver", false);
                if (TertiarySmallPressedState is not null)
                    VisualStateManager.GoToState(TertiarySmallPressedState, "Pressed", false);
            });
        };
    }

    private void IconToggle_Changed(object sender, RoutedEventArgs e) =>
        ApplyIcons((sender as CheckBox)?.IsChecked ?? false);

    /// <summary>Adds a trailing chevron to every interactive specimen when
    /// <paramref name="show"/> is true, or clears it (text-only) when false.
    /// Demonstrates that the button icon is optional.</summary>
    private void ApplyIcons(bool show)
    {
        SetRightIcon(PrimaryLargeDemo, show, 10);
        SetRightIcon(PrimarySmallDemo, show, 8);
        SetRightIcon(SecondaryLargeDemo, show, 10);
        SetRightIcon(SecondarySmallDemo, show, 8);
        SetRightIcon(TertiaryLargeDemo, show, 10);
        SetRightIcon(TertiarySmallDemo, show, 8);
        SetRightIcon(DefaultStyleDemo, show, 10);
    }

    private static void SetRightIcon(EtherButton? button, bool show, double size)
    {
        if (button is null)
            return;

        // Foreground left unset so the icon inherits the slot's per-variant colour from
        // the control template (white on Primary, ink on Secondary/Tertiary).
        button.RightIcon = show
            ? new FontIcon
            {
                Glyph = "\uE72A", // ChevronRight (Segoe MDL2 Assets)
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = size,
            }
            : null;
    }
}
