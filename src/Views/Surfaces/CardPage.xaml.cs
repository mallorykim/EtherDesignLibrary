using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Views.Surfaces;

public sealed partial class CardPage : Page
{
    public string SpecimenXaml { get; } =
        """
        <Border Style="{StaticResource EtherCardNormal}">
            <Border Style="{StaticResource EtherCardNormalBody}">
                <TextBlock Text="Card" />
            </Border>
        </Border>
        """;

    public CardPage()
    {
        this.InitializeComponent();
    }
}
