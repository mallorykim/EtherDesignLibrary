using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Views.Controls;

public sealed partial class IntelligenceButtonPage : Page
{
    public IntelligenceButtonPage()
    {
        this.InitializeComponent();
        this.Loaded += (_, _) =>
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (IntelligenceHoverState is not null)
                    VisualStateManager.GoToState(IntelligenceHoverState, "PointerOver", false);
                if (IntelligencePressedState is not null)
                    VisualStateManager.GoToState(IntelligencePressedState, "Pressed", false);
            });
        };
    }
}
