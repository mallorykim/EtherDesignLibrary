using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace EtherSandbox.Controls;

/// <summary>
/// The segment button for <see cref="EtherSegmentedControl"/>. It drives the hover / pressed /
/// checked background LAYERS directly from its live pointer/checked state instead of the visual
/// state machine: on this template the VSM leaves layer opacities stuck at 1 after the pointer
/// leaves (the "grey base won't clear" bug — confirmed: PointerExited fires yet the fill stays).
/// Reading IsPointerOver / IsPressed / IsChecked on every pointer event and setting Opacity
/// directly is deterministic, so a segment can never be left in a stale visual.
/// </summary>
public class HandRadioButton : RadioButton
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

    private Border? _hoverLayer;
    private Border? _pressedLayer;
    private Border? _checkedLayer;

    public HandRadioButton()
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);

        // IsChecked changes (including GroupName auto-uncheck and initial IsChecked) raise these.
        Checked += (_, _) => UpdateSegmentVisual();
        Unchecked += (_, _) => UpdateSegmentVisual();
        Indeterminate += (_, _) => UpdateSegmentVisual();
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _hoverLayer = GetTemplateChild("HoverLayer") as Border;
        _pressedLayer = GetTemplateChild("PressedLayer") as Border;
        _checkedLayer = GetTemplateChild("CheckedLayer") as Border;
        UpdateSegmentVisual();
    }

    protected override void OnPointerEntered(PointerRoutedEventArgs e) { base.OnPointerEntered(e); UpdateSegmentVisual(); }
    protected override void OnPointerExited(PointerRoutedEventArgs e) { base.OnPointerExited(e); UpdateSegmentVisual(); }
    protected override void OnPointerPressed(PointerRoutedEventArgs e) { base.OnPointerPressed(e); UpdateSegmentVisual(); }
    protected override void OnPointerReleased(PointerRoutedEventArgs e) { base.OnPointerReleased(e); UpdateSegmentVisual(); }
    protected override void OnPointerCanceled(PointerRoutedEventArgs e) { base.OnPointerCanceled(e); UpdateSegmentVisual(); }
    protected override void OnPointerCaptureLost(PointerRoutedEventArgs e) { base.OnPointerCaptureLost(e); UpdateSegmentVisual(); }

    private void UpdateSegmentVisual()
    {
        if (_hoverLayer is null || _pressedLayer is null || _checkedLayer is null)
            return;

        bool isChecked = IsChecked == true;
        bool isPressed = IsPressed || PreviewState == SegmentPreview.Pressed;
        bool isOver = IsPointerOver || PreviewState == SegmentPreview.Hover;

        // Checked wins; otherwise pressed over hover. Exactly one (or none) layer shows.
        _checkedLayer.Opacity = isChecked ? 1 : 0;
        _pressedLayer.Opacity = (!isChecked && isPressed) ? 1 : 0;
        _hoverLayer.Opacity = (!isChecked && !isPressed && isOver) ? 1 : 0;
    }
}
