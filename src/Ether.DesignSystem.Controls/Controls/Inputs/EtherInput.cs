using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="EtherInput"/> class.
    /// </summary>
    public EtherInput()
    {
        DefaultStyleKey = typeof(EtherInput);
    }
}
