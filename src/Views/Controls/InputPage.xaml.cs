using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Views.Controls;

public sealed partial class InputPage : Page
{
    public InputPage()
    {
        this.InitializeComponent();

        // The state specimens are non-interactive (IsHitTestVisible/IsTabStop = False),
        // so the TextBox state machine never overrides these forced states. Same approach
        // as CheckboxPage: force after Loaded, on the dispatcher, so the template parts exist.
        this.Loaded += (_, _) =>
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (HoverInput is not null)
                    VisualStateManager.GoToState(HoverInput, "PointerOver", false);
                if (ActiveInput is not null)
                    VisualStateManager.GoToState(ActiveInput, "Focused", false);
            });
        };
    }
}
