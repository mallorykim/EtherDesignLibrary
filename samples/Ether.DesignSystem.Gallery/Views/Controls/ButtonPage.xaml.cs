using System.Linq;
using EtherSandbox.Controls;
using EtherSandbox.Views.Foundations;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

namespace EtherSandbox.Views.Controls;

public sealed partial class ButtonPage : Page
{
    public string SpecimenXaml { get; } =
        """
        <controls:EtherButton Content="Primary Action"
                              Style="{StaticResource EtherButtonPrimary}" />
        """;

    public ButtonPage()
    {
        this.InitializeComponent();
        this.Loaded += (_, _) =>
        {
            ApplyIcons(
                LeftIconToggle?.IsChecked ?? false,
                RightIconToggle?.IsChecked ?? false);

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
        ApplyIcons(
            LeftIconToggle?.IsChecked ?? false,
            RightIconToggle?.IsChecked ?? false);

    private void InteractiveButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is EtherButton button && LiveExample is not null)
            LiveExample.OutputText = GalleryStrings.Format("GalleryOutput.Clicked", "Clicked: {0}", button.Content);
    }

    /// <summary>Sets independently optional leading and trailing icons on every live specimen.
    /// Either slot may be empty, so a pure-text button remains a first-class configuration.</summary>
    private void ApplyIcons(bool showLeft, bool showRight)
    {
        SetIcons(PrimaryLargeDemo, showLeft, showRight, 20);
        SetIcons(PrimarySmallDemo, showLeft, showRight, 16);
        SetIcons(SecondaryLargeDemo, showLeft, showRight, 20);
        SetIcons(SecondarySmallDemo, showLeft, showRight, 16);
        SetIcons(TertiaryLargeDemo, showLeft, showRight, 20);
        SetIcons(TertiarySmallDemo, showLeft, showRight, 16);
        SetIcons(DefaultStyleDemo, showLeft, showRight, 20);
    }

    private static void SetIcons(EtherButton? button, bool showLeft, bool showRight, double size)
    {
        if (button is null)
            return;

        // Foreground stays unset so each template supplies the appropriate Primary,
        // Secondary, or Tertiary colour. Callers may supply any IconElement instead.
        button.LeftIcon = showLeft ? CreateLibraryChevron("IconChevronLeft", size) : null;
        button.RightIcon = showRight ? CreateLibraryChevron("IconChevronRight", size) : null;
    }

    private static PathIcon CreateLibraryChevron(string resourceKey, double size)
    {
        var iconResources = MergedResourceDictionaries.Find(
            Application.Current.Resources, "EtherIconGeometries.xaml", resourceKey)
            ?? throw new InvalidOperationException($"Ether icon dictionary for '{resourceKey}' was not found.");
        if (iconResources[resourceKey] is not Style iconStyle)
            throw new InvalidOperationException($"Ether icon resource '{resourceKey}' was not found.");

        // The icon dictionary owns the geometry.  Do not attach the same Style
        // instance to several live PathIcons: WinUI can retain the Style's
        // Freezable Data value on the first consumer, leaving later buttons blank.
        // Clone the library geometry for each slot while keeping the source of
        // truth in EtherIconGeometries.xaml.
        var dataSetter = iconStyle.Setters
            .OfType<Setter>()
            .FirstOrDefault(setter => setter.Property == PathIcon.DataProperty);
        if (dataSetter?.Value is not Geometry geometry)
            throw new InvalidOperationException($"Ether icon resource '{resourceKey}' has no geometry data.");

        try
        {
            var clonedGeometry = (Geometry)XamlReader.Load(
                $"<Geometry xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">{geometry}</Geometry>");
            return new PathIcon { Data = clonedGeometry, Width = size, Height = size };
        }
        catch (Exception)
        {
            // Keep the Gallery usable if a platform build cannot round-trip the
            // Geometry representation; the library style remains the fallback.
            return new PathIcon { Style = iconStyle, Width = size, Height = size };
        }
    }
}
