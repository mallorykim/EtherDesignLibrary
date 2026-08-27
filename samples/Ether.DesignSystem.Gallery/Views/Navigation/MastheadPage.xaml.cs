using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Views.Navigation;

public sealed partial class MastheadPage : Page
{
    public string SpecimenXaml { get; } =
        """
        <controls:EtherMasthead ShowSettings="False"
                                PreviewIsMaximized="False"
                                EnableWindowCommands="False" />
        """;

    public MastheadPage()
    {
        this.InitializeComponent();
    }
}
