using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace EtherSandbox.Controls;

/// <summary>
/// EtherSlider — bar-chart style slider UserControl.
/// Set Value (0–100) to control which bars are highlighted.
/// The control renders 12 bars of increasing height; bars up to and including
/// the active index are highlighted with the ActionPrimaryBg color.
/// </summary>
public sealed partial class EtherSlider : UserControl
{
    private const int BarCount = 63;
    private const double BarWidth = 2;
    private const double BarGap = 3;
    private const double BarHeight = 40;
    private const double KnobWidth = 4;
    private const double KnobHeight = 45;
    private const double KnobCornerRadius = 2;

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value), typeof(double), typeof(EtherSlider),
            new PropertyMetadata(50.0, OnValueChanged));

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, Math.Clamp(value, 0, 100));
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EtherSlider s) s.RenderBars();
    }

    public string FormatValue(double value) => ((int)Math.Round(value)).ToString();

    public EtherSlider()
    {
        this.InitializeComponent();
        RefreshThemeBrushes();
        this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
        // RenderBars() recreates its own local highlight/inactive brushes from the current
        // theme every call, and RefreshThemeBrushes() mutates the long-lived knob brushes in
        // place - together these keep every color on this control in sync when the user
        // toggles Light/Dark at runtime (RootGrid.RequestedTheme in MainWindow.xaml.cs).
        this.ActualThemeChanged += (_, _) =>
        {
            RefreshThemeBrushes();
            RenderBars();
        };
        this.Loaded += (_, _) => RenderBars();
    }

    /// <summary>Fixed defect: the pressed-knob color was previously hardcoded to #0468DA, which
    /// does not match the design system's actual ActionPrimaryBgPressed token (#0054E5). Both
    /// brushes now read their color from the token, with the pre-existing hardcoded values kept
    /// only as a fallback for the (should-never-happen) case the token is missing.</summary>
    private void RefreshThemeBrushes()
    {
        _knobNormalBrush.Color = GetThemeColor("ActionPrimaryBg", Color.FromArgb(0xFF, 0x0D, 0x62, 0xFF));
        _knobPressedBrush.Color = GetThemeColor("ActionPrimaryBgPressed", Color.FromArgb(0xFF, 0x00, 0x54, 0xE5));
    }

    private void RenderBars()
    {
        if (BarCanvas is not Canvas canvas) return;

        canvas.Children.Clear();

        int highlightedCount = (int)Math.Round(Value / 100.0 * BarCount);
        highlightedCount = Math.Clamp(highlightedCount, 0, BarCount);
        int activeIndex = highlightedCount - 1;
        double segmentStep = BarWidth + BarGap;
        double totalWidth = BarCount * segmentStep + KnobWidth;
        double canvasHeight = KnobHeight;
        canvas.Width = totalWidth;
        canvas.Height = canvasHeight;

        // Resolved theme colors — re-resolved on every call (Value change or theme change),
        // so these two always reflect the current ActualTheme.
        var highlightBrush = new SolidColorBrush(GetThemeColor("ActionPrimaryBg", Color.FromArgb(0xFF, 0x0D, 0x62, 0xFF)));
        var inactiveBrush = new SolidColorBrush(GetThemeColor("BackgroundTrack", Color.FromArgb(0xFF, 0xBE, 0xCC, 0xD7)));

        double barTop = (canvasHeight - BarHeight) / 2.0;

        // Highlighted bars (left of the knob)
        for (int i = 0; i <= activeIndex; i++)
        {
            var rect = new Rectangle
            {
                Width = BarWidth,
                Height = BarHeight,
                Fill = highlightBrush
            };
            Canvas.SetLeft(rect, i * segmentStep);
            Canvas.SetTop(rect, barTop);
            canvas.Children.Add(rect);
        }

        // Knob — sits at the boundary between highlighted and unselected bars
        double knobX = highlightedCount * segmentStep;

        if (ValueText is not null)
        {
            // Center the label above the knob, but clamp to the slider edges.
            ValueText.Text = FormatValue(Value);
            ValueText.Margin = new Thickness(0, 0, 0, 8);
            ValueText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double labelWidth = ValueText.DesiredSize.Width;
            double labelCenter = knobX + KnobWidth / 2.0;
            double labelLeft = labelCenter - labelWidth / 2.0;
            if (labelLeft < 0) labelLeft = 0;
            else if (labelLeft + labelWidth > totalWidth) labelLeft = totalWidth - labelWidth;
            ValueText.Margin = new Thickness(labelLeft, 0, 0, 8);
        }

        // Build the knob; its Width/Radius expand on hover.
        _knob = new Rectangle
        {
            Width = KnobWidth,
            Height = KnobHeight,
            RadiusX = KnobCornerRadius,
            RadiusY = KnobCornerRadius,
            Fill = _knobNormalBrush
        };
        _knobBaseX = knobX;
        UpdateKnobVisual();
        Canvas.SetTop(_knob, 0);
        canvas.Children.Add(_knob);

        // Unselected bars (right of the knob)
        int unselectedCount = BarCount - highlightedCount;
        double firstUnselectedX = knobX + KnobWidth + BarGap;
        for (int i = 0; i < unselectedCount; i++)
        {
            var rect = new Rectangle
            {
                Width = BarWidth,
                Height = BarHeight,
                Fill = inactiveBrush
            };
            Canvas.SetLeft(rect, firstUnselectedX + i * segmentStep);
            Canvas.SetTop(rect, barTop);
            canvas.Children.Add(rect);
        }

        // Pointer interaction on the canvas
        canvas.PointerPressed -= Canvas_PointerPressed;
        canvas.PointerMoved -= Canvas_PointerMoved;
        canvas.PointerReleased -= Canvas_PointerReleased;
        canvas.PointerCaptureLost -= Canvas_PointerCaptureLost;
        canvas.PointerExited -= Canvas_PointerExited;
        canvas.PointerPressed += Canvas_PointerPressed;
        canvas.PointerMoved += Canvas_PointerMoved;
        canvas.PointerReleased += Canvas_PointerReleased;
        canvas.PointerCaptureLost += Canvas_PointerCaptureLost;
        canvas.PointerExited += Canvas_PointerExited;
    }

    private bool _dragging;
    private Rectangle? _knob;
    private bool _hoveringKnob;
    private bool _pressedKnob;
    private double _knobBaseX;
    private readonly SolidColorBrush _knobNormalBrush = new(Colors.Transparent);
    private readonly SolidColorBrush _knobPressedBrush = new(Colors.Transparent);

    private void Canvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _dragging = true;
        _pressedKnob = true;
        (sender as Canvas)?.CapturePointer(e.Pointer);
        UpdateValueFromPointer(e);
        if (_knob is not null) UpdateKnobVisual();
    }

    private void Canvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_dragging) UpdateValueFromPointer(e);
        UpdateKnobHoverState(e);
    }

    private void Canvas_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _dragging = false;
        _pressedKnob = false;
        (sender as Canvas)?.ReleasePointerCapture(e.Pointer);
        if (_knob is not null) UpdateKnobVisual();
    }

    private void Canvas_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        _dragging = false;
        _pressedKnob = false;
        if (_knob is not null) UpdateKnobVisual();
    }

    private void Canvas_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _hoveringKnob = false;
        if (_knob is not null) UpdateKnobVisual();
    }

    private void UpdateKnobHoverState(PointerRoutedEventArgs e)
    {
        if (_knob is null || BarCanvas is not Canvas canvas) return;
        var pt = e.GetCurrentPoint(canvas).Position;
        double left = Canvas.GetLeft(_knob);
        double right = left + _knob.Width;
        bool isOver = pt.X >= left && pt.X <= right &&
                     pt.Y >= 0 && pt.Y <= KnobHeight;
        if (isOver != _hoveringKnob)
        {
            _hoveringKnob = isOver;
            UpdateKnobVisual();
        }
    }

    private void UpdateKnobVisual()
    {
        if (_knob is null) return;
        double w = (_hoveringKnob || _pressedKnob) ? 6.0 : KnobWidth;
        double r = (_hoveringKnob || _pressedKnob) ? 4.0 : KnobCornerRadius;
        _knob.Width = w;
        _knob.Height = KnobHeight;
        _knob.RadiusX = r;
        _knob.RadiusY = r;
        _knob.Fill = _pressedKnob ? _knobPressedBrush : _knobNormalBrush;

        // Keep the expanded knob fully inside the slider track.
        double desiredLeft = _knobBaseX + KnobWidth / 2.0 - w / 2.0;
        double totalWidth = BarCount * (BarWidth + BarGap) + KnobWidth;
        double clampedLeft = Math.Max(0, Math.Min(desiredLeft, totalWidth - w));
        Canvas.SetLeft(_knob, clampedLeft);
    }

    private void UpdateValueFromPointer(PointerRoutedEventArgs e)
    {
        if (BarCanvas is not Canvas canvas) return;
        var pt = e.GetCurrentPoint(canvas).Position;
        double segmentStep = BarWidth + BarGap;
        int slot = (int)Math.Round((pt.X - BarWidth / 2.0) / segmentStep);
        slot = Math.Clamp(slot, 0, BarCount);
        double rawValue = slot / (double)BarCount * 100.0;
        Value = (int)Math.Round(rawValue);
    }

    /// <summary>
    /// Resolves a token's Color for the element's CURRENT effective theme, keyed by
    /// ActualTheme rather than doing a flat Application.Current.Resources lookup (which is
    /// not guaranteed to reflect a local RequestedTheme override like RootGrid.RequestedTheme
    /// in MainWindow.xaml.cs).
    /// The design-system tokens now live inside a master Themes/Generic.xaml dictionary that is
    /// itself merged into Application.Resources, so this helper recurses through the merged
    /// dictionary tree until it finds a matching theme dictionary and resource key.
    /// ActualTheme is always resolved to Light or Dark (never ElementTheme.Default), so only
    /// those two branches are needed. High Contrast is not handled here — this app's runtime
    /// theme toggle only switches Light/Dark, and ActualTheme cannot report High Contrast the
    /// way the declarative {ThemeResource} markup extension can; out of scope for this fix.
    /// </summary>
    private Color GetThemeColor(string resourceKey, Color fallback)
    {
        var themeKey = ActualTheme == ElementTheme.Dark ? "Dark" : "Light";

        if (FindThemeBrush(Application.Current.Resources, themeKey, resourceKey) is { } brush)
            return brush.Color;

        return fallback;
    }

    private static SolidColorBrush? FindThemeBrush(ResourceDictionary root, string themeKey, string resourceKey)
    {
        if (root.ThemeDictionaries.TryGetValue(themeKey, out var dictObj) &&
            dictObj is ResourceDictionary themeDict &&
            themeDict.TryGetValue(resourceKey, out var res) &&
            res is SolidColorBrush b)
        {
            return b;
        }

        foreach (var merged in root.MergedDictionaries)
        {
            if (FindThemeBrush(merged, themeKey, resourceKey) is { } found)
                return found;
        }

        return null;
    }
}
