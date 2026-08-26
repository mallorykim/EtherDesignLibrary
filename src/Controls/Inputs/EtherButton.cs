using System;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Controls;

/// <summary>
/// A templated <see cref="Button"/> with a hand cursor and an optional trailing
/// <see cref="RightIcon"/>. The default visual is Primary medium
/// (<see cref="EtherButtonVariant.Primary"/> + <see cref="EtherButtonSize.Large"/>)
/// via <c>DefaultEtherButtonStyle</c>.
/// </summary>
/// <remarks>
/// Set <see cref="Variant"/> and <see cref="Size"/> together to select one of the six named
/// styles, or assign <c>Style</c> explicitly. The <c>RightIcon</c> slot is collapsed through
/// <c>RightIconStates</c> when the icon is unset. The control uses native <see cref="Button"/>
/// CommonStates and FocusStates; it does not add indeterminate APIs.
/// </remarks>
[TemplatePart(Name = RightIconPart, Type = typeof(ContentPresenter))]
[TemplateVisualState(GroupName = CommonStatesGroup, Name = NormalState)]
[TemplateVisualState(GroupName = CommonStatesGroup, Name = PointerOverState)]
[TemplateVisualState(GroupName = CommonStatesGroup, Name = PressedState)]
[TemplateVisualState(GroupName = CommonStatesGroup, Name = DisabledState)]
[TemplateVisualState(GroupName = FocusStatesGroup, Name = FocusedState)]
[TemplateVisualState(GroupName = FocusStatesGroup, Name = UnfocusedState)]
[TemplateVisualState(GroupName = FocusStatesGroup, Name = PointerFocusedState)]
[TemplateVisualState(GroupName = RightIconStatesGroup, Name = RightIconVisibleState)]
[TemplateVisualState(GroupName = RightIconStatesGroup, Name = RightIconCollapsedState)]
public class EtherButton : Button
{
    private const string RightIconPart = "RightIcon";
    private const string CommonStatesGroup = "CommonStates";
    private const string NormalState = "Normal";
    private const string PointerOverState = "PointerOver";
    private const string PressedState = "Pressed";
    private const string DisabledState = "Disabled";
    private const string FocusStatesGroup = "FocusStates";
    private const string FocusedState = "Focused";
    private const string UnfocusedState = "Unfocused";
    private const string PointerFocusedState = "PointerFocused";
    private const string RightIconStatesGroup = "RightIconStates";
    private const string RightIconVisibleState = "RightIconVisible";
    private const string RightIconCollapsedState = "RightIconCollapsed";

    /// <summary>
    /// Initializes a new instance of the <see cref="EtherButton"/> class.
    /// </summary>
    public EtherButton()
    {
        DefaultStyleKey = typeof(EtherButton);
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
    }

    /// <summary>Identifies the <see cref="RightIcon"/> dependency property.</summary>
    public static readonly DependencyProperty RightIconProperty =
        DependencyProperty.Register(
            nameof(RightIcon),
            typeof(IconElement),
            typeof(EtherButton),
            new PropertyMetadata(null, OnRightIconChanged));

    /// <summary>Optional trailing icon. Null = text only. Set (or replace) to show an icon.</summary>
    public IconElement? RightIcon
    {
        get => (IconElement?)GetValue(RightIconProperty);
        set => SetValue(RightIconProperty, value);
    }

    /// <summary>Visual weight of the button. Paired with <see cref="Size"/> to select one of
    /// the 6 named styles (e.g. <c>EtherButtonPrimarySmall</c>) automatically — an alternative
    /// to setting <c>Style</c> directly. Unset (null) by default: existing call sites that set
    /// <c>Style</c> explicitly are completely unaffected by this property's existence. Don't set
    /// both <c>Style</c> and <c>Variant</c>/<c>Size</c> on the same element — whichever is
    /// assigned last wins, with no guard against one clobbering the other.</summary>
    public EtherButtonVariant? Variant
    {
        get => (EtherButtonVariant?)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    /// <summary>Identifies the <see cref="Variant"/> dependency property.</summary>
    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(
        nameof(Variant), typeof(EtherButtonVariant?), typeof(EtherButton),
        new PropertyMetadata(null, OnVariantOrSizeChanged));

    /// <summary>Size of the button. See <see cref="Variant"/> — both must be set for this to
    /// take effect.</summary>
    public EtherButtonSize? Size
    {
        get => (EtherButtonSize?)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>Identifies the <see cref="Size"/> dependency property.</summary>
    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
        nameof(Size), typeof(EtherButtonSize?), typeof(EtherButton),
        new PropertyMetadata(null, OnVariantOrSizeChanged));

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateRightIconState();
    }

    /// <summary>
    /// Resolves Variant + Size to one of the 6 named styles and assigns the whole Style
    /// property — the same thing a caller does by hand with
    /// Style="{StaticResource EtherButtonPrimarySmall}". Deliberately does NOT set MinHeight,
    /// Padding, or FontSize as separate local values: those come from the resolved Style's own
    /// setters, so they can never fight a local-value override a caller sets directly (e.g.
    /// HorizontalContentAlignment, IsHitTestVisible) the way a DP-driven per-property setter
    /// would. No-ops until both Variant and Size are set, so existing Style= call sites are
    /// entirely unaffected by this property's existence.
    /// </summary>
    private static void OnVariantOrSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var button = (EtherButton)d;
        if (button.Variant is not EtherButtonVariant variant || button.Size is not EtherButtonSize size)
            return;

        var key = (variant, size) switch
        {
            (EtherButtonVariant.Primary, EtherButtonSize.Large) => "EtherButtonPrimary",
            (EtherButtonVariant.Primary, EtherButtonSize.Small) => "EtherButtonPrimarySmall",
            (EtherButtonVariant.Secondary, EtherButtonSize.Large) => "EtherButtonSecondary",
            (EtherButtonVariant.Secondary, EtherButtonSize.Small) => "EtherButtonSecondarySmall",
            (EtherButtonVariant.Tertiary, EtherButtonSize.Large) => "EtherButtonTertiary",
            (EtherButtonVariant.Tertiary, EtherButtonSize.Small) => "EtherButtonTertiarySmall",
            _ => throw new InvalidOperationException($"Unsupported EtherButton variant/size combination: {variant}/{size}."),
        };

        if (Application.Current.Resources.TryGetValue(key, out var style) && style is Style s)
            button.Style = s;
    }

    private static void OnRightIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((EtherButton)d).UpdateRightIconState();

    private void UpdateRightIconState()
    {
        var state = RightIcon is null ? RightIconCollapsedState : RightIconVisibleState;
        VisualStateManager.GoToState(this, state, false);
    }
}

/// <summary>Visual weight for <see cref="EtherButton.Variant"/>.</summary>
public enum EtherButtonVariant { Primary, Secondary, Tertiary }

/// <summary>Size for <see cref="EtherButton.Size"/>.</summary>
public enum EtherButtonSize { Large, Small }
