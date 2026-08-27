using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Views.Controls;

public sealed partial class DropdownPage : Page
{
    public string SpecimenXaml { get; } =
        """
        <controls:EtherDropdown SelectedIndex="0">
            <ComboBoxItem Content="10 Minutes"/>
            <ComboBoxItem Content="30 Minutes"/>
            <ComboBoxItem Content="1 Hour"/>
        </controls:EtherDropdown>
        """;

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

    private void InteractiveDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LiveExample is null)
            return;

        var content = (sender as ComboBox)?.SelectedItem is ComboBoxItem item
            ? item.Content?.ToString()
            : null;
        LiveExample.OutputText = string.IsNullOrEmpty(content) ? "Selected: —" : $"Selected: {content}";
    }
}
