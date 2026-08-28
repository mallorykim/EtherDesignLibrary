using System.Globalization;
using EtherSandbox.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace EtherSandbox.Views.Controls;

public sealed partial class SliderPage : Page
{
    public static readonly DependencyProperty ContinuousLevelProperty = DependencyProperty.Register(
        nameof(ContinuousLevel),
        typeof(double),
        typeof(SliderPage),
        new PropertyMetadata(50d, OnPayloadChanged));

    public static readonly DependencyProperty NamedLevelProperty = DependencyProperty.Register(
        nameof(NamedLevel),
        typeof(double),
        typeof(SliderPage),
        new PropertyMetadata(50d, OnPayloadChanged));

    public string SpecimenXaml { get; } =
        """
        <!-- These two binds are live on this page. Serialize the doubles. -->
        <controls:EtherSlider Title="Title" Value="{x:Bind ContinuousLevel, Mode=TwoWay}" />

        <controls:EtherSlider Title="Title" Value="{x:Bind NamedLevel, Mode=TwoWay}" SnapToStops="True">
            <x:String>Off</x:String>
            <x:String>Low</x:String>
            <x:String>Mid</x:String>
            <x:String>High</x:String>
            <x:String>Max</x:String>
        </controls:EtherSlider>
        """;

    public double ContinuousLevel
    {
        get => (double)GetValue(ContinuousLevelProperty);
        set => SetValue(ContinuousLevelProperty, value);
    }

    public double NamedLevel
    {
        get => (double)GetValue(NamedLevelProperty);
        set => SetValue(NamedLevelProperty, value);
    }

    public SliderPage()
    {
        this.InitializeComponent();
        RefreshPayloadOutput();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        ExplicitStopsSlider.Stops = new DoubleCollection { 0, 10, 50, 90, 100 };
        RefreshPayloadOutput();
    }

    private static void OnPayloadChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((SliderPage)d).RefreshPayloadOutput();

    private void RefreshPayloadOutput()
    {
        if (LiveExample is null)
            return;

        var continuous = FormatNumber(ContinuousLevel);
        var named = FormatNumber(NamedLevel);
        var caption = CaptionFor(InteractiveNamedSlider, NamedLevel);
        LiveExample.OutputText = caption is null
            ? $"{{\"continuous\": {continuous}, \"named\": {named}}}"
            : $"{{\"continuous\": {continuous}, \"named\": {named}}}  (caption “{caption}” is not sent)";
    }

    private static string FormatNumber(double value)
        => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string? CaptionFor(EtherSlider? slider, double value)
    {
        if (slider?.Labels is not { Count: > 0 } labels)
            return null;

        var count = Math.Min(labels.Count, EtherSlider.MaxLabelCount);
        var span = slider.Maximum - slider.Minimum;
        var t = span <= 0d ? 0d : (value - slider.Minimum) / span;
        var index = Math.Clamp((int)Math.Round(t * (count - 1)), 0, count - 1);
        return labels[index];
    }
}
