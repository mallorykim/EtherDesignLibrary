using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Views.DataDisplay;

public sealed partial class ProgressBarPage : Page
{
    private DispatcherTimer? _timer;
    private readonly Random _rng = new();
    private double _rate;   // simulated throughput, percent per second

    public string SpecimenXaml { get; } =
        """
        <controls:EtherProgressBar Title="Downloading"
                                   ValueContent="0%"
                                   Maximum="100"
                                   Value="0" />
        """;

    public ProgressBarPage()
    {
        this.InitializeComponent();

        // The value label tracks Value, so it counts up in step with the fill.
        SimBar.ValueChanged += (_, e) =>
        {
            var text = $"{(int)Math.Round(e.NewValue)}%";
            SimBar.ValueContent = text;
            if (LiveExample is not null)
                LiveExample.OutputText = $"Value: {text}";
        };

        // Run once on open so the motion is visible without touching anything.
        SimBar.Loaded += (_, _) => Play();
    }

    /// <summary>
    /// Advances the bar from a simulated data stream rather than a fixed-length sweep: a
    /// throughput that random-walks each tick (so the speed rises and dips like a real
    /// transfer) is integrated into Value. The bar therefore moves at the data's pace, not a
    /// canned duration — a slow stretch crawls, a fast one races.
    /// </summary>
    private void Play()
    {
        _timer?.Stop();
        SimBar.Value = 0;
        _rate = 30;   // starting throughput (= baseline)

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
        _timer.Tick += OnTick;
        _timer.Start();
    }

    private void OnTick(object? sender, object e)
    {
        const double dt = 0.02;         // seconds per tick
        const double baseline = 30;     // average throughput, percent per second

        // Mean-reverting random walk: the rate wanders (visible fast / slow stretches) but is
        // pulled back toward the baseline, so it never sticks at a crawl and still fills in a
        // few seconds — the motion follows the "data", not a fixed duration.
        _rate += (baseline - _rate) * 0.06 + (_rng.NextDouble() - 0.5) * 9;
        _rate = Math.Clamp(_rate, 6, 62);

        var next = SimBar.Value + _rate * dt;
        if (next >= SimBar.Maximum)
        {
            next = SimBar.Maximum;
            _timer?.Stop();
        }
        SimBar.Value = next;
    }

    private void Replay_Click(object sender, RoutedEventArgs e) => Play();
}
