using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ether.DesignSystem.Controls;

/// <summary>
/// A templated <see cref="RadioButton"/>. The default visual is two-state
/// (<c>IsThreeState</c> is false) via <c>DefaultEtherRadioButtonStyle</c>.
/// </summary>
/// <remarks>
/// GroupName-based mutual exclusion is the native <see cref="RadioButton"/> mechanism.
/// The control reuses CommonStates and CheckStates. It does not add an indeterminate
/// visual or animation APIs.
/// </remarks>
[TemplatePart(Name = LayoutRootPart, Type = typeof(Grid))]
[TemplatePart(Name = UncheckedFillPart, Type = typeof(Border))]
[TemplatePart(Name = UncheckedStrokePart, Type = typeof(Border))]
[TemplatePart(Name = UncheckedFacePart, Type = typeof(Grid))]
[TemplatePart(Name = CheckedFacePart, Type = typeof(Grid))]
[TemplatePart(Name = CheckedFillPart, Type = typeof(Border))]
[TemplatePart(Name = CheckedStrokePart, Type = typeof(Border))]
[TemplatePart(Name = DotPart, Type = typeof(Microsoft.UI.Xaml.Shapes.Ellipse))]
[TemplatePart(Name = LabelPart, Type = typeof(ContentPresenter))]
[TemplateVisualState(GroupName = CommonStatesGroup, Name = NormalState)]
[TemplateVisualState(GroupName = CommonStatesGroup, Name = PointerOverState)]
[TemplateVisualState(GroupName = CommonStatesGroup, Name = PressedState)]
[TemplateVisualState(GroupName = CommonStatesGroup, Name = DisabledState)]
[TemplateVisualState(GroupName = CheckStatesGroup, Name = UncheckedState)]
[TemplateVisualState(GroupName = CheckStatesGroup, Name = CheckedState)]
public sealed class EtherRadioButton : RadioButton
{
    private const string LayoutRootPart = "LayoutRoot";
    private const string UncheckedFillPart = "UncheckedFill";
    private const string UncheckedStrokePart = "UncheckedStroke";
    private const string UncheckedFacePart = "UncheckedFace";
    private const string CheckedFacePart = "CheckedFace";
    private const string CheckedFillPart = "CheckedFill";
    private const string CheckedStrokePart = "CheckedStroke";
    private const string DotPart = "Dot";
    private const string LabelPart = "Label";
    private const string CommonStatesGroup = "CommonStates";
    private const string NormalState = "Normal";
    private const string PointerOverState = "PointerOver";
    private const string PressedState = "Pressed";
    private const string DisabledState = "Disabled";
    private const string CheckStatesGroup = "CheckStates";
    private const string UncheckedState = "Unchecked";
    private const string CheckedState = "Checked";

    // Internal template state: explicit consumer templates/selectors always take precedence
    // over the built-in single-line string rendering path.
    internal static readonly DependencyProperty UsesTextContentPathProperty =
        DependencyProperty.Register(
            nameof(UsesTextContentPath),
            typeof(bool),
            typeof(EtherRadioButton),
            new PropertyMetadata(false));

    internal bool UsesTextContentPath => (bool)GetValue(UsesTextContentPathProperty);

    /// <summary>
    /// Initializes a new instance of the <see cref="EtherRadioButton"/> class.
    /// </summary>
    public EtherRadioButton()
    {
        DefaultStyleKey = typeof(EtherRadioButton);
        RegisterPropertyChangedCallback(ContentProperty, OnContentPresentationPropertyChanged);
        RegisterPropertyChangedCallback(ContentTemplateProperty, OnContentPresentationPropertyChanged);
        RegisterPropertyChangedCallback(ContentTemplateSelectorProperty, OnContentPresentationPropertyChanged);
        UpdateUsesTextContentPath();
    }

    private void OnContentPresentationPropertyChanged(DependencyObject sender, DependencyProperty property)
        => UpdateUsesTextContentPath();

    private void UpdateUsesTextContentPath()
        => SetValue(
            UsesTextContentPathProperty,
            Content is string && ContentTemplate is null && ContentTemplateSelector is null);
}
