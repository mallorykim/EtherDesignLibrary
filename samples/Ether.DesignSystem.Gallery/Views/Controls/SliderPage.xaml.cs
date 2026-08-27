using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace EtherSandbox.Views.Controls;

public sealed partial class SliderPage : Page
{
    public string SpecimenXaml { get; } =
        """
        <controls:EtherSlider Value="50" />
        """;

    public SliderPage()
    {
        this.InitializeComponent();
    }

    private void InteractiveSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (LiveExample is not null)
            LiveExample.OutputText = $"Value: {(int)System.Math.Round(e.NewValue)}%";
    }
}
