using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Ether.DesignSystem.Controls;

/// <summary>
/// A templated <see cref="TextBox"/>. The default visual is the single-line Ether
/// input via <c>DefaultEtherInputStyle</c>.
/// </summary>
/// <remarks>
/// The control reuses native <see cref="TextBox"/> CommonStates (Normal, PointerOver,
/// Focused, Disabled). Focus is the brand-blue border in CommonStates, not a FocusStates
/// group.
///
/// WIDTH CONTRACT (no MinWidth floor is shipped — the field can be made arbitrarily narrow):
/// <list type="bullet">
/// <item>Left as-is, the field is 280 px wide and left-aligned, because the shipping style sets
/// <see cref="FrameworkElement.HorizontalAlignment"/> to Left and <see cref="MeasureOverride"/>
/// reports the 280 px default while <see cref="FrameworkElement.Width"/> stays <c>Auto</c>.</item>
/// <item>Set <c>HorizontalAlignment="Stretch"</c> to fill the layout slot — Arrange stretches
/// past the reported default once Width is Auto and alignment is Stretch.</item>
/// <item>Set a concrete <see cref="FrameworkElement.Width"/> to pin the field to that number;
/// an explicit Width wins even alongside Stretch (standard WinUI precedence).</item>
/// </list>
///
/// NOT SUPPORTED (intentionally simplified; the template omits these parts, so the inherited
/// TextBox members silently render nothing — do not expect them to work):
/// the delete/clear (X) button; <see cref="TextBox.Header"/> / <c>HeaderTemplate</c>;
/// <see cref="TextBox.Description"/>. Place a label/description alongside the control instead.
/// </remarks>
[TemplatePart(Name = LayoutRootPart, Type = typeof(Grid))]
[TemplatePart(Name = BorderElementPart, Type = typeof(Border))]
[TemplatePart(Name = PlaceholderTextContentPresenterPart, Type = typeof(TextBlock))]
[TemplatePart(Name = ContentElementPart, Type = typeof(ScrollViewer))]
[TemplateVisualState(GroupName = CommonStatesGroup, Name = NormalState)]
[TemplateVisualState(GroupName = CommonStatesGroup, Name = PointerOverState)]
[TemplateVisualState(GroupName = CommonStatesGroup, Name = FocusedState)]
[TemplateVisualState(GroupName = CommonStatesGroup, Name = DisabledState)]
public sealed class EtherInput : TextBox
{
    private const string LayoutRootPart = "LayoutRoot";
    private const string BorderElementPart = "BorderElement";
    private const string PlaceholderTextContentPresenterPart = "PlaceholderTextContentPresenter";
    private const string ContentElementPart = "ContentElement";
    private const string CommonStatesGroup = "CommonStates";
    private const string NormalState = "Normal";
    private const string PointerOverState = "PointerOver";
    private const string FocusedState = "Focused";
    private const string DisabledState = "Disabled";

    /// <summary>The width the field reports when <see cref="FrameworkElement.Width"/> is left at <c>Auto</c>.</summary>
    private const double DefaultWidthDips = 280d;

    /// <summary>
    /// Initializes a new instance of the <see cref="EtherInput"/> class.
    /// </summary>
    public EtherInput()
    {
        DefaultStyleKey = typeof(EtherInput);
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var baseSize = base.MeasureOverride(availableSize);

        // A concrete Width pins the field to that number; the base measure already honors it.
        if (!double.IsNaN(Width))
            return baseSize;

        // Width is Auto: report the 280 px default as the desired width. The shipping style is
        // Left-aligned, so that reads as a fixed default; a consumer who sets
        // HorizontalAlignment="Stretch" has Arrange stretch past this to fill the slot.
        var width = DefaultWidthDips;
        if (!double.IsInfinity(availableSize.Width))
            width = Math.Min(width, availableSize.Width);

        return new Size(width, baseSize.Height);
    }
}
