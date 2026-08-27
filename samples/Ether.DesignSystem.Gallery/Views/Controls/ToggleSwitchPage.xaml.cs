using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Views.Controls;

public sealed partial class ToggleSwitchPage : Page
{
    public string SpecimenXaml { get; } =
        """
        <ToggleSwitch Style="{StaticResource EtherSwitch}"
                      OffContent="Off"
                      OnContent="On" />
        """;

    public ToggleSwitchPage()
    {
        this.InitializeComponent();
        this.Loaded += (_, _) =>
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (OffHoverState is not null)
                    VisualStateManager.GoToState(OffHoverState, "PointerOver", false);
                if (OffPressedState is not null)
                    VisualStateManager.GoToState(OffPressedState, "Pressed", false);
                if (OnHoverState is not null)
                    VisualStateManager.GoToState(OnHoverState, "PointerOver", false);
                if (OnPressedState is not null)
                    VisualStateManager.GoToState(OnPressedState, "Pressed", false);
            });
        };
    }

    private void InteractiveSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (LiveExample is not null && sender is ToggleSwitch toggle)
            LiveExample.OutputText = toggle.IsOn
                ? GalleryStrings.Get("GalleryOutput.StateOn", "State: On")
                : GalleryStrings.Get("GalleryOutput.StateOff", "State: Off");
    }
}
