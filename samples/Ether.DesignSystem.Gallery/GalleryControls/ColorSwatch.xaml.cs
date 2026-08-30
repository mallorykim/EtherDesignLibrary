using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Ether.DesignSystem.Controls;

public enum ColorSwatchDisplayMode
{
    Background,
    Foreground,
    Border
}

public enum ColorSwatchLayout
{
    Horizontal,
    Vertical
}

public sealed partial class ColorSwatch : UserControl
{
    public static readonly DependencyProperty KeyProperty =
        DependencyProperty.Register(nameof(Key), typeof(string), typeof(ColorSwatch), new PropertyMetadata(string.Empty, OnPropertyChanged));

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(ColorSwatch), new PropertyMetadata(string.Empty, OnPropertyChanged));

    public static readonly DependencyProperty DisplayModeProperty =
        DependencyProperty.Register(nameof(DisplayMode), typeof(ColorSwatchDisplayMode), typeof(ColorSwatch), new PropertyMetadata(ColorSwatchDisplayMode.Background, OnPropertyChanged));

    public static readonly DependencyProperty BackgroundKeyProperty =
        DependencyProperty.Register(nameof(BackgroundKey), typeof(string), typeof(ColorSwatch), new PropertyMetadata(null, OnPropertyChanged));

    public static readonly DependencyProperty ShowValueProperty =
        DependencyProperty.Register(nameof(ShowValue), typeof(bool), typeof(ColorSwatch), new PropertyMetadata(false, OnPropertyChanged));

    public static readonly DependencyProperty SampleBrushProperty =
        DependencyProperty.Register(nameof(SampleBrush), typeof(Brush), typeof(ColorSwatch), new PropertyMetadata(null, OnPropertyChanged));

    public static readonly DependencyProperty LayoutProperty =
        DependencyProperty.Register(nameof(Layout), typeof(ColorSwatchLayout), typeof(ColorSwatch), new PropertyMetadata(ColorSwatchLayout.Horizontal, OnPropertyChanged));

    public static readonly DependencyProperty ValueTextProperty =
        DependencyProperty.Register(nameof(ValueText), typeof(string), typeof(ColorSwatch), new PropertyMetadata(string.Empty, OnPropertyChanged));

    public string Key
    {
        get => (string)GetValue(KeyProperty);
        set => SetValue(KeyProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public ColorSwatchDisplayMode DisplayMode
    {
        get => (ColorSwatchDisplayMode)GetValue(DisplayModeProperty);
        set => SetValue(DisplayModeProperty, value);
    }

    public string? BackgroundKey
    {
        get => (string?)GetValue(BackgroundKeyProperty);
        set => SetValue(BackgroundKeyProperty, value);
    }

    public bool ShowValue
    {
        get => (bool)GetValue(ShowValueProperty);
        set => SetValue(ShowValueProperty, value);
    }

    public Brush? SampleBrush
    {
        get => (Brush?)GetValue(SampleBrushProperty);
        set => SetValue(SampleBrushProperty, value);
    }

    public ColorSwatchLayout Layout
    {
        get => (ColorSwatchLayout)GetValue(LayoutProperty);
        set => SetValue(LayoutProperty, value);
    }

    public string ValueText
    {
        get => (string)GetValue(ValueTextProperty);
        set => SetValue(ValueTextProperty, value);
    }

    public ColorSwatch()
    {
        this.InitializeComponent();
        Loaded += (_, _) => Refresh();
        ActualThemeChanged += (_, _) => Refresh();
    }

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ColorSwatch swatch)
            swatch.Refresh();
    }

    private void Refresh()
    {
        var key = Key ?? string.Empty;
        var mode = DisplayMode;
        var tokenBrush = SampleBrush ?? LookupBrush(key);
        var backgroundKey = mode == ColorSwatchDisplayMode.Background && SampleBrush is null ? key : (BackgroundKey ?? "BackgroundSurfaceRaised");
        var backgroundBrush = mode == ColorSwatchDisplayMode.Background && SampleBrush is not null
            ? tokenBrush
            : LookupBrush(backgroundKey) ?? new SolidColorBrush(Colors.Transparent);

        ApplyLayout();

        if (mode == ColorSwatchDisplayMode.Background)
        {
            SampleBorder.Background = tokenBrush;
            SampleBorder.BorderBrush = LookupBrush("BorderSubtle");
            SampleBorder.BorderThickness = new Thickness(1);
            SampleText.Visibility = Visibility.Collapsed;
        }
        else if (mode == ColorSwatchDisplayMode.Foreground)
        {
            SampleBorder.Background = backgroundBrush;
            SampleBorder.BorderBrush = LookupBrush("BorderSubtle");
            SampleBorder.BorderThickness = new Thickness(1);
            SampleText.Foreground = tokenBrush;
            SampleText.Visibility = Visibility.Visible;
        }
        else // Border
        {
            SampleBorder.Background = backgroundBrush;
            SampleBorder.BorderBrush = tokenBrush;
            SampleBorder.BorderThickness = new Thickness(3);
            SampleText.Visibility = Visibility.Collapsed;
        }

        LabelBlock.Text = string.IsNullOrEmpty(Label) ? key : Label;

        if (!string.IsNullOrEmpty(ValueText))
        {
            ValueBlock.Text = ValueText;
            ValueBlock.Visibility = Visibility.Visible;
        }
        else if (ShowValue)
        {
            ValueBlock.Text = tokenBrush switch
            {
                SolidColorBrush scb => $"#{scb.Color.A:X2}{scb.Color.R:X2}{scb.Color.G:X2}{scb.Color.B:X2}",
                LinearGradientBrush => "linear gradient",
                RadialGradientBrush => "radial gradient",
                _ => string.Empty,
            };
            ValueBlock.Visibility = string.IsNullOrEmpty(ValueBlock.Text) ? Visibility.Collapsed : Visibility.Visible;
        }
        else
        {
            ValueBlock.Visibility = Visibility.Collapsed;
        }
    }

    private void ApplyLayout()
    {
        if (Layout == ColorSwatchLayout.Vertical)
        {
            RootGrid.MaxWidth = double.PositiveInfinity;
            RootGrid.ColumnSpacing = 0;
            SampleColumn.Width = new GridLength(1, GridUnitType.Star);
            ContentColumn.Width = new GridLength(0);
            SampleRow.Height = GridLength.Auto;
            ContentRow.Height = GridLength.Auto;
            Grid.SetColumn(SampleBorder, 0);
            Grid.SetRow(SampleBorder, 0);
            Grid.SetColumn(ContentPanel, 0);
            Grid.SetRow(ContentPanel, 1);
            SampleBorder.Width = 56;
            SampleBorder.HorizontalAlignment = HorizontalAlignment.Left;
            SampleBorder.VerticalAlignment = VerticalAlignment.Top;
            ContentPanel.Width = 56;
            ContentPanel.Margin = new Thickness(0, 6, 0, 0);
            ContentPanel.HorizontalAlignment = HorizontalAlignment.Left;
            ContentPanel.VerticalAlignment = VerticalAlignment.Top;
        }
        else
        {
            RootGrid.MaxWidth = 560;
            RootGrid.ColumnSpacing = 12;
            SampleColumn.Width = new GridLength(56);
            ContentColumn.Width = new GridLength(1, GridUnitType.Star);
            SampleRow.Height = GridLength.Auto;
            ContentRow.Height = new GridLength(0);
            Grid.SetColumn(SampleBorder, 0);
            Grid.SetRow(SampleBorder, 0);
            Grid.SetColumn(ContentPanel, 1);
            Grid.SetRow(ContentPanel, 0);
            SampleBorder.ClearValue(WidthProperty);
            SampleBorder.HorizontalAlignment = HorizontalAlignment.Stretch;
            SampleBorder.VerticalAlignment = VerticalAlignment.Center;
            ContentPanel.ClearValue(WidthProperty);
            ContentPanel.Margin = new Thickness(0);
            ContentPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
            ContentPanel.VerticalAlignment = VerticalAlignment.Center;
        }
    }

    private Brush? LookupBrush(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        // Mode-invariant resources live outside of ThemeDictionaries; look them up directly.
        if (Application.Current.Resources.TryGetValue(key, out var directObj) && directObj is Brush directBrush)
            return directBrush;

        var themeKey = ActualTheme == ElementTheme.Dark ? "Dark" : "Light";
        return FindThemeBrush(Application.Current.Resources, themeKey, key);
    }

    private static Brush? FindThemeBrush(ResourceDictionary root, string themeKey, string resourceKey)
    {
        if (root.ThemeDictionaries.TryGetValue(themeKey, out var dictObj) &&
            dictObj is ResourceDictionary themeDict &&
            themeDict.TryGetValue(resourceKey, out var res) &&
            res is Brush brush)
        {
            return brush;
        }

        foreach (var merged in root.MergedDictionaries)
        {
            if (FindThemeBrush(merged, themeKey, resourceKey) is { } found)
                return found;
        }

        return null;
    }
}
