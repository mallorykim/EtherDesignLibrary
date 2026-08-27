using EtherSandbox.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Views.Controls;

public sealed partial class RadioButtonPage : Page
{
    public string SpecimenXaml { get; } =
        """
        <controls:EtherRadioButton GroupName="Interactive" Content="Option A" IsChecked="True" />
        <controls:EtherRadioButton GroupName="Interactive" Content="Option B" />
        <controls:EtherRadioButton GroupName="Interactive" Content="Option C" />
        """;

    public RadioButtonPage()
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

    private void InteractiveRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (LiveExample is not null && sender is EtherRadioButton radio)
            LiveExample.OutputText = GalleryStrings.Format("GalleryOutput.Selected", "Selected: {0}", radio.Content);
    }
}
