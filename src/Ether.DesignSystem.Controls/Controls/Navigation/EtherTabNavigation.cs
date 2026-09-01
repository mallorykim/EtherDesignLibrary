using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Ether.DesignSystem.Controls;

/// <summary>A horizontal, single-selection tab strip. Applications own associated content.</summary>
public class EtherTabNavigation : ListView
{
    public EtherTabNavigation() => DefaultStyleKey = typeof(EtherTabNavigation);

    /// <inheritdoc />
    protected override bool IsItemItsOwnContainerOverride(object item) => item is EtherTabItem;

    /// <inheritdoc />
    protected override DependencyObject GetContainerForItemOverride() => new EtherTabItem();
}

/// <summary>A selectable item in <see cref="EtherTabNavigation"/>.</summary>
public class EtherTabItem : ListViewItem
{
    private const string IconPresenterPartName = "IconPresenter";
    private const double IconRenderSize = 14;
    private const double IconCanvasSize = 16;

    /// <summary>Identifies the <see cref="Icon"/> dependency property.</summary>
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon), typeof(string), typeof(EtherTabItem), new PropertyMetadata(null, OnIconChanged));

    /// <summary>
    /// Gets or sets the optional leading icon by Ether icon-library name (the
    /// <c>EtherIconGeometries</c> key without the <c>Icon</c> prefix, e.g. <c>"Home"</c>). Only
    /// library names render; anything else collapses the slot. The 14×14 glyph follows the label
    /// colour, inverting on the selected pill.
    /// </summary>
    public string? Icon
    {
        get => (string?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public EtherTabItem() => DefaultStyleKey = typeof(EtherTabItem);

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateIconPresenter();
    }

    private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs _) =>
        ((EtherTabItem)d).UpdateIconPresenter();

    // A PathIcon is recreated for every template pass. More importantly, BuildIcon deep-clones the
    // Style's already-coerced Geometry object graph: merely creating a new PathIcon while reusing
    // Setter.Value leaves the WinUI shared-Geometry lifetime bug intact when ListView containers are
    // torn down or recycled.
    private void UpdateIconPresenter()
    {
        if (GetTemplateChild(IconPresenterPartName) is not ContentPresenter presenter)
            return;

        var icon = BuildIcon(Icon);
        presenter.Content = icon;
        presenter.Visibility = icon is null ? Visibility.Collapsed : Visibility.Visible;
        presenter.Margin = icon is null ? new Thickness(0) : new Thickness(0, 0, 8, 0);
    }

    private static PathIcon? BuildIcon(string? iconName)
    {
        if (string.IsNullOrWhiteSpace(iconName) || Application.Current?.Resources is not { } resources)
            return null;

        if (FindStyle($"Icon{iconName}", resources) is not { } style)
            return null;

        if (ExtractData(style) is not { } source)
            return null;

        try
        {
            var geometry = GeometryCloner.Clone(source);
            // EtherIconGeometries authors glyphs on a 16-unit canvas; scale the (independent) clone
            // into the 14×14 slot so a 16-unit path is not clipped by the 14×14 PathIcon bounds.
            geometry.Transform = new ScaleTransform
            {
                ScaleX = IconRenderSize / IconCanvasSize,
                ScaleY = IconRenderSize / IconCanvasSize,
            };
            return new PathIcon { Data = geometry, Width = IconRenderSize, Height = IconRenderSize };
        }
        catch (NotSupportedException)
        {
            // A colliding, non-Ether Style should behave like an unknown icon name, not crash the UI.
            return null;
        }
    }

    private static Geometry? ExtractData(Style style)
    {
        foreach (var setter in style.Setters)
        {
            if (setter is Setter { Property: var property, Value: Geometry data } && property == PathIcon.DataProperty)
                return data;
        }

        return null;
    }

    private static Style? FindStyle(string key, ResourceDictionary dictionary)
    {
        if (dictionary.TryGetValue(key, out var value) && value is Style style)
            return style;

        foreach (var merged in dictionary.MergedDictionaries)
        {
            if (FindStyle(key, merged) is { } nested)
                return nested;
        }

        return null;
    }
}
