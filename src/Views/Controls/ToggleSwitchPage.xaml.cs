using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Views.Controls;

public sealed partial class ToggleSwitchPage : Page
{
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
}
