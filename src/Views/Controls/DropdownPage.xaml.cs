using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Views.Controls;

public sealed partial class DropdownPage : Page
{
    public DropdownPage()
    {
        this.InitializeComponent();
        this.Loaded += (_, _) =>
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (DropdownHoverState is not null)
                    VisualStateManager.GoToState(DropdownHoverState, "PointerOver", false);
                if (DropdownPressedState is not null)
                    VisualStateManager.GoToState(DropdownPressedState, "Pressed", false);
                if (DropdownOpenState is not null)
                    VisualStateManager.GoToState(DropdownOpenState, "Opened", false);
            });
        };
    }
}
