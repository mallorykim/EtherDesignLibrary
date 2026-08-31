using Ether.DesignSystem.Controls;
using Microsoft.UI.Xaml;

namespace Ether.DesignSystem.Controls;

/// <summary>
/// Gallery-only subclass of <see cref="EtherSegmentRadioButton"/> that adds a forced
/// <see cref="PreviewState"/> so the STATES preview swatches can show a hover/pressed look
/// without a real pointer. All hover/pressed/checked layer-driving logic lives in the shipped
/// base class; this type only folds <see cref="PreviewState"/> into that logic.
/// </summary>
public class HandRadioButton : EtherSegmentRadioButton
{
    /// <summary>Forces a state for the static STATES preview swatches (no real pointer).</summary>
    public enum SegmentPreview { None, Hover, Pressed }

    public static readonly DependencyProperty PreviewStateProperty =
        DependencyProperty.Register(
            nameof(PreviewState), typeof(SegmentPreview), typeof(HandRadioButton),
            new PropertyMetadata(SegmentPreview.None, (d, _) => ((HandRadioButton)d).UpdateSegmentVisual()));

    public SegmentPreview PreviewState
    {
        get => (SegmentPreview)GetValue(PreviewStateProperty);
        set => SetValue(PreviewStateProperty, value);
    }

    /// <inheritdoc />
    protected override bool IsPointerOverEffective => base.IsPointerOverEffective || PreviewState == SegmentPreview.Hover;

    /// <inheritdoc />
    protected override bool IsPressedEffective => base.IsPressedEffective || PreviewState == SegmentPreview.Pressed;
}
