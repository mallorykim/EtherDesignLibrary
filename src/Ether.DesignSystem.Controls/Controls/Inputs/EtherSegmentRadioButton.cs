using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Ether.DesignSystem.Controls;

/// <summary>
/// The segment button for <see cref="EtherSegmentedControl"/>. Use it with the
/// <c>EtherSegment</c> style inside an <see cref="EtherSegmentPanel"/> to get full
/// hover / pressed / checked fidelity; a plain <see cref="RadioButton"/> styled with
/// <c>EtherSegment</c> still shows the selected pill (via the template's VisualStateManager
/// fallback) but not the hover/pressed feedback this class drives.
/// </summary>
/// <remarks>
/// This class drives the hover / pressed / checked background LAYERS directly from its live
/// pointer/checked state instead of the visual state machine: on the <c>EtherSegmentTemplate</c>
/// template the VSM leaves layer opacities stuck at 1 after the pointer leaves (the "grey base
/// won't clear" bug - confirmed: PointerExited fires yet the fill stays). Reading
/// IsPointerOver / IsPressed / IsChecked on every pointer event and setting Opacity directly is
/// deterministic, so a segment can never be left in a stale visual. Not sealed: a subclass can
/// add a forced-preview state (e.g. a design-gallery swatch shown without a real pointer) by
/// overriding <see cref="IsPointerOverEffective"/> / <see cref="IsPressedEffective"/> and calling
/// <see cref="UpdateSegmentVisual"/> - see the Gallery's <c>HandRadioButton</c>.
/// </remarks>
public class EtherSegmentRadioButton : RadioButton
{
    /// <summary>Template part name for the hover background layer.</summary>
    protected const string HoverLayerPartName = "HoverLayer";

    /// <summary>Template part name for the pressed background layer.</summary>
    protected const string PressedLayerPartName = "PressedLayer";

    /// <summary>Template part name for the checked background layer.</summary>
    protected const string CheckedLayerPartName = "CheckedLayer";

    private Border? _hoverLayer;
    private Border? _pressedLayer;
    private Border? _checkedLayer;

    /// <summary>
    /// Initializes a new instance of the <see cref="EtherSegmentRadioButton"/> class.
    /// </summary>
    public EtherSegmentRadioButton()
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);

        // IsChecked changes (including GroupName auto-uncheck and initial IsChecked) raise these.
        Checked += (_, _) => UpdateSegmentVisual();
        Unchecked += (_, _) => UpdateSegmentVisual();
        Indeterminate += (_, _) => UpdateSegmentVisual();
    }

    /// <summary>
    /// Whether the hover layer should be considered active. Defaults to the live
    /// <c>IsPointerOver</c>; a subclass can override this to fold in a forced-preview state.
    /// </summary>
    protected virtual bool IsPointerOverEffective => IsPointerOver;

    /// <summary>
    /// Whether the pressed layer should be considered active. Defaults to the live
    /// <c>IsPressed</c>; a subclass can override this to fold in a forced-preview state.
    /// </summary>
    protected virtual bool IsPressedEffective => IsPressed;

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _hoverLayer = GetTemplateChild(HoverLayerPartName) as Border;
        _pressedLayer = GetTemplateChild(PressedLayerPartName) as Border;
        _checkedLayer = GetTemplateChild(CheckedLayerPartName) as Border;
        UpdateSegmentVisual();
    }

    /// <inheritdoc />
    protected override void OnPointerEntered(PointerRoutedEventArgs e) { base.OnPointerEntered(e); UpdateSegmentVisual(); }

    /// <inheritdoc />
    protected override void OnPointerExited(PointerRoutedEventArgs e) { base.OnPointerExited(e); UpdateSegmentVisual(); }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerRoutedEventArgs e) { base.OnPointerPressed(e); UpdateSegmentVisual(); }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerRoutedEventArgs e) { base.OnPointerReleased(e); UpdateSegmentVisual(); }

    /// <inheritdoc />
    protected override void OnPointerCanceled(PointerRoutedEventArgs e) { base.OnPointerCanceled(e); UpdateSegmentVisual(); }

    /// <inheritdoc />
    protected override void OnPointerCaptureLost(PointerRoutedEventArgs e) { base.OnPointerCaptureLost(e); UpdateSegmentVisual(); }

    /// <summary>
    /// Sets the hover/pressed/checked layer opacities from <see cref="IsPointerOverEffective"/>,
    /// <see cref="IsPressedEffective"/> and <c>IsChecked</c>. Checked wins;
    /// otherwise pressed beats hover; exactly one (or none) layer shows. Call this after
    /// changing anything <see cref="IsPointerOverEffective"/> / <see cref="IsPressedEffective"/>
    /// depend on.
    /// </summary>
    protected void UpdateSegmentVisual()
    {
        if (_hoverLayer is null || _pressedLayer is null || _checkedLayer is null)
            return;

        bool isChecked = IsChecked == true;
        bool isPressed = IsPressedEffective;
        bool isOver = IsPointerOverEffective;

        // Checked wins; otherwise pressed over hover. Exactly one (or none) layer shows.
        _checkedLayer.Opacity = isChecked ? 1 : 0;
        _pressedLayer.Opacity = (!isChecked && isPressed) ? 1 : 0;
        _hoverLayer.Opacity = (!isChecked && !isPressed && isOver) ? 1 : 0;
    }
}
