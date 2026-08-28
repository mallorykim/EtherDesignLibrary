using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace EtherSandbox.Controls;

/// <summary>
/// Equal-width row for <see cref="EtherSegmentedControl"/> segments. When the parent
/// gives a finite width the slots fill it; when unconstrained each slot uses the
/// widest child's desired width (Figma's equal 110px segments).
/// </summary>
public sealed class EtherSegmentPanel : Panel
{
    /// <summary>Identifies the <see cref="Spacing"/> dependency property.</summary>
    public static readonly DependencyProperty SpacingProperty =
        DependencyProperty.Register(
            nameof(Spacing),
            typeof(double),
            typeof(EtherSegmentPanel),
            new PropertyMetadata(4d, OnSpacingChanged));

    /// <summary>Gets or sets the gap between segments, in epx. Figma track gap is 4.</summary>
    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    private static void OnSpacingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EtherSegmentPanel panel)
            panel.InvalidateMeasure();
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var count = VisibleCount();
        if (count == 0)
            return new Size(0, 0);

        var spacing = Spacing;
        var gap = spacing * (count - 1);
        var constrained = !double.IsInfinity(availableSize.Width);
        var slotWidth = constrained
            ? Math.Max(0, (availableSize.Width - gap) / count)
            : double.PositiveInfinity;

        double maxChildWidth = 0;
        double maxHeight = 0;
        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
                continue;

            child.Measure(new Size(slotWidth, availableSize.Height));
            maxChildWidth = Math.Max(maxChildWidth, child.DesiredSize.Width);
            maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
        }

        if (!constrained)
            return new Size(maxChildWidth * count + gap, maxHeight);

        return new Size(availableSize.Width, maxHeight);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var count = VisibleCount();
        if (count == 0)
            return finalSize;

        var spacing = Spacing;
        var gap = spacing * (count - 1);
        var slotWidth = Math.Max(0, (finalSize.Width - gap) / count);
        double x = 0;
        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
                continue;

            child.Arrange(new Rect(x, 0, slotWidth, finalSize.Height));
            x += slotWidth + spacing;
        }

        return finalSize;
    }

    private int VisibleCount()
    {
        var count = 0;
        foreach (var child in Children)
        {
            if (child.Visibility != Visibility.Collapsed)
                count++;
        }

        return count;
    }
}
