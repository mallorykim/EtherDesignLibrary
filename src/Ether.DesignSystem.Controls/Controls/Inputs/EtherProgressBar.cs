using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Ether.DesignSystem.Controls;

/// <summary>
/// Displays a non-interactive, determinate amount of progress with optional title and value labels.
/// </summary>
/// <remarks>
/// The control derives from <see cref="RangeBase"/> to expose its familiar range properties,
/// but its automation contract is read-only. Value changes update layout directly; the control
/// deliberately has no animation, so reduced-motion handling is not applicable.
/// </remarks>
[TemplatePart(Name = LayoutRootPart, Type = typeof(Grid))]
[TemplatePart(Name = FillColumnPart, Type = typeof(ColumnDefinition))]
[TemplatePart(Name = RestColumnPart, Type = typeof(ColumnDefinition))]
[TemplatePart(Name = LabelRowPart, Type = typeof(Grid))]
[TemplatePart(Name = TitleTextPart, Type = typeof(ContentPresenter))]
[TemplatePart(Name = ValueLabelPart, Type = typeof(ContentPresenter))]
[TemplateVisualState(GroupName = LabelStatesGroup, Name = BothLabelsVisibleState)]
[TemplateVisualState(GroupName = LabelStatesGroup, Name = TitleOnlyState)]
[TemplateVisualState(GroupName = LabelStatesGroup, Name = ValueOnlyState)]
[TemplateVisualState(GroupName = LabelStatesGroup, Name = LabelsHiddenState)]
public sealed class EtherProgressBar : RangeBase
{
    private const string LayoutRootPart = "LayoutRoot";
    private const string FillColumnPart = "FillColumn";
    private const string RestColumnPart = "RestColumn";
    private const string LabelRowPart = "LabelRow";
    private const string TitleTextPart = "TitleText";
    private const string ValueLabelPart = "ValueLabel";
    private const string LabelStatesGroup = "LabelStates";
    private const string BothLabelsVisibleState = "BothLabelsVisible";
    private const string TitleOnlyState = "TitleOnly";
    private const string ValueOnlyState = "ValueOnly";
    private const string LabelsHiddenState = "LabelsHidden";

    private ColumnDefinition? _fillColumn;
    private ColumnDefinition? _restColumn;

    /// <summary>
    /// Initializes a new instance of the <see cref="EtherProgressBar"/> class.
    /// </summary>
    public EtherProgressBar()
    {
        DefaultStyleKey = typeof(EtherProgressBar);
    }

    /// <summary>Identifies the <see cref="Title"/> dependency property.</summary>
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(object), typeof(EtherProgressBar), new PropertyMetadata(null));

    /// <summary>Gets or sets the content of the left-hand title label.</summary>
    public object Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Identifies the <see cref="ValueContent"/> dependency property.</summary>
    public static readonly DependencyProperty ValueContentProperty = DependencyProperty.Register(
        nameof(ValueContent), typeof(object), typeof(EtherProgressBar), new PropertyMetadata(null));

    /// <summary>Gets or sets the content of the right-hand value label.</summary>
    public object ValueContent
    {
        get => GetValue(ValueContentProperty);
        set => SetValue(ValueContentProperty, value);
    }

    /// <summary>Identifies the <see cref="ShowTitle"/> dependency property.</summary>
    public static readonly DependencyProperty ShowTitleProperty = DependencyProperty.Register(
        nameof(ShowTitle), typeof(bool), typeof(EtherProgressBar), new PropertyMetadata(false, OnLabelsChanged));

    /// <summary>Gets or sets whether the title label is shown.</summary>
    public bool ShowTitle
    {
        get => (bool)GetValue(ShowTitleProperty);
        set => SetValue(ShowTitleProperty, value);
    }

    /// <summary>Identifies the <see cref="ShowValue"/> dependency property.</summary>
    public static readonly DependencyProperty ShowValueProperty = DependencyProperty.Register(
        nameof(ShowValue), typeof(bool), typeof(EtherProgressBar), new PropertyMetadata(false, OnLabelsChanged));

    /// <summary>Gets or sets whether the value label is shown.</summary>
    public bool ShowValue
    {
        get => (bool)GetValue(ShowValueProperty);
        set => SetValue(ShowValueProperty, value);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _fillColumn = GetTemplateChild(FillColumnPart) as ColumnDefinition;
        _restColumn = GetTemplateChild(RestColumnPart) as ColumnDefinition;
        UpdateFill();
        UpdateLabelState();
    }

    /// <inheritdoc />
    protected override void OnValueChanged(double oldValue, double newValue)
    {
        base.OnValueChanged(oldValue, newValue);
        UpdateFill();

        if (FrameworkElementAutomationPeer.FromElement(this) is EtherProgressBarAutomationPeer peer)
        {
            peer.RaiseValueChanged(oldValue, newValue);
        }
    }

    /// <inheritdoc />
    protected override void OnMinimumChanged(double oldMinimum, double newMinimum)
    {
        base.OnMinimumChanged(oldMinimum, newMinimum);
        UpdateFill();
    }

    /// <inheritdoc />
    protected override void OnMaximumChanged(double oldMaximum, double newMaximum)
    {
        base.OnMaximumChanged(oldMaximum, newMaximum);
        UpdateFill();
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer()
        => new EtherProgressBarAutomationPeer(this);

    private static void OnLabelsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((EtherProgressBar)d).UpdateLabelState();

    private void UpdateFill()
    {
        if (_fillColumn is null || _restColumn is null)
        {
            return;
        }

        var span = Maximum - Minimum;
        var filled = span <= 0d ? 0d : Math.Clamp((Value - Minimum) / span, 0d, 1d);
        _fillColumn.Width = new GridLength(filled, GridUnitType.Star);
        _restColumn.Width = new GridLength(1d - filled, GridUnitType.Star);
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

    private sealed class EtherProgressBarAutomationPeer(EtherProgressBar owner)
        : FrameworkElementAutomationPeer(owner), IRangeValueProvider
    {
        private EtherProgressBar OwnerControl => (EtherProgressBar)Owner;

        public bool IsReadOnly => true;

        public double LargeChange => double.NaN;

        public double Maximum => OwnerControl.Maximum;

        public double Minimum => OwnerControl.Minimum;

        public double SmallChange => double.NaN;

        public double Value => OwnerControl.Value;

        public void SetValue(double value)
            => throw new InvalidOperationException("EtherProgressBar is read-only.");

        internal void RaiseValueChanged(double oldValue, double newValue)
        {
            RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, oldValue, newValue);
        }

        protected override string GetNameCore()
        {
            var name = base.GetNameCore();
            return string.IsNullOrWhiteSpace(name) && OwnerControl.Title is string title ? title : name;
        }

        protected override string GetClassNameCore()
            => nameof(EtherProgressBar);

        protected override AutomationControlType GetAutomationControlTypeCore()
            => AutomationControlType.ProgressBar;

        protected override object? GetPatternCore(PatternInterface patternInterface)
            => patternInterface == PatternInterface.RangeValue ? this : base.GetPatternCore(patternInterface);
    }
}
