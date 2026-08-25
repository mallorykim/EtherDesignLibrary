using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Views.Controls;

public sealed partial class CheckboxPage : Page
{
    public CheckboxPage()
    {
        this.InitializeComponent();
        this.Loaded += (_, _) =>
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (UncheckedHover is not null)
                    VisualStateManager.GoToState(UncheckedHover, "PointerOver", false);
                if (UncheckedPressed is not null)
                    VisualStateManager.GoToState(UncheckedPressed, "Pressed", false);
                if (CheckedHover is not null)
                    VisualStateManager.GoToState(CheckedHover, "PointerOver", false);
            });
        };
    }
}
