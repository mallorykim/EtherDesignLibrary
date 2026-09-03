using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Views.Surfaces;

public sealed partial class TooltipPage : Page
{
    public string SpecimenXaml { get; } =
        """
        <Border Style="{StaticResource EtherTooltip}">
            <TextBlock Text="Tooltip"
                       Style="{StaticResource body/s-regular}"
                       Foreground="{ThemeResource EtherTooltipForegroundBrush}"
                       TextWrapping="Wrap"/>
        </Border>
        """;

    public TooltipPage()
    {
        this.InitializeComponent();
    }
}
