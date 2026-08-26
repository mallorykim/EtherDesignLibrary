using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace EtherSandbox.Controls;

public enum SteeringBarPreviewStatus
{
    None,
    Default,
    Hover,
    Pressed,
    Disabled
}

public sealed class SteeringBarValueChangedEventArgs : EventArgs
{
    public SteeringBarValueChangedEventArgs(double oldValue, double newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }

    public double OldValue { get; }

    public double NewValue { get; }
}

/// <summary>
/// Ether steering bar: a draggable progress/slider hybrid with a rounded track,
/// gradient fill, glass-like thumb, and optional title/value labels. The live
/// control is interactive; showcase pages can force a visual preview state via
/// <see cref="PreviewStatus"/>.
/// </summary>
/// <remarks>
/// <see cref="PreviewStatus"/> is Gallery-oriented and remains part of the public
/// surface. Removing it would be a PublicAPI breaking change. Label visibility is
/// driven through <c>LabelStates</c>. Thumb size, fill width, stop markers, and
/// PreviewStatus opacity stay code-driven so pointer and keyboard interaction is
/// not rewritten onto VisualStateManager.
/// </remarks>
[TemplatePart(Name = InteractionSurfacePart, Type = typeof(Grid))]
[TemplatePart(Name = TrackBackgroundPart, Type = typeof(Border))]
[TemplatePart(Name = FillBorderPart, Type = typeof(Border))]
[TemplatePart(Name = StopMarkersHostPart, Type = typeof(Canvas))]
[TemplatePart(Name = ThumbHostPart, Type = typeof(Grid))]
[TemplatePart(Name = ThumbBorderPart, Type = typeof(Border))]
[TemplatePart(Name = ThumbFillPart, Type = typeof(Border))]
[TemplatePart(Name = ThumbDisabledShellPart, Type = typeof(Border))]
[TemplatePart(Name = ShadowFarPart, Type = typeof(Border))]
[TemplatePart(Name = ShadowNearPart, Type = typeof(Border))]
[TemplatePart(Name = LabelRowPart, Type = typeof(Grid))]
[TemplatePart(Name = TitleTextPart, Type = typeof(ContentPresenter))]
[TemplatePart(Name = ValueLabelPart, Type = typeof(ContentPresenter))]
[TemplateVisualState(GroupName = LabelStatesGroup, Name = BothLabelsVisibleState)]
[TemplateVisualState(GroupName = LabelStatesGroup, Name = TitleOnlyState)]
[TemplateVisualState(GroupName = LabelStatesGroup, Name = ValueOnlyState)]
[TemplateVisualState(GroupName = LabelStatesGroup, Name = LabelsHiddenState)]
public sealed class EtherSteeringBar : Control
{
    private const string InteractionSurfacePart = "InteractionSurface";
    private const string TrackBackgroundPart = "TrackBackground";
    private const string FillBorderPart = "FillBorder";
    private const string StopMarkersHostPart = "StopMarkersHost";
    private const string ThumbHostPart = "ThumbHost";
    private const string ThumbBorderPart = "ThumbBorder";
    private const string ThumbFillPart = "ThumbFill";
    private const string ThumbDisabledShellPart = "ThumbDisabledShell";
    private const string ShadowFarPart = "ShadowFar";
    private const string ShadowNearPart = "ShadowNear";
    private const string LabelRowPart = "LabelRow";
    private const string TitleTextPart = "TitleText";
    private const string ValueLabelPart = "ValueLabel";
    private const string LabelStatesGroup = "LabelStates";
    private const string BothLabelsVisibleState = "BothLabelsVisible";
    private const string TitleOnlyState = "TitleOnly";
    private const string ValueOnlyState = "ValueOnly";
    private const string LabelsHiddenState = "LabelsHidden";

    private const double TrackHeight = 4;
    private const double DefaultThumbWidth = 21;
    private const double DefaultThumbHeight = 13;
    private const double HoverThumbWidth = 23;
    private const double HoverThumbHeight = 15;
    private const double DefaultThumbTop = 1;
    private const double HoverThumbTop = 0;

    private bool _isPointerOver;
    private bool _isPressed;

    private Grid? _interactionSurface;
    private Border? _trackBackground;
    private Border? _fillBorder;
    private Canvas? _stopMarkersHost;
    private Grid? _thumbHost;
    private Border? _thumbBorder;
    private Border? _thumbFill;
    private Border? _thumbDisabledShell;
    private Border? _shadowFar;
    private Border? _shadowNear;
    private ContentPresenter? _valueLabel;

    public event EventHandler<SteeringBarValueChangedEventArgs>? ValueChanged;

    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(EtherSteeringBar),
            new PropertyMetadata(0d, OnRangePropertyChanged));

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(EtherSteeringBar),
            new PropertyMetadata(100d, OnRangePropertyChanged));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(EtherSteeringBar),
            new PropertyMetadata(0d, OnRangePropertyChanged));

    public static readonly DependencyProperty PreviewStatusProperty =
        DependencyProperty.Register(nameof(PreviewStatus), typeof(SteeringBarPreviewStatus), typeof(EtherSteeringBar),
            new PropertyMetadata(SteeringBarPreviewStatus.None, OnRangePropertyChanged));

    public static readonly DependencyProperty StopsProperty =
        DependencyProperty.Register(nameof(Stops), typeof(DoubleCollection), typeof(EtherSteeringBar),
            new PropertyMetadata(null, OnRangePropertyChanged));

    public static readonly DependencyProperty SnapToStopsProperty =
        DependencyProperty.Register(nameof(SnapToStops), typeof(bool), typeof(EtherSteeringBar),
            new PropertyMetadata(false, OnRangePropertyChanged));

    public static readonly DependencyProperty ShowStopsProperty =
        DependencyProperty.Register(nameof(ShowStops), typeof(bool), typeof(EtherSteeringBar),
            new PropertyMetadata(true, OnRangePropertyChanged));

    public static readonly DependencyProperty SmallChangeProperty =
        DependencyProperty.Register(nameof(SmallChange), typeof(double), typeof(EtherSteeringBar),
            new PropertyMetadata(1d, OnRangePropertyChanged));

    public static readonly DependencyProperty LargeChangeProperty =
        DependencyProperty.Register(nameof(LargeChange), typeof(double), typeof(EtherSteeringBar),
            new PropertyMetadata(10d, OnRangePropertyChanged));

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(object), typeof(EtherSteeringBar),
            new PropertyMetadata(null, OnLabelPropertyChanged));

    public static readonly DependencyProperty ValueContentProperty =
        DependencyProperty.Register(nameof(ValueContent), typeof(object), typeof(EtherSteeringBar),
            new PropertyMetadata(null, OnLabelPropertyChanged));

    public static readonly DependencyProperty ShowTitleProperty =
        DependencyProperty.Register(nameof(ShowTitle), typeof(bool), typeof(EtherSteeringBar),
            new PropertyMetadata(false, OnLabelPropertyChanged));

    public static readonly DependencyProperty ShowValueProperty =
        DependencyProperty.Register(nameof(ShowValue), typeof(bool), typeof(EtherSteeringBar),
            new PropertyMetadata(false, OnLabelPropertyChanged));

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, NormalizeValue(value));
    }

    public SteeringBarPreviewStatus PreviewStatus
    {
        get => (SteeringBarPreviewStatus)GetValue(PreviewStatusProperty);
        set => SetValue(PreviewStatusProperty, value);
    }

    public DoubleCollection? Stops
    {
        get => (DoubleCollection?)GetValue(StopsProperty);
        set => SetValue(StopsProperty, value);
    }

    public bool SnapToStops
    {
        get => (bool)GetValue(SnapToStopsProperty);
        set => SetValue(SnapToStopsProperty, value);
    }

    public bool ShowStops
    {
        get => (bool)GetValue(ShowStopsProperty);
        set => SetValue(ShowStopsProperty, value);
    }

    public double SmallChange
    {
        get => (double)GetValue(SmallChangeProperty);
        set => SetValue(SmallChangeProperty, value);
    }

    public double LargeChange
    {
        get => (double)GetValue(LargeChangeProperty);
        set => SetValue(LargeChangeProperty, value);
    }

    public object? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public object? ValueContent
    {
        get => GetValue(ValueContentProperty);
        set => SetValue(ValueContentProperty, value);
    }

    public bool ShowTitle
    {
        get => (bool)GetValue(ShowTitleProperty);
        set => SetValue(ShowTitleProperty, value);
    }

    public bool ShowValue
    {
        get => (bool)GetValue(ShowValueProperty);
        set => SetValue(ShowValueProperty, value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EtherSteeringBar"/> class.
    /// </summary>
    public EtherSteeringBar()
    {
        DefaultStyleKey = typeof(EtherSteeringBar);
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);

        Loaded += (_, _) => UpdateVisuals();
        SizeChanged += (_, _) => UpdateVisuals();
        IsEnabledChanged += (_, _) => UpdateVisuals();
        ActualThemeChanged += (_, _) => UpdateVisuals();
        KeyDown += OnKeyDown;
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        DetachInteractionHandlers();

        _interactionSurface = GetTemplateChild(InteractionSurfacePart) as Grid;
        _trackBackground = GetTemplateChild(TrackBackgroundPart) as Border;
        _fillBorder = GetTemplateChild(FillBorderPart) as Border;
        _stopMarkersHost = GetTemplateChild(StopMarkersHostPart) as Canvas;
        _thumbHost = GetTemplateChild(ThumbHostPart) as Grid;
        _thumbBorder = GetTemplateChild(ThumbBorderPart) as Border;
        _thumbFill = GetTemplateChild(ThumbFillPart) as Border;
        _thumbDisabledShell = GetTemplateChild(ThumbDisabledShellPart) as Border;
        _shadowFar = GetTemplateChild(ShadowFarPart) as Border;
        _shadowNear = GetTemplateChild(ShadowNearPart) as Border;
        _valueLabel = GetTemplateChild(ValueLabelPart) as ContentPresenter;

        AttachInteractionHandlers();
        UpdateLabels();
        UpdateVisuals();
    }

    protected override AutomationPeer OnCreateAutomationPeer()
        => new EtherSteeringBarAutomationPeer(this);

    private static void OnRangePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (EtherSteeringBar)d;
        if (e.Property != ValueProperty)
        {
            var normalizedValue = control.NormalizeValue(control.Value);
            if (!AreClose(control.Value, normalizedValue))
            {
                control.SetValue(ValueProperty, normalizedValue);
                return;
            }
        }

        if (e.Property == ValueProperty && e.OldValue is double oldValue && e.NewValue is double newValue)
        {
            control.UpdateVisuals();
            control.OnValueChanged(oldValue, newValue);
        }
        else
        {
            control.UpdateVisuals();
        }
    }

    private static void OnLabelPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((EtherSteeringBar)d).UpdateLabels();

    private void AttachInteractionHandlers()
    {
        if (_interactionSurface is null)
            return;

        _interactionSurface.PointerEntered += InteractionSurface_PointerEntered;
        _interactionSurface.PointerExited += InteractionSurface_PointerExited;
        _interactionSurface.PointerPressed += InteractionSurface_PointerPressed;
        _interactionSurface.PointerMoved += InteractionSurface_PointerMoved;
        _interactionSurface.PointerReleased += InteractionSurface_PointerReleased;
        _interactionSurface.PointerCaptureLost += InteractionSurface_PointerCaptureLost;
    }

    private void DetachInteractionHandlers()
    {
        if (_interactionSurface is null)
            return;

        _interactionSurface.PointerEntered -= InteractionSurface_PointerEntered;
        _interactionSurface.PointerExited -= InteractionSurface_PointerExited;
        _interactionSurface.PointerPressed -= InteractionSurface_PointerPressed;
        _interactionSurface.PointerMoved -= InteractionSurface_PointerMoved;
        _interactionSurface.PointerReleased -= InteractionSurface_PointerReleased;
        _interactionSurface.PointerCaptureLost -= InteractionSurface_PointerCaptureLost;
    }

    private bool HasForcedState => PreviewStatus != SteeringBarPreviewStatus.None;

    private SteeringBarPreviewStatus EffectiveStatus
    {
        get
        {
            if (HasForcedState)
                return PreviewStatus;

            if (!IsEnabled) return SteeringBarPreviewStatus.Disabled;
            if (_isPressed) return SteeringBarPreviewStatus.Pressed;
            if (_isPointerOver) return SteeringBarPreviewStatus.Hover;
            return SteeringBarPreviewStatus.Default;
        }
    }

    internal double RangeMinimum => Math.Min(Minimum, Maximum);

    internal double RangeMaximum => Math.Max(Minimum, Maximum);

    private double NormalizeValue(double value)
    {
        var clampedValue = Math.Clamp(value, RangeMinimum, RangeMaximum);
        return SnapToStops ? SnapToNearestStop(clampedValue) : clampedValue;
    }

    private void OnValueChanged(double oldValue, double newValue)
    {
        if (AreClose(oldValue, newValue))
            return;

        UpdateLabels();

        ValueChanged?.Invoke(this, new SteeringBarValueChangedEventArgs(oldValue, newValue));

        if (FrameworkElementAutomationPeer.FromElement(this) is EtherSteeringBarAutomationPeer peer)
            peer.RaiseValueChanged(oldValue, newValue);
    }

    private double SnapToNearestStop(double value)
    {
        var stopValues = GetStopValues();
        if (stopValues.Count == 0)
            return value;

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
        if (Stops is null || Stops.Count == 0)
            return stopValues;

        foreach (var stop in Stops)
            stopValues.Add(Math.Clamp(stop, RangeMinimum, RangeMaximum));

        stopValues.Sort();
        for (var i = stopValues.Count - 1; i > 0; i--)
        {
            if (AreClose(stopValues[i], stopValues[i - 1]))
                stopValues.RemoveAt(i);
        }

        return stopValues;
    }

    public bool MoveToPreviousStop()
    {
        var stopValues = GetStopValues();
        if (stopValues.Count == 0)
            return false;

        for (var i = stopValues.Count - 1; i >= 0; i--)
        {
            if (stopValues[i] < Value && !AreClose(stopValues[i], Value))
            {
                Value = stopValues[i];
                return true;
            }
        }

        if (!AreClose(Value, stopValues[0]))
        {
            Value = stopValues[0];
            return true;
        }

        return false;
    }

    public bool MoveToNextStop()
    {
        var stopValues = GetStopValues();
        if (stopValues.Count == 0)
            return false;

        for (var i = 0; i < stopValues.Count; i++)
        {
            if (stopValues[i] > Value && !AreClose(stopValues[i], Value))
            {
                Value = stopValues[i];
                return true;
            }
        }

        if (!AreClose(Value, stopValues[^1]))
        {
            Value = stopValues[^1];
            return true;
        }

        return false;
    }

    private bool MoveByKeyboardStep(bool increase, bool useLargeChange)
    {
        if (SnapToStops && GetStopValues().Count > 0)
            return increase ? MoveToNextStop() : MoveToPreviousStop();

        var step = Math.Max(useLargeChange ? LargeChange : SmallChange, 0d);
        if (step <= 0)
            return false;

        var nextValue = NormalizeValue(Value + (increase ? step : -step));
        if (AreClose(nextValue, Value))
            return false;

        Value = nextValue;
        return true;
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (HasForcedState || !IsEnabled)
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

    private bool SetValueFromKeyboard(double value)
    {
        var nextValue = NormalizeValue(value);
        if (AreClose(nextValue, Value))
            return false;

        Value = nextValue;
        return true;
    }

    internal bool CanInteract => IsEnabled && !HasForcedState;

    internal bool SetValueFromAutomation(double value)
        => CanInteract && SetValueFromKeyboard(value);

    internal double AutomationSmallChange => Math.Max(SmallChange, 0d);

    internal double AutomationLargeChange => Math.Max(LargeChange, 0d);

    private void RenderStopMarkers(double trackWidth, double ratio, bool isDisabled)
    {
        if (_stopMarkersHost is null)
            return;

        _stopMarkersHost.Children.Clear();

        var stopValues = GetStopValues();
        if (!ShowStops || stopValues.Count == 0 || trackWidth <= 0)
            return;

        var span = RangeMaximum - RangeMinimum;
        if (span <= 0)
            return;

        _stopMarkersHost.Width = trackWidth;
        _stopMarkersHost.Height = _interactionSurface?.ActualHeight ?? 15;

        var markerSize = 3d;
        var markerTop = ((_interactionSurface?.ActualHeight ?? 15) - markerSize) / 2d;
        var activeBrush = GetStopMarkerBrush(
            isDisabled ? "EtherSteeringBarStopMarkerDisabledActiveBrush" : "EtherSteeringBarStopMarkerActiveBrush");
        var inactiveBrush = GetStopMarkerBrush(
            isDisabled ? "EtherSteeringBarStopMarkerDisabledInactiveBrush" : "EtherSteeringBarStopMarkerInactiveBrush");

        foreach (var stopValue in stopValues)
        {
            var stopRatio = Math.Clamp((stopValue - RangeMinimum) / span, 0d, 1d);
            var marker = new Border
            {
                Width = markerSize,
                Height = markerSize,
                CornerRadius = new CornerRadius(markerSize / 2d),
                Background = stopRatio <= ratio ? activeBrush : inactiveBrush,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(marker, Math.Clamp(trackWidth * stopRatio - markerSize / 2d, 0, Math.Max(0, trackWidth - markerSize)));
            Canvas.SetTop(marker, markerTop);
            _stopMarkersHost.Children.Add(marker);
        }
    }

    private static bool AreClose(double left, double right)
        => Math.Abs(left - right) < 0.0001;

    private Brush GetStopMarkerBrush(string resourceKey)
    {
        if (TryFindResource(resourceKey) is Brush brush)
            return brush;

        throw new InvalidOperationException($"EtherSteeringBar is missing component resource '{resourceKey}'.");
    }

    private object? TryFindResource(string resourceKey)
    {
        if (Resources.TryGetValue(resourceKey, out var localResource))
            return localResource;

        if (Application.Current?.Resources.TryGetValue(resourceKey, out var appResource) == true)
            return appResource;

        return null;
    }

    private void UpdateLabels()
    {
        if (_valueLabel is not null)
            _valueLabel.Content = GetEffectiveValueContent();

        UpdateLabelState();
    }

    private void UpdateLabelState()
    {
        var state = (ShowTitle, ShowValue) switch
        {
            (true, true) => BothLabelsVisibleState,
            (true, false) => TitleOnlyState,
            (false, true) => ValueOnlyState,
            _ => LabelsHiddenState,
        };

        VisualStateManager.GoToState(this, state, false);
    }

    private object? GetEffectiveValueContent()
        => ValueContent ?? $"{Math.Round(Value):0}%";

    private void UpdateVisuals()
    {
        if (_interactionSurface is null)
            return;

        var span = RangeMaximum - RangeMinimum;
        var ratio = span <= 0 ? 0d : Math.Clamp((Value - RangeMinimum) / span, 0d, 1d);
        var trackWidth = _interactionSurface.ActualWidth;
        var fillWidth = trackWidth * ratio;
        var status = EffectiveStatus;

        var thumbWidth = status == SteeringBarPreviewStatus.Hover ? HoverThumbWidth : DefaultThumbWidth;
        var thumbHeight = status == SteeringBarPreviewStatus.Hover ? HoverThumbHeight : DefaultThumbHeight;
        var thumbTop = status == SteeringBarPreviewStatus.Hover ? HoverThumbTop : DefaultThumbTop;
        var thumbCenter = fillWidth;
        var thumbLeft = Math.Clamp(thumbCenter - thumbWidth / 2d, 0, Math.Max(0, trackWidth - thumbWidth));
        var isDisabled = status == SteeringBarPreviewStatus.Disabled;

        if (_trackBackground is not null)
            _trackBackground.Opacity = isDisabled ? 0.4 : 1.0;
        if (_fillBorder is not null)
            _fillBorder.Opacity = isDisabled ? 0.4 : 1.0;
        if (_shadowFar is not null)
            _shadowFar.Opacity = isDisabled ? 0.4 : 1.0;
        if (_shadowNear is not null)
            _shadowNear.Opacity = isDisabled ? 0.4 : 1.0;
        if (_thumbBorder is not null)
            _thumbBorder.Opacity = isDisabled ? 0.0 : 1.0;
        if (_thumbFill is not null)
            _thumbFill.Opacity = isDisabled ? 0.0 : 1.0;
        if (_thumbDisabledShell is not null)
            _thumbDisabledShell.Opacity = isDisabled ? 1.0 : 0.0;

        RenderStopMarkers(trackWidth, ratio, isDisabled);

        if (_fillBorder is not null)
        {
            _fillBorder.Width = fillWidth;
            _fillBorder.Height = TrackHeight;
        }

        if (_thumbHost is not null)
        {
            _thumbHost.Width = thumbWidth;
            _thumbHost.Height = thumbHeight;
            _thumbHost.Margin = new Thickness(thumbLeft, thumbTop, 0, 0);
        }
    }

    private void UpdateValueFromPointer(PointerRoutedEventArgs e)
    {
        if (HasForcedState || !IsEnabled || _interactionSurface is null || _interactionSurface.ActualWidth <= 0)
            return;

        var point = e.GetCurrentPoint(_interactionSurface).Position;
        var ratio = Math.Clamp(point.X / _interactionSurface.ActualWidth, 0d, 1d);
        var value = RangeMinimum + (RangeMaximum - RangeMinimum) * ratio;
        Value = value;
    }

    private void InteractionSurface_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (HasForcedState || !IsEnabled)
            return;
        _isPointerOver = true;
        UpdateVisuals();
    }

    private void InteractionSurface_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (HasForcedState || !IsEnabled)
            return;
        _isPointerOver = false;
        UpdateVisuals();
    }

    private void InteractionSurface_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (HasForcedState || !IsEnabled || _interactionSurface is null)
            return;
        Focus(FocusState.Programmatic);
        _isPressed = true;
        _isPointerOver = true;
        _interactionSurface.CapturePointer(e.Pointer);
        UpdateValueFromPointer(e);
        UpdateVisuals();
    }

    private void InteractionSurface_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (HasForcedState || !IsEnabled)
            return;
        if (_isPressed)
            UpdateValueFromPointer(e);
    }

    private void InteractionSurface_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (HasForcedState || !IsEnabled || _interactionSurface is null)
            return;
        _isPressed = false;
        _interactionSurface.ReleasePointerCapture(e.Pointer);
        UpdateVisuals();
    }

    private void InteractionSurface_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        if (HasForcedState)
            return;
        _isPressed = false;
        UpdateVisuals();
    }

    private sealed class EtherSteeringBarAutomationPeer(EtherSteeringBar owner)
        : FrameworkElementAutomationPeer(owner), IRangeValueProvider
    {
        private EtherSteeringBar OwnerControl => (EtherSteeringBar)Owner;

        public bool IsReadOnly => !OwnerControl.CanInteract;

        public double LargeChange => OwnerControl.AutomationLargeChange;

        public double Maximum => OwnerControl.RangeMaximum;

        public double Minimum => OwnerControl.RangeMinimum;

        public double SmallChange => OwnerControl.AutomationSmallChange;

        public double Value => OwnerControl.Value;

        public void SetValue(double value)
        {
            if (!OwnerControl.SetValueFromAutomation(value))
                throw new InvalidOperationException("The steering bar cannot accept automation-driven value changes in its current state.");
        }

        internal void RaiseValueChanged(double oldValue, double newValue)
            => RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, oldValue, newValue);

        protected override string GetNameCore()
        {
            var name = base.GetNameCore();
            return string.IsNullOrWhiteSpace(name) && OwnerControl.Title is string title ? title : name;
        }

        protected override string GetClassNameCore()
            => nameof(EtherSteeringBar);

        protected override AutomationControlType GetAutomationControlTypeCore()
            => AutomationControlType.Slider;

        protected override object? GetPatternCore(PatternInterface patternInterface)
            => patternInterface == PatternInterface.RangeValue ? this : base.GetPatternCore(patternInterface);
    }
}
