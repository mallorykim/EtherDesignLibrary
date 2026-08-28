using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.System;

namespace EtherSandbox.Controls;

/// <summary>
/// Ether bar-chart slider: 63 ticks, a 4px knob, and click/drag range interaction.
/// Derives from <see cref="RangeBase"/> so Minimum, Maximum, Value, and change
/// steps share the WinUI range contract.
/// </summary>
/// <remarks>
/// The 63 bar rectangles and knob are template-declared. Code updates fill,
/// Canvas.Left, and knob hover/press sizing. Pointer capture stays on
/// BarCanvas rather than VisualStateManager. Component <c>EtherSlider*</c>
/// keys supply colors.
/// </remarks>
[TemplatePart(Name = ValueTextPart, Type = typeof(TextBlock))]
[TemplatePart(Name = BarCanvasPart, Type = typeof(Canvas))]
[TemplatePart(Name = KnobPart, Type = typeof(Rectangle))]
[TemplatePart(Name = HighlightBrushSourcePart, Type = typeof(Border))]
[TemplatePart(Name = InactiveBrushSourcePart, Type = typeof(Border))]
[TemplatePart(Name = KnobBrushSourcePart, Type = typeof(Border))]
[TemplatePart(Name = KnobPressedBrushSourcePart, Type = typeof(Border))]
public sealed class EtherSlider : RangeBase
{
    private const string ValueTextPart = "ValueText";
    private const string BarCanvasPart = "BarCanvas";
    private const string KnobPart = "Knob";
    private const string HighlightBrushSourcePart = "HighlightBrushSource";
    private const string InactiveBrushSourcePart = "InactiveBrushSource";
    private const string KnobBrushSourcePart = "KnobBrushSource";
    private const string KnobPressedBrushSourcePart = "KnobPressedBrushSource";

    private const int BarCount = 63;
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
    private Border? _highlightBrushSource;
    private Border? _inactiveBrushSource;
    private Border? _knobBrushSource;
    private Border? _knobPressedBrushSource;
    private Rectangle? _knob;
    private Rectangle[] _bars = [];
    private bool _dragging;
    private bool _hoveringKnob;
    private bool _pressedKnob;
    private double _knobBaseX;

    /// <summary>
    /// Initializes a new instance of the <see cref="EtherSlider"/> class.
    /// </summary>
    public EtherSlider()
    {
        DefaultStyleKey = typeof(EtherSlider);
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);

        Loaded += (_, _) => UpdateBarLayout();
        IsEnabledChanged += (_, _) => UpdateInteractionState();
        ActualThemeChanged += (_, _) => UpdateBarLayout();
        KeyDown += OnKeyDown;
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

        _valueText = GetTemplateChild(ValueTextPart) as TextBlock;
        _barCanvas = GetTemplateChild(BarCanvasPart) as Canvas;
        _highlightBrushSource = GetTemplateChild(HighlightBrushSourcePart) as Border;
        _inactiveBrushSource = GetTemplateChild(InactiveBrushSourcePart) as Border;
        _knobBrushSource = GetTemplateChild(KnobBrushSourcePart) as Border;
        _knobPressedBrushSource = GetTemplateChild(KnobPressedBrushSourcePart) as Border;
        _knob = GetTemplateChild(KnobPart) as Rectangle;
        _bars = CollectDeclaredBars();

        AttachInteractionHandlers();
        UpdateBarLayout();
    }

    /// <inheritdoc />
    protected override void OnValueChanged(double oldValue, double newValue)
    {
        base.OnValueChanged(oldValue, newValue);
        UpdateBarLayout();

        if (FrameworkElementAutomationPeer.FromElement(this) is EtherSliderAutomationPeer peer)
            peer.RaiseValueChanged(oldValue, newValue);
    }

    /// <inheritdoc />
    protected override void OnMinimumChanged(double oldMinimum, double newMinimum)
    {
        base.OnMinimumChanged(oldMinimum, newMinimum);
        UpdateBarLayout();
    }

    /// <inheritdoc />
    protected override void OnMaximumChanged(double oldMaximum, double newMaximum)
    {
        base.OnMaximumChanged(oldMaximum, newMaximum);
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
        => CanInteract && SetValueFromKeyboard(value);

    internal double AutomationSmallChange => Math.Max(SmallChange, 0d);

    internal double AutomationLargeChange => Math.Max(LargeChange, 0d);

    internal double RangeMinimum => Math.Min(Minimum, Maximum);

    internal double RangeMaximum => Math.Max(Minimum, Maximum);

    private Rectangle[] CollectDeclaredBars()
    {
        if (_barCanvas is null)
            return [];

        var bars = _barCanvas.Children
            .OfType<Rectangle>()
            .Where(rectangle => rectangle != _knob)
            .ToArray();
        if (bars.Length != BarCount)
        {
            throw new InvalidOperationException(
                $"EtherSlider template must declare {BarCount} bar rectangles plus Knob. Observed {bars.Length} bars.");
        }

        return bars;
    }

    private void UpdateBarLayout()
    {
        UpdateEnabledAppearance();

        if (_barCanvas is not Canvas canvas || _knob is null || _bars.Length != BarCount)
            return;

        var span = RangeMaximum - RangeMinimum;
        var ratio = span <= 0d ? 0d : Math.Clamp((Value - RangeMinimum) / span, 0d, 1d);
        var highlightedCount = Math.Clamp((int)Math.Round(ratio * BarCount), 0, BarCount);
        var segmentStep = BarWidth + BarGap;
        var totalWidth = BarCount * segmentStep + KnobWidth;
        canvas.Width = totalWidth;
        canvas.Height = KnobHeight;

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

        if (_valueText is not null)
        {
            _valueText.Text = FormatValue(Value);
            _valueText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var labelWidth = _valueText.DesiredSize.Width;
            var labelCenter = knobX + KnobWidth / 2.0;
            var labelLeft = labelCenter - labelWidth / 2.0;
            if (labelLeft < 0)
                labelLeft = 0;
            else if (labelLeft + labelWidth > totalWidth)
                labelLeft = totalWidth - labelWidth;
            _valueText.Margin = new Thickness(labelLeft, 0, 0, 8);
        }

        _knobBaseX = knobX;
        UpdateKnobVisual();
        Canvas.SetTop(_knob, 0);

        var firstUnselectedX = knobX + KnobWidth + BarGap;
        for (var i = highlightedCount; i < BarCount; i++)
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

        var w = (_hoveringKnob || _pressedKnob) ? KnobHoverWidth : KnobWidth;
        var r = (_hoveringKnob || _pressedKnob) ? KnobHoverCornerRadius : KnobCornerRadius;
        _knob.Width = w;
        _knob.Height = KnobHeight;
        _knob.RadiusX = r;
        _knob.RadiusY = r;
        BindBarFill(
            _knob,
            _pressedKnob ? _knobPressedBrushSource : _knobBrushSource,
            _pressedKnob ? "EtherSliderKnobPressedBrush" : "EtherSliderKnobBrush");

        var desiredLeft = _knobBaseX + KnobWidth / 2.0 - w / 2.0;
        var totalWidth = BarCount * (BarWidth + BarGap) + KnobWidth;
        var clampedLeft = Math.Max(0, Math.Min(desiredLeft, totalWidth - w));
        Canvas.SetLeft(_knob, clampedLeft);
    }

    private void UpdateValueFromPointer(PointerRoutedEventArgs e)
    {
        if (!CanInteract || _barCanvas is not Canvas canvas)
            return;

        var pt = e.GetCurrentPoint(canvas).Position;
        var segmentStep = BarWidth + BarGap;
        var slot = (int)Math.Round((pt.X - BarWidth / 2.0) / segmentStep);
        slot = Math.Clamp(slot, 0, BarCount);
        var span = RangeMaximum - RangeMinimum;
        var rawValue = RangeMinimum + slot / (double)BarCount * span;
        Value = Math.Round(rawValue);
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
        var step = Math.Max(useLargeChange ? LargeChange : SmallChange, 0d);
        if (step <= 0)
            return false;

        var nextValue = Math.Clamp(Value + (increase ? step : -step), RangeMinimum, RangeMaximum);
        return SetValueFromKeyboard(nextValue);
    }

    private bool SetValueFromKeyboard(double value)
    {
        if (AreClose(value, Value))
            return false;

        Value = value;
        return true;
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

    private sealed class EtherSliderAutomationPeer(EtherSlider owner)
        : FrameworkElementAutomationPeer(owner), IRangeValueProvider
    {
        private EtherSlider OwnerControl => (EtherSlider)Owner;

        public bool IsReadOnly => !OwnerControl.CanInteract;

        public double LargeChange => OwnerControl.AutomationLargeChange;

        public double Maximum => OwnerControl.RangeMaximum;

        public double Minimum => OwnerControl.RangeMinimum;

        public double SmallChange => OwnerControl.AutomationSmallChange;

        public double Value => OwnerControl.Value;

        public void SetValue(double value)
        {
            if (!OwnerControl.SetValueFromAutomation(value))
                throw new InvalidOperationException("The slider cannot accept automation-driven value changes in its current state.");
        }

        internal void RaiseValueChanged(double oldValue, double newValue)
            => RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, oldValue, newValue);

        protected override string GetClassNameCore()
            => nameof(EtherSlider);

        protected override AutomationControlType GetAutomationControlTypeCore()
            => AutomationControlType.Slider;

        protected override object? GetPatternCore(PatternInterface patternInterface)
            => patternInterface == PatternInterface.RangeValue ? this : base.GetPatternCore(patternInterface);
    }
}
