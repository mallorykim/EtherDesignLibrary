using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Views.Controls;

public sealed partial class InputPage : Page
{
    public string SpecimenXaml { get; } =
        """
        <controls:EtherInput PlaceholderText="Enter text" Width="280" />
        """;

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

    private void InteractiveInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (LiveExample is not null && sender is TextBox input)
            LiveExample.OutputText = string.IsNullOrEmpty(input.Text) ? "Text: —" : $"Text: {input.Text}";
    }
}
