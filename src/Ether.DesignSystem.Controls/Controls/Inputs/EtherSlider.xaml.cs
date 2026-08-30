using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.System;

namespace Ether.DesignSystem.Controls;

/// <summary>
/// Ether bar-chart slider: even-count ticks, a 4px knob, and click/drag range
/// interaction. The track stretches with the parent; tick count stays even
/// and follows Figma density (2 epx bar + 3 epx gap). Derives from
/// <see cref="RangeBase"/> so Minimum, Maximum, Value, and change steps share
/// the WinUI range contract.
/// </summary>
/// <remarks>
/// Tick bars are created to fill the stretched track. Code updates fill,
/// Canvas.Left, and knob hover/press sizing. Pointer capture stays on
/// BarCanvas rather than VisualStateManager. Component <c>EtherSlider*</c>
/// keys bind Light/Dark colors to primitive tokens. Title and tick labels are
/// optional; at most <see cref="MaxLabelCount"/> labels are shown. Tick labels may
/// be numbers or named values via <see cref="Labels"/> (also XAML content) and are
/// not the range payload. Pointer, keyboard, and automation input round to integers
/// unless <see cref="SnapToStops"/> is on.
/// </remarks>
[TemplatePart(Name = ValueTextPart, Type = typeof(TextBlock))]
[TemplatePart(Name = BarCanvasPart, Type = typeof(Canvas))]
[TemplatePart(Name = KnobPart, Type = typeof(Rectangle))]
[TemplatePart(Name = LabelRowPart, Type = typeof(Grid))]
[TemplatePart(Name = HighlightBrushSourcePart, Type = typeof(Border))]
[TemplatePart(Name = InactiveBrushSourcePart, Type = typeof(Border))]
[TemplatePart(Name = KnobBrushSourcePart, Type = typeof(Border))]
[TemplatePart(Name = KnobPressedBrushSourcePart, Type = typeof(Border))]
[ContentProperty(Name = nameof(Labels))]
public sealed partial class EtherSlider : RangeBase
{
    private const string ValueTextPart = "ValueText";
    private const string BarCanvasPart = "BarCanvas";
    private const string KnobPart = "Knob";
    private const string LabelRowPart = "LabelRow";
    private const string HighlightBrushSourcePart = "HighlightBrushSource";
    private const string InactiveBrushSourcePart = "InactiveBrushSource";
    private const string KnobBrushSourcePart = "KnobBrushSource";
    private const string KnobPressedBrushSourcePart = "KnobPressedBrushSource";

    private const int DefaultAutoLabelCount = 7;
    private const double DefaultTrackWidth = 330;
    private const double BarWidth = 2;
    private const double BarGap = 3;
    private const double BarHeight = 40;
    private const double KnobWidth = 4;
    private const double KnobHeight = 45;
    private const double KnobCornerRadius = 2;
    private const double KnobHoverWidth = 6;
    private const double KnobHoverCornerRadius = 4;

    private TextBlock? _valueText;
    private Canvas? _barCanvas;
    private Grid? _labelRow;
    private Border? _highlightBrushSource;
    private Border? _inactiveBrushSource;
    private Border? _knobBrushSource;
    private Border? _knobPressedBrushSource;
    private Rectangle? _knob;
    private Rectangle[] _bars = [];
    private TextBlock[] _labels = [];
    private bool _dragging;
    private bool _hoveringKnob;
    private bool _pressedKnob;
    private bool _keyboardFocused;
    private bool _coercingValue;
    private double _knobBaseX;
    private double _segmentStep = BarWidth + BarGap;
    private double _trackWidth = DefaultTrackWidth;
    private bool _updatingLayout;
    private bool _positioningLabels;
    private INotifyCollectionChanged? _labelsCollection;

    /// <summary>Fewest even tick bars rendered on a narrow track.</summary>
    public const int MinBarCount = 8;

    /// <summary>Most even tick bars rendered on a wide track.</summary>
    public const int MaxBarCount = 512;

    /// <summary>Maximum number of tick labels rendered under the track.</summary>
    public const int MaxLabelCount = 11;

    /// <summary>
    /// Initializes a new instance of the <see cref="EtherSlider"/> class.
    /// </summary>
    public EtherSlider()
    {
        DefaultStyleKey = typeof(EtherSlider);
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
        SetValue(LabelsProperty, new EtherSliderLabelCollection());

        Loaded += (_, _) => UpdateBarLayout();
        SizeChanged += (_, _) => UpdateBarLayout();
        RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => UpdateInteractionState());
        ActualThemeChanged += (_, _) => UpdateBarLayout();
        KeyDown += OnKeyDown;
        GotFocus += OnGotFocus;
        LostFocus += OnLostFocus;
    }

    /// <summary>Identifies the <see cref="ShowTitle"/> dependency property.</summary>
    public static readonly DependencyProperty ShowTitleProperty = DependencyProperty.Register(
        nameof(ShowTitle),
        typeof(bool),
        typeof(EtherSlider),
        new PropertyMetadata(false, OnChromeChanged));

    /// <summary>Identifies the <see cref="Title"/> dependency property.</summary>
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(EtherSlider),
        new PropertyMetadata(null, OnChromeChanged));

    /// <summary>Identifies the <see cref="ShowLabels"/> dependency property.</summary>
    public static readonly DependencyProperty ShowLabelsProperty = DependencyProperty.Register(
        nameof(ShowLabels),
        typeof(bool),
        typeof(EtherSlider),
        new PropertyMetadata(false, OnChromeChanged));

    /// <summary>Identifies the <see cref="Labels"/> dependency property.</summary>
    public static readonly DependencyProperty LabelsProperty = DependencyProperty.Register(
        nameof(Labels),
        typeof(EtherSliderLabelCollection),
        typeof(EtherSlider),
        new PropertyMetadata(null, OnLabelsChanged));

    /// <summary>Identifies the <see cref="Stops"/> dependency property.</summary>
    public static readonly DependencyProperty StopsProperty = DependencyProperty.Register(
        nameof(Stops),
        typeof(DoubleCollection),
        typeof(EtherSlider),
        new PropertyMetadata(null, OnChromeChanged));

    /// <summary>Identifies the <see cref="SnapToStops"/> dependency property.</summary>
    public static readonly DependencyProperty SnapToStopsProperty = DependencyProperty.Register(
        nameof(SnapToStops),
        typeof(bool),
        typeof(EtherSlider),
        new PropertyMetadata(false, OnChromeChanged));

    /// <summary>Gets or sets whether the title above the track is shown.</summary>
    public bool ShowTitle
    {
        get => (bool)GetValue(ShowTitleProperty);
        set => SetValue(ShowTitleProperty, value);
    }

    /// <summary>Gets or sets the title text. Hidden when null or empty even if <see cref="ShowTitle"/> is true.</summary>
    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Gets or sets whether tick labels under the track are shown.</summary>
    public bool ShowLabels
    {
        get => (bool)GetValue(ShowLabelsProperty);
        set => SetValue(ShowLabelsProperty, value);
    }

    /// <summary>
    /// Gets or sets tick-label strings under the track (numbers or named values).
    /// At most <see cref="MaxLabelCount"/> entries are rendered. When empty,
    /// even integer steps of Minimum–Maximum are generated (0–100 → 0, 20, 40, 60, 80, 100).
    /// Strings may also be provided as XAML content on the control. Labels are UI only;
    /// the payload is always <see cref="RangeBase.Value"/>.
    /// </summary>
    public EtherSliderLabelCollection? Labels
    {
        get => (EtherSliderLabelCollection?)GetValue(LabelsProperty);
        set => SetValue(LabelsProperty, value);
    }

    /// <summary>
    /// Gets or sets explicit stop values. When empty and <see cref="SnapToStops"/> is true,
    /// stops are inferred evenly from <see cref="Labels"/>.
    /// </summary>
    public DoubleCollection? Stops
    {
        get => (DoubleCollection?)GetValue(StopsProperty);
        set => SetValue(StopsProperty, value);
    }

    /// <summary>
    /// Gets or sets whether pointer, keyboard, automation, and programmatic values
    /// snap to <see cref="Stops"/> (or to even label positions when Stops is empty).
    /// Enabling this, or changing Stops/Labels while it is true, coerces the current
    /// <see cref="RangeBase.Value"/> onto the nearest stop.
    /// </summary>
    public bool SnapToStops
    {
        get => (bool)GetValue(SnapToStopsProperty);
        set => SetValue(SnapToStopsProperty, value);
    }

    private static void OnChromeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not EtherSlider slider)
            return;

        slider.CoerceValueToStops();
        slider.UpdateBarLayout();
    }

    private static void OnLabelsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not EtherSlider slider)
            return;

        slider.DetachLabelsCollection();
        slider.AttachLabelsCollection(e.NewValue as INotifyCollectionChanged);
        slider.CoerceValueToStops();
        slider.UpdateBarLayout();
    }

    /// <summary>
    /// Formats <paramref name="value"/> as a whole-number string using the current culture.
    /// </summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This existing public instance method is part of the declared public API; making it static would be a breaking API change.")]
    public string FormatValue(double value) => ((int)Math.Round(value)).ToString(CultureInfo.CurrentCulture);

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        DetachInteractionHandlers();
        DetachLabelLayoutHandlers();

        _valueText = GetTemplateChild(ValueTextPart) as TextBlock;
        _barCanvas = GetTemplateChild(BarCanvasPart) as Canvas;
        _labelRow = GetTemplateChild(LabelRowPart) as Grid;
        _highlightBrushSource = GetTemplateChild(HighlightBrushSourcePart) as Border;
        _inactiveBrushSource = GetTemplateChild(InactiveBrushSourcePart) as Border;
        _knobBrushSource = GetTemplateChild(KnobBrushSourcePart) as Border;
        _knobPressedBrushSource = GetTemplateChild(KnobPressedBrushSourcePart) as Border;
        _knob = GetTemplateChild(KnobPart) as Rectangle;
        _bars = [];
        _labels = CollectDeclaredLabels();

        AttachInteractionHandlers();
        AttachLabelLayoutHandlers();
        UpdateBarLayout();
    }

    /// <inheritdoc />
    protected override void OnValueChanged(double oldValue, double newValue)
    {
        if (!_coercingValue && SnapToStops)
        {
            var coerced = NormalizeInputValue(newValue);
            if (!AreClose(coerced, newValue))
            {
                _coercingValue = true;
                try
                {
                    Value = coerced;
                }
                finally
                {
                    _coercingValue = false;
                }

                return;
            }
        }

        base.OnValueChanged(oldValue, newValue);
        if (_bars.Length > 0)
            UpdateTrackVisual();
        else
            UpdateBarLayout();

        if (FrameworkElementAutomationPeer.FromElement(this) is EtherSliderAutomationPeer peer)
            peer.RaiseValueChanged(oldValue, newValue);
    }

    /// <inheritdoc />
    protected override void OnMinimumChanged(double oldMinimum, double newMinimum)
    {
        base.OnMinimumChanged(oldMinimum, newMinimum);
        CoerceValueToStops();
        UpdateBarLayout();
    }

    /// <inheritdoc />
    protected override void OnMaximumChanged(double oldMaximum, double newMaximum)
    {
        base.OnMaximumChanged(oldMaximum, newMaximum);
        CoerceValueToStops();
        UpdateBarLayout();
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer()
        => new EtherSliderAutomationPeer(this);

    private void AttachInteractionHandlers()
    {
        if (_barCanvas is null)
            return;

        _barCanvas.PointerPressed += Canvas_PointerPressed;
        _barCanvas.PointerMoved += Canvas_PointerMoved;
        _barCanvas.PointerReleased += Canvas_PointerReleased;
        _barCanvas.PointerCaptureLost += Canvas_PointerCaptureLost;
        _barCanvas.PointerExited += Canvas_PointerExited;
    }

    private void DetachInteractionHandlers()
    {
        if (_barCanvas is null)
            return;

        _barCanvas.PointerPressed -= Canvas_PointerPressed;
        _barCanvas.PointerMoved -= Canvas_PointerMoved;
        _barCanvas.PointerReleased -= Canvas_PointerReleased;
        _barCanvas.PointerCaptureLost -= Canvas_PointerCaptureLost;
        _barCanvas.PointerExited -= Canvas_PointerExited;
    }

    internal bool CanInteract => IsEnabled;

    internal bool SetValueFromAutomation(double value)
    {
        if (!CanInteract)
            return false;

        SetValueFromKeyboard(value);
        return true;
    }

    internal double AutomationSmallChange => Math.Max(SmallChange, 0d);

    internal double AutomationLargeChange => Math.Max(LargeChange, 0d);

    internal double RangeMinimum => Math.Min(Minimum, Maximum);

    internal double RangeMaximum => Math.Max(Minimum, Maximum);

    private double ResolveTrackWidth()
    {
        if (_barCanvas is { ActualWidth: > 1 })
            return _barCanvas.ActualWidth;
        if (ActualWidth > 1)
            return ActualWidth;
        return DefaultTrackWidth;
    }

    private static int ResolveEvenBarCount(double trackWidth)
    {
        var usable = Math.Max(trackWidth - KnobWidth, MinBarCount * (BarWidth + BarGap));
        var raw = (int)Math.Floor(usable / (BarWidth + BarGap));
        var even = raw - (raw % 2);
        if (even < MinBarCount)
            even = MinBarCount;
        else if (even > MaxBarCount)
            even = MaxBarCount;
        return even;
    }

    private void EnsureBars(int count)
    {
        if (_barCanvas is not Canvas canvas || _knob is null)
        {
            _bars = [];
            return;
        }

        var existing = canvas.Children
            .OfType<Rectangle>()
            .Where(rectangle => rectangle != _knob)
            .ToList();

        if (existing.Count == count && _bars.Length == count)
        {
            _bars = existing.ToArray();
            return;
        }

        while (existing.Count > count)
        {
            var extra = existing[^1];
            canvas.Children.Remove(extra);
            existing.RemoveAt(existing.Count - 1);
        }

        var knobIndex = canvas.Children.IndexOf(_knob);
        if (knobIndex < 0)
        {
            throw new InvalidOperationException(
                "EtherSlider template must declare the Knob rectangle on BarCanvas.");
        }

        while (existing.Count < count)
        {
            var bar = new Rectangle
            {
                Width = BarWidth,
                Height = BarHeight,
                IsHitTestVisible = false
            };
            canvas.Children.Insert(knobIndex, bar);
            existing.Add(bar);
            knobIndex++;
        }

        _bars = existing.ToArray();
    }

    private void UpdateBarLayout()
    {
        if (_updatingLayout)
            return;

        _updatingLayout = true;
        try
        {
            UpdateEnabledAppearance();
            if (!EnsureTrack())
                return;

            UpdateTitle();
            UpdateLabelRow();
            ApplyBarFills();
        }
        finally
        {
            _updatingLayout = false;
        }
    }

    private void UpdateTrackVisual()
    {
        if (_updatingLayout || _bars.Length == 0)
            return;

        ApplyBarFills();
    }

    private bool EnsureTrack()
    {
        if (_barCanvas is not Canvas canvas || _knob is null)
            return false;

        canvas.ClearValue(WidthProperty);
        canvas.Height = KnobHeight;

        var trackWidth = Math.Max(
            ResolveTrackWidth(),
            MinBarCount * (BarWidth + BarGap) + KnobWidth);
        var barCount = ResolveEvenBarCount(trackWidth);
        EnsureBars(barCount);
        if (_bars.Length != barCount)
            return false;

        _trackWidth = trackWidth;
        _segmentStep = (trackWidth - KnobWidth) / barCount;
        return true;
    }

    private void ApplyBarFills()
    {
        var barCount = _bars.Length;
        if (barCount == 0 || _knob is null)
            return;

        var span = RangeMaximum - RangeMinimum;
        var ratio = span <= 0d ? 0d : Math.Clamp((Value - RangeMinimum) / span, 0d, 1d);
        var highlightedCount = Math.Clamp((int)Math.Round(ratio * barCount), 0, barCount);
        var segmentStep = _segmentStep;
        var gap = Math.Max(segmentStep - BarWidth, 0d);
        var barTop = (KnobHeight - BarHeight) / 2.0;
        var knobX = highlightedCount * segmentStep;

        for (var i = 0; i < highlightedCount; i++)
        {
            var bar = _bars[i];
            bar.Width = BarWidth;
            bar.Height = BarHeight;
            BindBarFill(bar, _highlightBrushSource, "EtherSliderHighlightBrush");
            Canvas.SetLeft(bar, i * segmentStep);
            Canvas.SetTop(bar, barTop);
        }

        _knobBaseX = knobX;
        UpdateKnobVisual();
        Canvas.SetTop(_knob, 0);

        var firstUnselectedX = knobX + KnobWidth + gap;
        for (var i = highlightedCount; i < barCount; i++)
        {
            var bar = _bars[i];
            bar.Width = BarWidth;
            bar.Height = BarHeight;
            BindBarFill(bar, _inactiveBrushSource, "EtherSliderInactiveBrush");
            Canvas.SetLeft(bar, firstUnselectedX + (i - highlightedCount) * segmentStep);
            Canvas.SetTop(bar, barTop);
        }
    }

    private void UpdateInteractionState()
    {
        if (!IsEnabled)
        {
            _dragging = false;
            _pressedKnob = false;
            _hoveringKnob = false;
        }

        UpdateBarLayout();
    }

    private void UpdateEnabledAppearance()
    {
        // RangeBase already prevents interaction while disabled. Dim the entire
        // rendered value (ticks, knob, and label) as well, so the visual state
        // communicates that same contract instead of looking interactive.
        if (_barCanvas is not null)
            _barCanvas.Opacity = IsEnabled ? 1d : 0.4d;
        if (_valueText is not null)
            _valueText.Opacity = IsEnabled ? 1d : 0.4d;
        if (_labelRow is not null)
            _labelRow.Opacity = IsEnabled ? 1d : 0.4d;
    }

    private void Canvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (!CanInteract)
            return;

        Focus(FocusState.Programmatic);
        _dragging = true;
        _pressedKnob = true;
        (sender as Canvas)?.CapturePointer(e.Pointer);
        UpdateValueFromPointer(e);
        if (_knob is not null)
            UpdateKnobVisual();
    }

    private void Canvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!CanInteract)
            return;

        if (_dragging)
            UpdateValueFromPointer(e);
        UpdateKnobHoverState(e);
    }

    private void Canvas_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!CanInteract)
            return;

        _dragging = false;
        _pressedKnob = false;
        (sender as Canvas)?.ReleasePointerCapture(e.Pointer);
        if (_knob is not null)
            UpdateKnobVisual();
    }

    private void Canvas_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        _dragging = false;
        _pressedKnob = false;
        if (_knob is not null)
            UpdateKnobVisual();
    }

    private void Canvas_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _hoveringKnob = false;
        if (_knob is not null)
            UpdateKnobVisual();
    }

    private void OnGotFocus(object sender, RoutedEventArgs e)
    {
        _keyboardFocused = FocusState == FocusState.Keyboard;
        if (_knob is not null)
            UpdateKnobVisual();
    }

    private void OnLostFocus(object sender, RoutedEventArgs e)
    {
        _keyboardFocused = false;
        if (_knob is not null)
            UpdateKnobVisual();
    }

    private void UpdateKnobHoverState(PointerRoutedEventArgs e)
    {
        if (_knob is null || _barCanvas is not Canvas canvas)
            return;

        var pt = e.GetCurrentPoint(canvas).Position;
        var left = Canvas.GetLeft(_knob);
        var right = left + _knob.Width;
        var isOver = pt.X >= left && pt.X <= right &&
                     pt.Y >= 0 && pt.Y <= KnobHeight;
        if (isOver != _hoveringKnob)
        {
            _hoveringKnob = isOver;
            UpdateKnobVisual();
        }
    }

    private void UpdateKnobVisual()
    {
        if (_knob is null)
            return;

        var w = (_hoveringKnob || _pressedKnob || _keyboardFocused) ? KnobHoverWidth : KnobWidth;
        var r = (_hoveringKnob || _pressedKnob || _keyboardFocused) ? KnobHoverCornerRadius : KnobCornerRadius;
        _knob.Width = w;
        _knob.Height = KnobHeight;
        _knob.RadiusX = r;
        _knob.RadiusY = r;
        BindBarFill(
            _knob,
            _pressedKnob ? _knobPressedBrushSource : _knobBrushSource,
            _pressedKnob ? "EtherSliderKnobPressedBrush" : "EtherSliderKnobBrush");

        var desiredLeft = _knobBaseX + KnobWidth / 2.0 - w / 2.0;
        var clampedLeft = Math.Max(0, Math.Min(desiredLeft, _trackWidth - w));
        Canvas.SetLeft(_knob, clampedLeft);
    }

    private void UpdateValueFromPointer(PointerRoutedEventArgs e)
    {
        if (!CanInteract || _barCanvas is not Canvas canvas)
            return;

        var barCount = _bars.Length;
        if (barCount <= 0 || _segmentStep <= 0)
            return;

        var pt = e.GetCurrentPoint(canvas).Position;
        var span = RangeMaximum - RangeMinimum;
        var slot = (int)Math.Round((pt.X - BarWidth / 2.0) / _segmentStep);
        slot = Math.Clamp(slot, 0, barCount);
        var rawValue = RangeMinimum + slot / (double)barCount * span;
        Value = NormalizeInputValue(rawValue);
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (!CanInteract)
            return;

        var handled = e.Key switch
        {
            VirtualKey.Left or VirtualKey.Down => MoveByKeyboardStep(false, false),
            VirtualKey.Right or VirtualKey.Up => MoveByKeyboardStep(true, false),
            VirtualKey.PageDown => MoveByKeyboardStep(false, true),
            VirtualKey.PageUp => MoveByKeyboardStep(true, true),
            VirtualKey.Home => SetValueFromKeyboard(RangeMinimum),
            VirtualKey.End => SetValueFromKeyboard(RangeMaximum),
            _ => false
        };

        if (handled)
            e.Handled = true;
    }

    private bool MoveByKeyboardStep(bool increase, bool useLargeChange)
    {
        if (SnapToStops && GetStopValues().Count > 0)
            return increase ? MoveToNextStop() : MoveToPreviousStop();

        var step = Math.Max(useLargeChange ? LargeChange : SmallChange, 0d);
        if (step <= 0)
            return false;

        var nextValue = Math.Clamp(Value + (increase ? step : -step), RangeMinimum, RangeMaximum);
        return SetValueFromKeyboard(nextValue);
    }

    /// <summary>Moves to the previous stop, if any.</summary>
    public bool MoveToPreviousStop()
    {
        var stopValues = GetStopValues();
        if (stopValues.Count == 0)
            return false;

        for (var i = stopValues.Count - 1; i >= 0; i--)
        {
            if (stopValues[i] < Value && !AreClose(stopValues[i], Value))
                return SetValueFromKeyboard(stopValues[i]);
        }

        return SetValueFromKeyboard(stopValues[0]);
    }

    /// <summary>Moves to the next stop, if any.</summary>
    public bool MoveToNextStop()
    {
        var stopValues = GetStopValues();
        if (stopValues.Count == 0)
            return false;

        for (var i = 0; i < stopValues.Count; i++)
        {
            if (stopValues[i] > Value && !AreClose(stopValues[i], Value))
                return SetValueFromKeyboard(stopValues[i]);
        }

        return SetValueFromKeyboard(stopValues[^1]);
    }

    private bool SetValueFromKeyboard(double value)
    {
        var nextValue = NormalizeInputValue(value);
        if (AreClose(nextValue, Value))
            return false;

        Value = nextValue;
        return true;
    }

    private void CoerceValueToStops()
    {
        if (!SnapToStops)
            return;

        var nextValue = NormalizeInputValue(Value);
        if (!AreClose(nextValue, Value))
            Value = nextValue;
    }

    /// <summary>
    /// Normalizes pointer, keyboard, and automation input. Continuous mode rounds to
    /// the nearest integer. <see cref="SnapToStops"/> uses explicit or inferred stops.
    /// Direct <see cref="RangeBase.Value"/> assignment is not rounded unless snap is on.
    /// </summary>
    private double NormalizeInputValue(double value)
    {
        var clamped = Math.Clamp(value, RangeMinimum, RangeMaximum);
        if (SnapToStops)
            return SnapToNearestStop(clamped);

        return Math.Round(clamped);
    }

    private double SnapToNearestStop(double value)
    {
        var stopValues = GetStopValues();
        if (stopValues.Count == 0)
            return Math.Round(value);

        var nearestStop = stopValues[0];
        var nearestDistance = Math.Abs(value - nearestStop);
        for (var i = 1; i < stopValues.Count; i++)
        {
            var distance = Math.Abs(value - stopValues[i]);
            if (distance < nearestDistance)
            {
                nearestStop = stopValues[i];
                nearestDistance = distance;
            }
        }

        return nearestStop;
    }

    private List<double> GetStopValues()
    {
        var stopValues = new List<double>();
        if (Stops is { Count: > 0 } explicitStops)
        {
            foreach (var stop in explicitStops)
                stopValues.Add(Math.Clamp(stop, RangeMinimum, RangeMaximum));
        }
        else if (Labels is { Count: > 1 } labels)
        {
            var count = Math.Min(labels.Count, MaxLabelCount);
            var span = RangeMaximum - RangeMinimum;
            for (var i = 0; i < count; i++)
                stopValues.Add(RangeMinimum + span * i / (count - 1));
        }

        stopValues.Sort();
        for (var i = stopValues.Count - 1; i > 0; i--)
        {
            if (AreClose(stopValues[i], stopValues[i - 1]))
                stopValues.RemoveAt(i);
        }

        return stopValues;
    }

    private static bool AreClose(double left, double right)
        => Math.Abs(left - right) < 0.0001;

    private static void BindBarFill(Shape shape, Border? source, string resourceKey)
    {
        if (source is null)
            throw new InvalidOperationException($"EtherSlider is missing component resource '{resourceKey}'.");

        shape.SetBinding(Shape.FillProperty, new Binding
        {
            Source = source,
            Path = new PropertyPath(nameof(Border.Background))
        });
    }
}
