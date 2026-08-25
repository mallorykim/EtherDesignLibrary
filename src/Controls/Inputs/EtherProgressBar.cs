using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace EtherSandbox.Controls;

/// <summary>
/// Ether Design System progress bar (Figma node 60609:946): a rounded track with a
/// gradient fill sized to Value/Maximum, plus optional title (left) and value (right)
/// labels above it. The four spec variants are the on/off combinations of those two
/// labels — <see cref="ShowTitle"/> and <see cref="ShowValue"/>.
/// </summary>
/// <remarks>
/// Derives from <see cref="RangeBase"/> purely to inherit Value / Minimum / Maximum and
/// their change virtuals. The fill width is the ratio between two star-weighted grid
/// columns (<c>FillColumn</c> / <c>RestColumn</c>) — an actual laid-out width, not a
/// scale transform, so its 2&#160;px rounded ends stay perfectly round instead of being
/// squished. Motion comes from changing Value over time (the ProgressBarPage simulator
/// drives it at a data-like variable rate); each change just re-splits the columns.
///
/// Applied via the implicit style in Controls/EtherProgressBar.xaml; no
/// DefaultStyleKey, so nothing looks for a generic.xaml default style.
/// </remarks>
public sealed class EtherProgressBar : RangeBase
{
    public EtherProgressBar()
    {
        Minimum = 0;
        Maximum = 100;
        Value = 0;
    }

    /// <summary>Content of the left-hand title label. A replaceable slot, not baked-in text:
    /// set a string (rendered in the title style) or any element (icon, run, custom text).</summary>
    public object Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(object), typeof(EtherProgressBar), new PropertyMetadata(null));

    /// <summary>Content of the right-hand value label. A replaceable slot like <see cref="Title"/>:
    /// a string ("65%", "3 of 4") or any element. Not derived from Value, so it can read whatever
    /// the caller wants; the simulator on ProgressBarPage updates it as the value animates.</summary>
    public object ValueContent
    {
        get => GetValue(ValueContentProperty);
        set => SetValue(ValueContentProperty, value);
    }
    public static readonly DependencyProperty ValueContentProperty = DependencyProperty.Register(
        nameof(ValueContent), typeof(object), typeof(EtherProgressBar), new PropertyMetadata(null));

    /// <summary>Whether the title label is shown.</summary>
    public bool ShowTitle
    {
        get => (bool)GetValue(ShowTitleProperty);
        set => SetValue(ShowTitleProperty, value);
    }
    public static readonly DependencyProperty ShowTitleProperty = DependencyProperty.Register(
        nameof(ShowTitle), typeof(bool), typeof(EtherProgressBar), new PropertyMetadata(true, OnLabelsChanged));

    /// <summary>Whether the value label is shown.</summary>
    public bool ShowValue
    {
        get => (bool)GetValue(ShowValueProperty);
        set => SetValue(ShowValueProperty, value);
    }
    public static readonly DependencyProperty ShowValueProperty = DependencyProperty.Register(
        nameof(ShowValue), typeof(bool), typeof(EtherProgressBar), new PropertyMetadata(true, OnLabelsChanged));

    private ColumnDefinition? _fillColumn;
    private ColumnDefinition? _restColumn;
    private FrameworkElement? _labelRow;
    private FrameworkElement? _titleText;
    private FrameworkElement? _valueLabel;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _fillColumn = GetTemplateChild("FillColumn") as ColumnDefinition;
        _restColumn = GetTemplateChild("RestColumn") as ColumnDefinition;
        _labelRow = GetTemplateChild("LabelRow") as FrameworkElement;
        _titleText = GetTemplateChild("TitleText") as FrameworkElement;
        _valueLabel = GetTemplateChild("ValueLabel") as FrameworkElement;
        UpdateFill();
        UpdateLabels();
    }

    protected override void OnValueChanged(double oldValue, double newValue)
    {
        base.OnValueChanged(oldValue, newValue);
        UpdateFill();
    }

    protected override void OnMinimumChanged(double oldMinimum, double newMinimum)
    {
        base.OnMinimumChanged(oldMinimum, newMinimum);
        UpdateFill();
    }

    protected override void OnMaximumChanged(double oldMaximum, double newMaximum)
    {
        base.OnMaximumChanged(oldMaximum, newMaximum);
        UpdateFill();
    }

    private static void OnLabelsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((EtherProgressBar)d).UpdateLabels();

    /// <summary>Splits the track into filled / remaining star columns from the value ratio.</summary>
    private void UpdateFill()
    {
        if (_fillColumn is null || _restColumn is null) return;
        var span = Maximum - Minimum;
        var filled = span <= 0 ? 0d : Math.Clamp((Value - Minimum) / span, 0d, 1d);
        _fillColumn.Width = new GridLength(filled, GridUnitType.Star);
        _restColumn.Width = new GridLength(1d - filled, GridUnitType.Star);
    }

    /// <summary>
    /// Toggles the two labels, and collapses the whole label row (and its 8&#160;px gap)
    /// when neither is shown, so the bar-only variant has nothing above the track.
    /// </summary>
    private void UpdateLabels()
    {
        if (_titleText is not null)
            _titleText.Visibility = ShowTitle ? Visibility.Visible : Visibility.Collapsed;
        if (_valueLabel is not null)
            _valueLabel.Visibility = ShowValue ? Visibility.Visible : Visibility.Collapsed;
        if (_labelRow is not null)
            _labelRow.Visibility = (ShowTitle || ShowValue) ? Visibility.Visible : Visibility.Collapsed;
    }
}
