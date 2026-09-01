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
        RegisterPropertyChangedCallback(IsThreeStateProperty, OnIsThreeStateChanged);
        RegisterPropertyChangedCallback(IsCheckedProperty, OnIsCheckedChanged);
        UpdateUsesTextContentPath();
    }

    // Two-state by design. A null (indeterminate) IsChecked is reachable via a bool? binding
    // regardless of IsThreeState; the default style has no Indeterminate visual, so coerce a null
    // back to false so the control always renders a defined Unchecked/Checked face.
    private void OnIsCheckedChanged(DependencyObject sender, DependencyProperty dp)
    {
        if (IsChecked is null)
            IsChecked = false;
    }

    private void OnContentPresentationPropertyChanged(DependencyObject sender, DependencyProperty property)
        => UpdateUsesTextContentPath();

    private void UpdateUsesTextContentPath()
        => SetValue(
            UsesTextContentPathProperty,
            Content is string && ContentTemplate is null && ContentTemplateSelector is null);

    /// <summary>
    /// DefaultEtherRadioButtonStyle's CheckStates group only declares Unchecked and Checked
    /// (see the [TemplateVisualState] attributes above) - there is no Indeterminate visual.
    /// This control is two-state by design, so silently coerce IsThreeState back to false.
    /// (Throwing from a property-changed callback would surface as a XamlParseException / app
    /// crash if a consumer set IsThreeState="True" in markup.) The companion OnIsCheckedChanged
    /// coerces any null IsChecked to false, so no indeterminate state can strand the visual.
    /// </summary>
    private void OnIsThreeStateChanged(DependencyObject sender, DependencyProperty dp)
    {
        if (IsThreeState)
            SetValue(IsThreeStateProperty, false);
    }

    private FrameworkElement? _focusRing;

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _focusRing = GetTemplateChild("FocusRing") as FrameworkElement;
        UpdateFocusRing();
    }

    /// <inheritdoc />
    protected override void OnGotFocus(RoutedEventArgs e)
    {
        base.OnGotFocus(e);
        UpdateFocusRing();
    }

    /// <inheritdoc />
    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        UpdateFocusRing();
    }

    // Reveal the keyboard focus ring only for keyboard focus (not pointer/programmatic), matching
    // the platform reveal-focus convention. Modern WinUI controls do not drive a FocusStates VSM
    // group, so the ring is toggled here rather than through visual states.
    private void UpdateFocusRing()
    {
        if (_focusRing is not null)
            _focusRing.Visibility = FocusState == FocusState.Keyboard ? Visibility.Visible : Visibility.Collapsed;
    }
}
