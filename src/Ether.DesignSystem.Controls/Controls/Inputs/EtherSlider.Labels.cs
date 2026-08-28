using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace EtherSandbox.Controls;

public sealed partial class EtherSlider
{
    private void AttachLabelLayoutHandlers()
    {
        if (_labelRow is not null)
            _labelRow.SizeChanged += OnLabelRowSizeChanged;

        foreach (var label in _labels)
            label.SizeChanged += OnTickLabelSizeChanged;
    }

    private void DetachLabelLayoutHandlers()
    {
        if (_labelRow is not null)
            _labelRow.SizeChanged -= OnLabelRowSizeChanged;

        foreach (var label in _labels)
            label.SizeChanged -= OnTickLabelSizeChanged;
    }

    private void OnLabelRowSizeChanged(object sender, SizeChangedEventArgs e)
        => PositionTickLabels();

    private void OnTickLabelSizeChanged(object sender, SizeChangedEventArgs e)
        => PositionTickLabels();

    private TextBlock[] CollectDeclaredLabels()
    {
        if (_labelRow is null)
            return [];

        var labels = _labelRow.Children.OfType<TextBlock>().ToArray();
        if (labels.Length != MaxLabelCount)
        {
            throw new InvalidOperationException(
                $"EtherSlider template must declare {MaxLabelCount} tick-label TextBlocks. Observed {labels.Length}.");
        }

        return labels;
    }

    private void AttachLabelsCollection(INotifyCollectionChanged? collection)
    {
        _labelsCollection = collection;
        if (_labelsCollection is not null)
            _labelsCollection.CollectionChanged += OnLabelsCollectionChanged;
    }

    private void DetachLabelsCollection()
    {
        if (_labelsCollection is null)
            return;

        _labelsCollection.CollectionChanged -= OnLabelsCollectionChanged;
        _labelsCollection = null;
    }

    private void OnLabelsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        CoerceValueToStops();
        UpdateBarLayout();
    }

    private string[] GetEffectiveLabels()
    {
        if (!ShowLabels)
            return [];

        if (Labels is { Count: > 0 } custom)
            return custom.Take(MaxLabelCount).ToArray();

        return CreateEvenAutoLabels();
    }

    private string[] CreateEvenAutoLabels()
    {
        var span = RangeMaximum - RangeMinimum;
        var spanInt = (int)Math.Round(span);
        if (Math.Abs(span - spanInt) > 0.0001 || spanInt <= 0)
            return CreateInterpolatedAutoLabels(span);

        var count = ResolveAutoLabelCount(spanInt);
        var labels = new string[count];
        for (var i = 0; i < count; i++)
        {
            var value = count == 1
                ? RangeMinimum
                : RangeMinimum + spanInt * i / (double)(count - 1);
            labels[i] = FormatValue(value);
        }

        return labels;
    }

    private string[] CreateInterpolatedAutoLabels(double span)
    {
        var labels = new string[DefaultAutoLabelCount];
        for (var i = 0; i < DefaultAutoLabelCount; i++)
        {
            var value = DefaultAutoLabelCount == 1
                ? RangeMinimum
                : RangeMinimum + span * i / (DefaultAutoLabelCount - 1);
            labels[i] = FormatValue(value);
        }

        return labels;
    }

    private static int ResolveAutoLabelCount(int spanInt)
    {
        var best = 2;
        var bestDist = int.MaxValue;
        for (var n = 2; n <= MaxLabelCount; n++)
        {
            if (spanInt % (n - 1) != 0)
                continue;

            var dist = Math.Abs(n - DefaultAutoLabelCount);
            if (dist < bestDist || (dist == bestDist && n > best))
            {
                best = n;
                bestDist = dist;
            }
        }

        return best;
    }

    private void UpdateTitle()
    {
        if (_valueText is null)
            return;

        var title = Title;
        var show = ShowTitle && !string.IsNullOrEmpty(title);
        _valueText.Text = show ? title ?? string.Empty : string.Empty;
        _valueText.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateLabelRow()
    {
        if (_labelRow is null || _labels.Length != MaxLabelCount)
            return;

        var texts = GetEffectiveLabels();
        _labelRow.ClearValue(WidthProperty);
        _labelRow.Visibility = texts.Length == 0 ? Visibility.Collapsed : Visibility.Visible;

        for (var i = 0; i < MaxLabelCount; i++)
        {
            var label = _labels[i];
            if (i < texts.Length)
            {
                label.Text = texts[i];
                label.Visibility = Visibility.Visible;
                label.Margin = new Thickness(0);
                label.HorizontalAlignment = HorizontalAlignment.Left;
                label.TextAlignment = TextAlignment.Center;
            }
            else
            {
                label.Text = string.Empty;
                label.Visibility = Visibility.Collapsed;
                label.Margin = new Thickness(0);
                label.RenderTransform = null;
            }
        }

        PositionTickLabels();
    }

    private void PositionTickLabels()
    {
        if (_positioningLabels || _labelRow is null || _labels.Length != MaxLabelCount)
            return;

        var trackWidth = _labelRow.ActualWidth > 1 ? _labelRow.ActualWidth : _trackWidth;
        if (trackWidth <= 1)
            return;

        var count = 0;
        for (var i = 0; i < MaxLabelCount; i++)
        {
            if (_labels[i].Visibility == Visibility.Visible)
                count++;
        }

        if (count == 0)
            return;

        _positioningLabels = true;
        try
        {
            for (var i = 0; i < count; i++)
            {
                var label = _labels[i];
                var labelWidth = label.ActualWidth;
                if (labelWidth <= 0)
                    continue;

                var t = count == 1 ? 0d : i / (double)(count - 1);
                var anchor = t * trackWidth;
                var left = i == 0
                    ? 0d
                    : i == count - 1
                        ? trackWidth - labelWidth
                        : anchor - labelWidth / 2.0;
                left = Math.Clamp(left, 0d, Math.Max(0d, trackWidth - labelWidth));
                label.RenderTransform = new TranslateTransform { X = left };
            }
        }
        finally
        {
            _positioningLabels = false;
        }
    }
}

/// <summary>XAML-friendly list of tick labels for <see cref="EtherSlider"/>.</summary>
public sealed class EtherSliderLabelCollection : ObservableCollection<string>
{
}
