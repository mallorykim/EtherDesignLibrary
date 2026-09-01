using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Views.Navigation;

public sealed partial class TabNavigationPage : Page
{
    public string SpecimenXaml { get; } =
        """
        <controls:EtherTabNavigation SelectedIndex="0" SelectionChanged="TabNavigation_SelectionChanged">
            <controls:EtherTabItem Content="Overview" Icon="Home"/>
            <controls:EtherTabItem Content="Details" Icon="Document"/>
            <controls:EtherTabItem Content="History" Icon="Clock"/>
        </controls:EtherTabNavigation>
        """;

    public TabNavigationPage()
    {
        InitializeComponent();
    }

    private void TabNavigation_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView { SelectedItem: Ether.DesignSystem.Controls.EtherTabItem { Content: { } content } })
            LiveExample.OutputText = GalleryStrings.Format("GalleryOutput.Selected", "Selected: {0}", content);
    }
}
