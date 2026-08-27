using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Views.Foundations;

public sealed partial class ScrollBarPage : Page
{
    public string SpecimenXaml { get; } =
        """
        <ScrollViewer VerticalScrollBarVisibility="Auto"
                      HorizontalScrollBarVisibility="Disabled">
            <StackPanel Padding="12" Spacing="12">
                <TextBlock Text="Battery" />
                <TextBlock Text="Display" />
            </StackPanel>
        </ScrollViewer>
        """;

    public ScrollBarPage()
    {
        this.InitializeComponent();
    }
}
