using Ether.DesignSystem.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Views.Controls;

public sealed partial class SegmentedControlPage : Page
{
    public string SpecimenXaml { get; } =
        """
        <controls:EtherSegmentedControl SelectedValue="list">
            <controls:EtherSegmentPanel>
                <controls:EtherSegmentRadioButton GroupName="SegmentedTwo" Content="List" Tag="list"
                                         Style="{StaticResource EtherSegment}"
                                         IsChecked="True"/>
                <controls:EtherSegmentRadioButton GroupName="SegmentedTwo" Content="Grid" Tag="grid"
                                         Style="{StaticResource EtherSegment}"/>
            </controls:EtherSegmentPanel>
        </controls:EtherSegmentedControl>
        """;

    // The STATES preview swatches force their look via HandRadioButton.PreviewState in XAML
    // (HandRadioButton is a thin Gallery-only subclass of the shipped
    // Ether.DesignSystem.Controls.EtherSegmentRadioButton), so no code-behind visual-state
    // poking is needed here.
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
