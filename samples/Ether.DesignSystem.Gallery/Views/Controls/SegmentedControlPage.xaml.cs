using EtherSandbox.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Views.Controls;

public sealed partial class SegmentedControlPage : Page
{
    public string SpecimenXaml { get; } =
        """
        <controls:EtherSegmentedControl SelectedValue="list">
            <controls:EtherSegmentPanel>
                <controls:HandRadioButton GroupName="SegmentedTwo" Content="List" Tag="list"
                                         Style="{StaticResource EtherSegment}"
                                         IsChecked="True"/>
                <controls:HandRadioButton GroupName="SegmentedTwo" Content="Grid" Tag="grid"
                                         Style="{StaticResource EtherSegment}"/>
            </controls:EtherSegmentPanel>
        </controls:EtherSegmentedControl>
        """;

    // The STATES preview swatches force their look via HandRadioButton.PreviewState in XAML,
    // so no code-behind visual-state poking is needed here.
    public SegmentedControlPage()
    {
        this.InitializeComponent();
    }

    private void InteractiveSegmentedControl_SelectionChanged(object sender, SegmentedSelectionChangedEventArgs e)
    {
        if (LiveExample is not null)
            LiveExample.OutputText = GalleryStrings.Format("GalleryOutput.Selected", "Selected: {0}", e.NewValue ?? "(none)");
    }
}
