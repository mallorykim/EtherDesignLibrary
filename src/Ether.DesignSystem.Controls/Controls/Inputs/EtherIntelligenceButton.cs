using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ether.DesignSystem.Controls;

/// <summary>
/// A templated <see cref="Button"/> with a hand cursor and the intelligence
/// gradient border / glow visual. The default visual is owned by
/// <c>DefaultEtherIntelligenceButtonStyle</c>.
/// </summary>
/// <remarks>
/// The control reuses native <see cref="Button"/> CommonStates and FocusStates.
/// It does not add variants, trailing-icon APIs, or animation APIs. Decorative
/// glow layers and the dual inside stroke stay in the template.
/// </remarks>
[TemplatePart(Name = BgPart, Type = typeof(Border))]
[TemplatePart(Name = BorderIntelligenceBluePart, Type = typeof(Border))]
[TemplatePart(Name = BorderIntelligenceGradientPart, Type = typeof(Border))]
[TemplatePart(Name = BlueGlowOuterPart, Type = typeof(Border))]
[TemplatePart(Name = BlueGlowMiddlePart, Type = typeof(Border))]
[TemplatePart(Name = BlueGlowCorePart, Type = typeof(Border))]
[TemplatePart(Name = PurpleGlowPart, Type = typeof(Border))]
[TemplatePart(Name = ContentPresenterPart, Type = typeof(ContentPresenter))]
[TemplatePart(Name = FocusRingPart, Type = typeof(Border))]
[TemplateVisualState(GroupName = CommonStatesGroup, Name = NormalState)]
[TemplateVisualState(GroupName = CommonStatesGroup, Name = PointerOverState)]
[TemplateVisualState(GroupName = CommonStatesGroup, Name = PressedState)]
[TemplateVisualState(GroupName = CommonStatesGroup, Name = DisabledState)]
[TemplateVisualState(GroupName = FocusStatesGroup, Name = FocusedState)]
[TemplateVisualState(GroupName = FocusStatesGroup, Name = UnfocusedState)]
[TemplateVisualState(GroupName = FocusStatesGroup, Name = PointerFocusedState)]
public sealed class EtherIntelligenceButton : Button
{
    private const string BgPart = "Bg";
    private const string BorderIntelligenceBluePart = "BorderIntelligenceBlue";
    private const string BorderIntelligenceGradientPart = "BorderIntelligenceGradient";
    private const string BlueGlowOuterPart = "BlueGlowOuter";
    private const string BlueGlowMiddlePart = "BlueGlowMiddle";
    private const string BlueGlowCorePart = "BlueGlowCore";
    private const string PurpleGlowPart = "PurpleGlow";
    private const string ContentPresenterPart = "Cp";
    private const string FocusRingPart = "FocusRing";
    private const string CommonStatesGroup = "CommonStates";
    private const string NormalState = "Normal";
    private const string PointerOverState = "PointerOver";
    private const string PressedState = "Pressed";
    private const string DisabledState = "Disabled";
    private const string FocusStatesGroup = "FocusStates";
    private const string FocusedState = "Focused";
    private const string UnfocusedState = "Unfocused";
    private const string PointerFocusedState = "PointerFocused";

    /// <summary>
    /// Initializes a new instance of the <see cref="EtherIntelligenceButton"/> class.
    /// </summary>
    public EtherIntelligenceButton()
    {
        DefaultStyleKey = typeof(EtherIntelligenceButton);
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
    }
}
