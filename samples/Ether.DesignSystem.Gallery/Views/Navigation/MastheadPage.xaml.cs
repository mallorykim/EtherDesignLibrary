using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Views.Navigation;

public sealed partial class MastheadPage : Page
{
    public string SpecimenXaml { get; } =
        """
        <controls:EtherMasthead ShowSettings="False"
                                EnableWindowCommands="False" />
        """;

    public MastheadPage()
    {
        this.InitializeComponent();
        DefaultMasthead.PreviewIsMaximized = false;
        MaximizedMasthead.PreviewIsMaximized = true;
    }
}
