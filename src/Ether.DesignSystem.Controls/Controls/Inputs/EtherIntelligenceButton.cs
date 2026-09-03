using System;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Ether.DesignSystem.Controls;

/// <summary>
/// A templated <see cref="Button"/> with a hand cursor and the intelligence
/// gradient border / glow visual. Mirrors <see cref="EtherButton"/>'s behaviour and
/// icon API: optional strongly-typed <see cref="LeftIcon"/> / <see cref="RightIcon"/>
/// slots (LeftIcon defaults to the sparkles glyph and can be replaced or cleared).
/// The default visual is owned by <c>DefaultEtherIntelligenceButtonStyle</c>.
/// </summary>
/// <remarks>
/// The control reuses native <see cref="Button"/> CommonStates and FocusStates and adds
/// LeftIconStates / RightIconStates for the two icon slots. It has a single visual skin
/// (no Variant/Size); decorative glow layers and the dual inside stroke stay in the template.
/// </remarks>
[TemplatePart(Name = BgPart, Type = typeof(Border))]
[TemplatePart(Name = BorderIntelligenceBluePart, Type = typeof(Border))]
[TemplatePart(Name = BorderIntelligenceGradientPart, Type = typeof(Border))]
[TemplatePart(Name = BlueGlowOuterPart, Type = typeof(Border))]
[TemplatePart(Name = BlueGlowMiddlePart, Type = typeof(Border))]
[TemplatePart(Name = BlueGlowCorePart, Type = typeof(Border))]
[TemplatePart(Name = PurpleGlowPart, Type = typeof(Border))]
[TemplatePart(Name = ContentPresenterPart, Type = typeof(ContentPresenter))]
[TemplatePart(Name = LeftIconPart, Type = typeof(ContentPresenter))]
[TemplatePart(Name = RightIconPart, Type = typeof(ContentPresenter))]
[TemplatePart(Name = FocusRingPart, Type = typeof(Border))]
[TemplateVisualState(GroupName = CommonStatesGroup, Name = NormalState)]
[TemplateVisualState(GroupName = CommonStatesGroup, Name = PointerOverState)]
[TemplateVisualState(GroupName = CommonStatesGroup, Name = PressedState)]
[TemplateVisualState(GroupName = CommonStatesGroup, Name = DisabledState)]
[TemplateVisualState(GroupName = FocusStatesGroup, Name = FocusedState)]
[TemplateVisualState(GroupName = FocusStatesGroup, Name = UnfocusedState)]
[TemplateVisualState(GroupName = FocusStatesGroup, Name = PointerFocusedState)]
[TemplateVisualState(GroupName = LeftIconStatesGroup, Name = LeftIconVisibleState)]
[TemplateVisualState(GroupName = LeftIconStatesGroup, Name = LeftIconCollapsedState)]
[TemplateVisualState(GroupName = RightIconStatesGroup, Name = RightIconVisibleState)]
[TemplateVisualState(GroupName = RightIconStatesGroup, Name = RightIconCollapsedState)]
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
    private const string LeftIconPart = "LeftIcon";
    private const string RightIconPart = "RightIcon";
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
    private const string LeftIconStatesGroup = "LeftIconStates";
    private const string LeftIconVisibleState = "LeftIconVisible";
    private const string LeftIconCollapsedState = "LeftIconCollapsed";
    private const string RightIconStatesGroup = "RightIconStates";
    private const string RightIconVisibleState = "RightIconVisible";
    private const string RightIconCollapsedState = "RightIconCollapsed";

    // Internal template state: explicit consumer templates/selectors always take precedence
    // over the built-in single-line string rendering path. Mirrors EtherButton.
    internal static readonly DependencyProperty UsesTextContentPathProperty =
        DependencyProperty.Register(
            nameof(UsesTextContentPath),
            typeof(bool),
            typeof(EtherIntelligenceButton),
            new PropertyMetadata(false));

    internal bool UsesTextContentPath => (bool)GetValue(UsesTextContentPathProperty);

    /// <summary>
    /// Initializes a new instance of the <see cref="EtherIntelligenceButton"/> class.
    /// </summary>
    public EtherIntelligenceButton()
    {
        DefaultStyleKey = typeof(EtherIntelligenceButton);
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
        RegisterPropertyChangedCallback(ContentProperty, OnContentPresentationPropertyChanged);
        RegisterPropertyChangedCallback(ContentTemplateProperty, OnContentPresentationPropertyChanged);
        RegisterPropertyChangedCallback(ContentTemplateSelectorProperty, OnContentPresentationPropertyChanged);
        UpdateUsesTextContentPath();
        LeftIcon = CreateDefaultSparklesIcon();
    }

    /// <summary>Identifies the <see cref="LeftIcon"/> dependency property. Registered default is <see langword="null"/>; the constructor seeds the sparkles icon.</summary>
    public static readonly DependencyProperty LeftIconProperty =
        DependencyProperty.Register(
            nameof(LeftIcon),
            typeof(IconElement),
            typeof(EtherIntelligenceButton),
            new PropertyMetadata(null, OnIconChanged));

    /// <summary>Leading icon. Defaults to the sparkles glyph; set another <see cref="IconElement"/> to replace it, or <see langword="null"/> to remove it.</summary>
    public IconElement? LeftIcon
    {
        get => (IconElement?)GetValue(LeftIconProperty);
        set => SetValue(LeftIconProperty, value);
    }

    /// <summary>Identifies the <see cref="RightIcon"/> dependency property. Registered default is <see langword="null"/>.</summary>
    public static readonly DependencyProperty RightIconProperty =
        DependencyProperty.Register(
            nameof(RightIcon),
            typeof(IconElement),
            typeof(EtherIntelligenceButton),
            new PropertyMetadata(null, OnIconChanged));

    /// <summary>Optional trailing icon. Registered and effective default is <see langword="null"/>; set to show a trailing icon.</summary>
    public IconElement? RightIcon
    {
        get => (IconElement?)GetValue(RightIconProperty);
        set => SetValue(RightIconProperty, value);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateUsesTextContentPath();
        UpdateIconStates();
    }

    private static ImageIcon CreateDefaultSparklesIcon() => new()
    {
        Width = 24,
        Height = 24,
        Source = new SvgImageSource(new Uri("ms-appx:///Assets/Icons/Sparkles.svg")),
    };

    private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((EtherIntelligenceButton)d).UpdateIconStates();

    private void OnContentPresentationPropertyChanged(DependencyObject sender, DependencyProperty property)
        => UpdateUsesTextContentPath();

    private void UpdateUsesTextContentPath()
        => SetValue(
            UsesTextContentPathProperty,
            Content is string && ContentTemplate is null && ContentTemplateSelector is null);

    private void UpdateIconStates()
    {
        VisualStateManager.GoToState(
            this,
            LeftIcon is null ? LeftIconCollapsedState : LeftIconVisibleState,
            false);
        VisualStateManager.GoToState(
            this,
            RightIcon is null ? RightIconCollapsedState : RightIconVisibleState,
            false);
    }
}
