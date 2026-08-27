using EtherSandbox.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Views.Controls;

public sealed partial class SegmentedControlPage : Page
{
    public string SpecimenXaml { get; } =
        """
        <controls:EtherSegmentedControl>
            <StackPanel Orientation="Horizontal" Spacing="4">
                <controls:HandRadioButton GroupName="SegmentedTwo" Content="List"
                                         Style="{StaticResource EtherSegment}"
                                         IsChecked="True"/>
                <controls:HandRadioButton GroupName="SegmentedTwo" Content="Grid"
                                         Style="{StaticResource EtherSegment}"/>
            </StackPanel>
        </controls:EtherSegmentedControl>
        """;

    // The STATES preview swatches force their look via HandRadioButton.PreviewState in XAML,
    // so no code-behind visual-state poking is needed here.
    public SegmentedControlPage()
    {
        this.InitializeComponent();
    }

    private void InteractiveSegment_Checked(object sender, RoutedEventArgs e)
    {
        if (LiveExample is not null && sender is HandRadioButton segment)
            LiveExample.OutputText = $"Selected: {segment.Content}";
    }
}
