using EtherSandbox.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Views.Controls;

public sealed partial class CheckboxPage : Page
{
    public string SpecimenXaml { get; } =
        """
        <controls:EtherCheckbox Content="Option" />
        """;

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
                if (CheckedPressed is not null)
                    VisualStateManager.GoToState(CheckedPressed, "Pressed", false);
            });
        };
    }

    private void InteractiveCheckbox_Changed(object sender, RoutedEventArgs e)
    {
        if (LiveExample is not null && sender is EtherCheckbox checkbox)
            LiveExample.OutputText = GalleryStrings.Format("GalleryOutput.Checked", "Checked: {0}", checkbox.IsChecked ?? false);
    }
}
