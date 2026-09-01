using System;
using System.Diagnostics;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ether.DesignSystem.Controls;

/// <summary>
/// A templated <see cref="Button"/> with a hand cursor and optional leading and
/// trailing icons. The default visual is Primary medium
/// (<see cref="EtherButtonVariant.Primary"/> + <see cref="EtherButtonSize.Large"/>)
/// via <c>DefaultEtherButtonStyle</c>.
/// </summary>
/// <remarks>
/// Set <see cref="Variant"/> and <see cref="Size"/> together to select one of the six named
/// styles, or assign <c>Style</c> explicitly. Icon slots collapse independently when unset.
/// The control uses native <see cref="Button"/>
/// CommonStates and FocusStates; it does not add indeterminate APIs.
/// </remarks>
[TemplatePart(Name = BgPart, Type = typeof(Border))]
[TemplatePart(Name = StrokePart, Type = typeof(Border))]
[TemplatePart(Name = CpPart, Type = typeof(ContentPresenter))]
[TemplatePart(Name = FocusRingPart, Type = typeof(Border))]
[TemplatePart(Name = LeftIconPart, Type = typeof(ContentPresenter))]
[TemplatePart(Name = RightIconPart, Type = typeof(ContentPresenter))]
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
public class EtherButton : Button
{
    private const string BgPart = "Bg";
    private const string StrokePart = "Stroke";
    private const string CpPart = "Cp";
    private const string FocusRingPart = "FocusRing";
    private const string LeftIconPart = "LeftIcon";
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
    private const string LeftIconStatesGroup = "LeftIconStates";
    private const string LeftIconVisibleState = "LeftIconVisible";
    private const string LeftIconCollapsedState = "LeftIconCollapsed";
    private const string RightIconStatesGroup = "RightIconStates";
    private const string RightIconVisibleState = "RightIconVisible";
    private const string RightIconCollapsedState = "RightIconCollapsed";
    private RoutedEventHandler? _resolveStyleOnLoaded;

    // Internal template state: explicit consumer templates/selectors always take precedence
    // over the built-in single-line string rendering path.
    internal static readonly DependencyProperty UsesTextContentPathProperty =
        DependencyProperty.Register(
            nameof(UsesTextContentPath),
            typeof(bool),
            typeof(EtherButton),
            new PropertyMetadata(false));

    internal bool UsesTextContentPath => (bool)GetValue(UsesTextContentPathProperty);

    /// <summary>
    /// Initializes a new instance of the <see cref="EtherButton"/> class.
    /// </summary>
    public EtherButton()
    {
        DefaultStyleKey = typeof(EtherButton);
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
        RegisterPropertyChangedCallback(ContentProperty, OnContentPresentationPropertyChanged);
        RegisterPropertyChangedCallback(ContentTemplateProperty, OnContentPresentationPropertyChanged);
        RegisterPropertyChangedCallback(ContentTemplateSelectorProperty, OnContentPresentationPropertyChanged);
        UpdateUsesTextContentPath();
    }

    /// <summary>Identifies the <see cref="LeftIcon"/> dependency property.</summary>
    public static readonly DependencyProperty LeftIconProperty =
        DependencyProperty.Register(
            nameof(LeftIcon),
            typeof(IconElement),
            typeof(EtherButton),
            new PropertyMetadata(null, OnIconChanged));

    /// <summary>Optional leading icon. Null = text only on the leading edge.</summary>
    public IconElement? LeftIcon
    {
        get => (IconElement?)GetValue(LeftIconProperty);
        set => SetValue(LeftIconProperty, value);
    }

    /// <summary>Identifies the <see cref="RightIcon"/> dependency property.</summary>
    public static readonly DependencyProperty RightIconProperty =
        DependencyProperty.Register(
            nameof(RightIcon),
            typeof(IconElement),
            typeof(EtherButton),
            new PropertyMetadata(null, OnIconChanged));

    /// <summary>Optional trailing icon. Null = text only. Set (or replace) to show an icon.</summary>
    public IconElement? RightIcon
    {
        get => (IconElement?)GetValue(RightIconProperty);
        set => SetValue(RightIconProperty, value);
    }

    /// <summary>Identifies the <see cref="Variant"/> dependency property.</summary>
    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(
        nameof(Variant), typeof(EtherButtonVariant?), typeof(EtherButton),
        new PropertyMetadata(null, OnVariantOrSizeChanged));

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

    /// <summary>Identifies the <see cref="Size"/> dependency property.</summary>
    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
        nameof(Size), typeof(EtherButtonSize?), typeof(EtherButton),
        new PropertyMetadata(null, OnVariantOrSizeChanged));

    /// <summary>Size of the button. See <see cref="Variant"/> — both must be set for this to
    /// take effect.</summary>
    public EtherButtonSize? Size
    {
        get => (EtherButtonSize?)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateUsesTextContentPath();
        UpdateIconStates();
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

        var style = ResolveNamedStyle(button, key);
        if (style is not null)
        {
            button.CancelDeferredStyleResolution();
            button.Style = style;
            return;
        }

        if (button.IsLoaded)
        {
            Debug.WriteLine($"[EtherButton] Could not resolve style '{key}' for Variant/Size; ensure the Ether control resource dictionaries are merged into the application or an ancestor scope.");
            return;
        }

        button.CancelDeferredStyleResolution();
        RoutedEventHandler loadedHandler = null!;
        loadedHandler = (_, _) =>
        {
            button.Loaded -= loadedHandler;
            if (ReferenceEquals(button._resolveStyleOnLoaded, loadedHandler))
                button._resolveStyleOnLoaded = null;

            var loadedStyle = ResolveNamedStyle(button, key);
            if (loadedStyle is not null)
                button.Style = loadedStyle;
            else
                Debug.WriteLine($"[EtherButton] Could not resolve style '{key}' for Variant/Size; ensure the Ether control resource dictionaries are merged into the application or an ancestor scope.");
        };
        button._resolveStyleOnLoaded = loadedHandler;
        button.Loaded += loadedHandler;
    }

    private void CancelDeferredStyleResolution()
    {
        if (_resolveStyleOnLoaded is null)
            return;

        Loaded -= _resolveStyleOnLoaded;
        _resolveStyleOnLoaded = null;
    }

    /// <summary>
    /// Resolves a named style from the element's resource scope (local → parents →
    /// application), matching WinUI <c>FindResource</c> order without throwing when absent.
    /// </summary>
    private static Style? ResolveNamedStyle(EtherButton button, string key)
    {
        for (FrameworkElement? current = button; current is not null; current = current.Parent as FrameworkElement)
        {
            if (current.Resources.TryGetValue(key, out var local) && local is Style localStyle)
                return localStyle;
        }

        if (Application.Current?.Resources.TryGetValue(key, out var app) == true && app is Style appStyle)
            return appStyle;

        return null;
    }

    private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((EtherButton)d).UpdateIconStates();

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

/// <summary>Visual weight for <see cref="EtherButton.Variant"/>.</summary>
public enum EtherButtonVariant { Primary, Secondary, Tertiary }

/// <summary>Size for <see cref="EtherButton.Size"/>.</summary>
public enum EtherButtonSize { Large, Small }
