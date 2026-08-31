using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Ether.DesignSystem.Controls;

/// <summary>
/// A templated <see cref="ContentControl"/> host for segmented radio items. The default
/// visual is the Figma track via <c>DefaultEtherSegmentedControlStyle</c>.
/// </summary>
/// <remarks>
/// Call sites may still set <c>Style="{StaticResource EtherSegmentedTrack}"</c>; that key
/// is an alias of the default style. Segment item chrome stays on the keyed
/// <c>EtherSegment</c> <see cref="RadioButton"/> style. Put segments in
/// <see cref="EtherSegmentPanel"/> so the track can stretch with the parent and keep
/// equal-width slots. Set a segment's <see cref="FrameworkElement.Tag"/> to its stable
/// business value; <see cref="SelectedValue"/> exposes that value and
/// <see cref="SelectionChanged"/> reports transitions. When Tag is null, the segment's
/// Content is used. Native radio grouping and <see cref="EtherSegmentRadioButton"/> layer
/// opacity remain the interaction model; the host does not add visual-state groups.
/// </remarks>
[TemplatePart(Name = TrackSurfacePartName, Type = typeof(Border))]
public class EtherSegmentedControl : ContentControl
{
    private const string TrackSurfacePartName = "TrackSurface";
    private readonly List<RadioButton> _segments = [];
    private bool _synchronizingSelection;

    /// <summary>Identifies the <see cref="SelectedValue"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedValueProperty = DependencyProperty.Register(
        nameof(SelectedValue),
        typeof(object),
        typeof(EtherSegmentedControl),
        new PropertyMetadata(null, OnSelectedValueChanged));

    /// <summary>
    /// Gets or sets the selected segment's <see cref="FrameworkElement.Tag"/>, or its
    /// Content when Tag is null. Bind this property TwoWay to expose a stable value to an
    /// application or backend-interaction adapter.
    /// </summary>
    public object? SelectedValue
    {
        get => GetValue(SelectedValueProperty);
        set => SetValue(SelectedValueProperty, value);
    }

    /// <summary>Raised after the selected segment value changes.</summary>
    public event EventHandler<SegmentedSelectionChangedEventArgs>? SelectionChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="EtherSegmentedControl"/> class.
    /// </summary>
    public EtherSegmentedControl()
    {
        DefaultStyleKey = typeof(EtherSegmentedControl);
        Loaded += OnLoaded;
        RegisterPropertyChangedCallback(ContentProperty, (_, _) => WireSegments());
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        WireSegments();
    }

    private void OnLoaded(object sender, RoutedEventArgs args) => WireSegments();

    private static void OnSelectedValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (EtherSegmentedControl)d;
        if (!control._synchronizingSelection)
            control.SelectSegmentForValue(e.NewValue);

        control.SelectionChanged?.Invoke(
            control,
            new SegmentedSelectionChangedEventArgs(e.OldValue, e.NewValue));
    }

    private void WireSegments()
    {
        foreach (var segment in _segments)
            segment.Checked -= OnSegmentChecked;

        _segments.Clear();
        AddSegments(Content, _segments);

        foreach (var segment in _segments)
            segment.Checked += OnSegmentChecked;

        if (SelectedValue is null)
            SelectFirstCheckedSegment();
        else
            SelectSegmentForValue(SelectedValue);
    }

    private static void AddSegments(object? content, ICollection<RadioButton> target)
    {
        switch (content)
        {
            case RadioButton segment:
                target.Add(segment);
                break;
            case Panel panel:
                foreach (var child in panel.Children)
                    AddSegments(child, target);
                break;
            case ContentControl contentControl:
                AddSegments(contentControl.Content, target);
                break;
        }
    }

    private void SelectFirstCheckedSegment()
    {
        var selected = _segments.FirstOrDefault(segment => segment.IsChecked == true);
        if (selected is not null)
            SetSelectedValueFromSegment(selected);
    }

    private void SelectSegmentForValue(object? value)
    {
        var selected = _segments.FirstOrDefault(segment => Equals(GetSegmentValue(segment), value));
        if (selected is null || selected.IsChecked == true)
            return;

        _synchronizingSelection = true;
        try
        {
            foreach (var segment in _segments)
            {
                if (!ReferenceEquals(segment, selected))
                    segment.IsChecked = false;
            }
            selected.IsChecked = true;
        }
        finally
        {
            _synchronizingSelection = false;
        }
    }

    private void OnSegmentChecked(object sender, RoutedEventArgs e)
    {
        if (!_synchronizingSelection && sender is RadioButton segment)
            SetSelectedValueFromSegment(segment);
    }

    private void SetSelectedValueFromSegment(RadioButton segment)
    {
        var value = GetSegmentValue(segment);
        if (Equals(SelectedValue, value))
            return;

        _synchronizingSelection = true;
        try
        {
            foreach (var otherSegment in _segments)
            {
                if (!ReferenceEquals(otherSegment, segment))
                    otherSegment.IsChecked = false;
            }
            SetValue(SelectedValueProperty, value);
        }
        finally
        {
            _synchronizingSelection = false;
        }
    }

    private static object? GetSegmentValue(RadioButton segment)
        => segment.Tag ?? segment.Content;
}
