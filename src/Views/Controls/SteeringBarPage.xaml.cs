using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace EtherSandbox.Views.Controls;

public sealed partial class SteeringBarPage : Page
{
    private const int MinStopCount = 2;
    private const int MaxStopCount = 8;
    private int _stopCount = 5;
    public SteeringBarPage()
    {
        this.InitializeComponent();
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
        StopCountText.Text = _stopCount == 1 ? "1 stop" : $"{_stopCount} stops";

        if (DecreaseStopsButton is not null)
            DecreaseStopsButton.IsEnabled = _stopCount > MinStopCount;
        if (IncreaseStopsButton is not null)
            IncreaseStopsButton.IsEnabled = _stopCount < MaxStopCount;
    }

}
