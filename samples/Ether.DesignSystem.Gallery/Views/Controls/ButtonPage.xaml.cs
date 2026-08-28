using EtherSandbox.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

namespace EtherSandbox.Views.Controls;

public sealed partial class ButtonPage : Page
{
    public string SpecimenXaml { get; } =
        """
        <controls:EtherButton Content="Button"
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
        button.LeftIcon = showLeft ? CreateChevron(ChevronLeftPathData, size) : null;
        button.RightIcon = showRight ? CreateChevron(ChevronRightPathData, size) : null;
    }

    // Path markup matches the Figma button icon instances (node 62075:5625) exactly:
    // the glyph sits inside a 20-unit box with built-in padding (glyph ≈ 13.4 tall),
    // NOT the full-bleed 16-grid geometry in EtherIconGeometries.xaml (frozen tokens).
    // ChevronRight is ChevronLeft mirrored about x=10. Each PathIcon needs its OWN
    // Geometry instance: sharing one Geometry/Style across simultaneously-rendered
    // PathIcon siblings renders only the first (see IconsPage). Geometry does not
    // round-trip through ToString(), so raw path data + XamlReader.Load per slot.
    private const string ChevronLeftPathData =
        "M5.80837 9.55815L12.0584 3.30815L12.9423 4.19204L7.1342 10.0001L12.9423 15.8082L12.0584 16.692L5.80837 10.442C5.56429 10.198 5.56429 9.80223 5.80837 9.55815Z";

    private const string ChevronRightPathData =
        "M14.19163 9.55815L7.9416 3.30815L7.0577 4.19204L12.8658 10.0001L7.0577 15.8082L7.9416 16.692L14.19163 10.442C14.43571 10.198 14.43571 9.80223 14.19163 9.55815Z";

    private static PathIcon CreateChevron(string pathData, double size)
    {
        var geometry = (Geometry)XamlReader.Load(
            $"<Geometry xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">{pathData}</Geometry>");

        // PathIcon renders path units 1:1 (it does not stretch to Width/Height), so scale
        // the geometry itself from the 20-unit Figma box to the target size: 20EPX on
        // Default (large), 16EPX on Small.
        if (size != 20)
        {
            var scale = size / 20d;
            geometry.Transform = new ScaleTransform { ScaleX = scale, ScaleY = scale };
        }

        return new PathIcon { Data = geometry, Width = size, Height = size };
    }
}
