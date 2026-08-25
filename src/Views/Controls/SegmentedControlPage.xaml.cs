using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Views.Controls;

public sealed partial class SegmentedControlPage : Page
{
    // The STATES preview swatches force their look via HandRadioButton.PreviewState in XAML,
    // so no code-behind visual-state poking is needed here.
    public SegmentedControlPage()
    {
        this.InitializeComponent();
    }
}
