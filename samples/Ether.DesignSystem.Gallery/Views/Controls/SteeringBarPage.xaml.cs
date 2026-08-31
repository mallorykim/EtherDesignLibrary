using Ether.DesignSystem.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace EtherSandbox.Views.Controls;

public sealed partial class SteeringBarPage : Page
{
    private const int MinStopCount = 2;
    private const int MaxStopCount = 8;
    private int _stopCount = 5;

    public string SpecimenXaml { get; } =
        """
        <controls:EtherSteeringBar Title="Playback" Value="31" />
        """;

    public SteeringBarPage()
    {
        this.InitializeComponent();
        DefaultPreview.PreviewStatus = SteeringBarPreviewStatus.Default;
        HoverPreview.PreviewStatus = SteeringBarPreviewStatus.Hover;
        PressedPreview.PreviewStatus = SteeringBarPreviewStatus.Pressed;
        DisabledPreview.PreviewStatus = SteeringBarPreviewStatus.Disabled;
        this.Loaded += (_, _) => ApplyStopCount();
    }

    private void DecreaseStops_Click(object sender, RoutedEventArgs e)
    {
        if (_stopCount <= MinStopCount)
            return;

        _stopCount--;
        ApplyStopCount();
    }

    private void IncreaseStops_Click(object sender, RoutedEventArgs e)
    {
        if (_stopCount >= MaxStopCount)
            return;

        _stopCount++;
        ApplyStopCount();
    }

    private void ApplyStopCount()
    {
        if (StopsDemo is null || StopCountText is null)
            return;

        var stops = new DoubleCollection();
        for (var i = 0; i < _stopCount; i++)
            stops.Add(_stopCount == 1 ? 0d : 100d * i / (_stopCount - 1));

        StopsDemo.Stops = stops;
        StopCountText.Text = _stopCount == 1
            ? GalleryStrings.Get("GalleryOutput.StopSingular", "1 stop")
            : GalleryStrings.Format("GalleryOutput.StopPlural", "{0} stops", _stopCount);

        if (DecreaseStopsButton is not null)
            DecreaseStopsButton.IsEnabled = _stopCount > MinStopCount;
        if (IncreaseStopsButton is not null)
            IncreaseStopsButton.IsEnabled = _stopCount < MaxStopCount;
    }

    private void InteractiveSteeringBar_ValueChanged(object sender, SteeringBarValueChangedEventArgs e)
    {
        if (LiveExample is not null)
            LiveExample.OutputText = GalleryStrings.Format("GalleryOutput.Value", "Value: {0}", (int)System.Math.Round(e.NewValue));
    }

}
