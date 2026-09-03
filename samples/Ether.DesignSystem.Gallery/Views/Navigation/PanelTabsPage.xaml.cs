using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Views.Navigation;

public sealed partial class PanelTabsPage : Page
{
    public string SpecimenXaml { get; } =
        """
        <controls:EtherSegmentedControl Style="{StaticResource EtherPanelTabs}">
            <controls:EtherSegmentPanel>
                <controls:EtherSegmentRadioButton Style="{StaticResource EtherPanelTabSegment}"
                             GroupName="Panel" Content="Overview" IsChecked="True"/>
                <controls:EtherSegmentRadioButton Style="{StaticResource EtherPanelTabSegment}"
                             GroupName="Panel" Content="Details"/>
            </controls:EtherSegmentPanel>
        </controls:EtherSegmentedControl>
        """;

    public PanelTabsPage()
    {
        this.InitializeComponent();
    }
}
