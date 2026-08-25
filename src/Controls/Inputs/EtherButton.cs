using System;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Controls;

/// <summary>
/// The Ether button. A templated <see cref="Button"/> with a hand cursor and an OPTIONAL,
/// REPLACEABLE trailing icon exposed as a strongly-typed <see cref="RightIcon"/>
/// dependency property (an <see cref="IconElement"/> — FontIcon / BitmapIcon / PathIcon /
/// SymbolIcon). The template hosts it with {TemplateBinding RightIcon}; when it is null the
/// slot collapses (via <see cref="RightIconVisibility"/>) so the button is text only.
///
/// This is the idiomatic WinUI 3 pattern (a real DP + TemplateBinding, like AppBarButton.Icon).
/// It deliberately avoids binding a template FontIcon's Glyph, which faults in MeasureOverride
/// (see the ButtonRightIconSlot history / memory).
/// </summary>
public class EtherButton : Button
{
    public EtherButton()
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
    }

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

    public static readonly DependencyProperty RightIconVisibilityProperty =
        DependencyProperty.Register(
            nameof(RightIconVisibility),
            typeof(Visibility),
            typeof(EtherButton),
            new PropertyMetadata(Visibility.Collapsed));

    /// <summary>Visible only when <see cref="RightIcon"/> is set; the template binds the icon
    /// slot's Visibility to this so there is no trailing gap when the button is text only.</summary>
    public Visibility RightIconVisibility
    {
        get => (Visibility)GetValue(RightIconVisibilityProperty);
        private set => SetValue(RightIconVisibilityProperty, value);
    }

    private static void OnRightIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var button = (EtherButton)d;
        button.RightIconVisibility = e.NewValue is null ? Visibility.Collapsed : Visibility.Visible;
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
    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
        nameof(Size), typeof(EtherButtonSize?), typeof(EtherButton),
        new PropertyMetadata(null, OnVariantOrSizeChanged));

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
            _ => throw new ArgumentOutOfRangeException(nameof(variant)),
        };

        if (Application.Current.Resources.TryGetValue(key, out var style) && style is Style s)
            button.Style = s;
    }
}

/// <summary>Visual weight for <see cref="EtherButton.Variant"/>.</summary>
public enum EtherButtonVariant { Primary, Secondary, Tertiary }

/// <summary>Size for <see cref="EtherButton.Size"/>.</summary>
public enum EtherButtonSize { Large, Small }
