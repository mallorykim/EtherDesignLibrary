using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace EtherSandbox.Controls;

/// <summary>
/// A templated <see cref="ContentControl"/> host for segmented radio items. The default
/// visual is the Figma track via <c>DefaultEtherSegmentedControlStyle</c>, including the
/// composition drop shadows.
/// </summary>
/// <remarks>
/// Call sites may still set <c>Style="{StaticResource EtherSegmentedTrack}"</c>; that key
/// is an alias of the default style. Segment item chrome stays on the keyed
/// <c>EtherSegment</c> <see cref="RadioButton"/> style. Put segments in
/// <see cref="EtherSegmentPanel"/> so the track can stretch with the parent and keep
/// equal-width slots. This control does not add selection APIs or VisualState groups
/// on the host — native radio grouping and <c>HandRadioButton</c> layer opacity remain
/// the interaction model.
/// </remarks>
[TemplatePart(Name = ShadowHostPartName, Type = typeof(Border))]
[TemplatePart(Name = TrackSurfacePartName, Type = typeof(Border))]
[TemplatePart(Name = CasterBrushSourcePartName, Type = typeof(Border))]
[TemplatePart(Name = ShadowBrushSourcePartName, Type = typeof(Border))]
public class EtherSegmentedControl : ContentControl
{
    private const string ShadowHostPartName = "ShadowHost";
    private const string TrackSurfacePartName = "TrackSurface";
    private const string CasterBrushSourcePartName = "CasterBrushSource";
    private const string ShadowBrushSourcePartName = "ShadowBrushSource";
    private const float TrackCornerRadius = 8f;
    private const float FarBlur = 18f;
    private const float FarOffsetY = 12f;
    private const float NearBlur = 4f;
    private const float NearOffsetY = 2f;
    private const float FarOpacity = 0.08f;
    private const float NearOpacity = 0.10f;

    private Border? _shadowHost;
    private Border? _trackSurface;
    private Border? _casterBrushSource;
    private Border? _shadowBrushSource;
    private ContainerVisual? _shadowContainer;
    private ShadowLayer? _nearShadow;
    private ShadowLayer? _farShadow;

    /// <summary>
    /// Initializes a new instance of the <see cref="EtherSegmentedControl"/> class.
    /// </summary>
    public EtherSegmentedControl()
    {
        DefaultStyleKey = typeof(EtherSegmentedControl);
        Loaded += OnLoaded;
        ActualThemeChanged += OnActualThemeChanged;
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args) => ApplyThemeBrushes();

    /// <summary>
    /// The shadow caster is an opaque shape sitting behind the track surface. In light the
    /// surface (AlphaWhite40 / AlphaBlack70) is translucent, so the caster shows
    /// through. It must therefore match the SURFACE the control sits on
    /// (Gray0 light / Gray900 dark), not the canvas — otherwise the
    /// translucent track reads a different shade over the caster than over the surface next to
    /// it, producing the patchy "grey base" (only where the caster reaches).
    /// Matching the surface makes the caster invisible regardless of how it is sized. The drop
    /// shadow casts from the shape's alpha, so its colour is unaffected.
    /// Colours come from collapsed template <c>Border</c> parts whose fills are
    /// <c>{ThemeResource EtherSegmentedControl*}</c>, so High Contrast rebind follows
    /// <see cref="FrameworkElement.ActualTheme"/>.
    /// </summary>
    private void ApplyThemeBrushes()
    {
        var casterColor = ReadBrushColor(_casterBrushSource);
        _nearShadow?.SetFill(casterColor);
        _farShadow?.SetFill(casterColor);

        var shadowRgb = ReadBrushColor(_shadowBrushSource);
        _nearShadow?.SetShadowRgb(shadowRgb);
        _farShadow?.SetShadowRgb(shadowRgb);
    }

    private static Color ReadBrushColor(Border? source)
    {
        return source?.Background is SolidColorBrush solid ? solid.Color : Color.FromArgb(0, 0, 0, 0);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _shadowContainer = null;
        _nearShadow = null;
        _farShadow = null;
        _shadowHost = GetTemplateChild(ShadowHostPartName) as Border;
        _trackSurface = GetTemplateChild(TrackSurfacePartName) as Border;
        _casterBrushSource = GetTemplateChild(CasterBrushSourcePartName) as Border;
        _shadowBrushSource = GetTemplateChild(ShadowBrushSourcePartName) as Border;
        InitializeShadows();
    }

    private void OnLoaded(object sender, RoutedEventArgs args) => InitializeShadows();

    private void InitializeShadows()
    {
        if (_shadowContainer is not null || _shadowHost is null || _trackSurface is null)
            return;

        var hostVisual = ElementCompositionPreview.GetElementVisual(_shadowHost);
        var trackVisual = ElementCompositionPreview.GetElementVisual(_trackSurface);
        var compositor = hostVisual.Compositor;

        // WinUI Composition's blur spread is wider than Figma's CSS shadow blur.
        // These calibrated values visually match Figma's 0 24px 36px layer.
        _farShadow = CreateShadowLayer(compositor, FarBlur, FarOffsetY, FarOpacity);
        _nearShadow = CreateShadowLayer(compositor, NearBlur, NearOffsetY, NearOpacity);

        _shadowContainer = compositor.CreateContainerVisual();
        _shadowContainer.Children.InsertAtBottom(_farShadow.Layer);
        _shadowContainer.Children.InsertAtTop(_nearShadow.Layer);
        ElementCompositionPreview.SetElementChildVisual(_shadowHost, _shadowContainer);

        // Hand-in visuals are clipped to the host UIElement. ShadowHost is wider than
        // TrackSurface (XAML Margin="-24,0") so the blurred far shadow has room to bleed
        // instead of ending at the track's edge. Bind sizes/offsets through
        // ExpressionAnimation so the island, the rounded caster, and the host stay in
        // the same space at any DPI.
        BindSize(_shadowContainer, hostVisual);
        _nearShadow.Bind(hostVisual, trackVisual);
        _farShadow.Bind(hostVisual, trackVisual);

        ApplyThemeBrushes();
    }

    private static void BindSize(Visual target, Visual sizeSource)
    {
        var animation = target.Compositor.CreateExpressionAnimation("source.Size");
        animation.SetReferenceParameter("source", sizeSource);
        target.StartAnimation("Size", animation);
    }

    private static void BindOffset(Visual target, Visual hostVisual, Visual trackVisual)
    {
        var animation = target.Compositor.CreateExpressionAnimation(
            "Vector3(trackVisual.Offset.X - hostVisual.Offset.X, trackVisual.Offset.Y - hostVisual.Offset.Y, 0)");
        animation.SetReferenceParameter("hostVisual", hostVisual);
        animation.SetReferenceParameter("trackVisual", trackVisual);
        target.StartAnimation("Offset", animation);
    }

    private ShadowLayer CreateShadowLayer(Compositor compositor, float blurRadius, float offsetY, float opacity)
    {
        var geometry = compositor.CreateRoundedRectangleGeometry();
        geometry.CornerRadius = new Vector2(TrackCornerRadius);

        var shape = compositor.CreateSpriteShape(geometry);
        // Colour is set per-theme via ApplyThemeBrushes; this shape only exists to cast the
        // shadow, so its own fill must blend with the canvas showing through the track.
        var fillBrush = compositor.CreateColorBrush(ReadBrushColor(_casterBrushSource));
        shape.FillBrush = fillBrush;

        var shapeVisual = compositor.CreateShapeVisual();
        shapeVisual.Shapes.Add(shape);

        var dropShadow = compositor.CreateDropShadow();
        // Encode Figma's 8% / 10% alpha in the color itself. This remains stable
        // when Composition inherits the source visual's alpha content. RGB comes
        // from EtherSegmentedControlShadowBrush so High Contrast can rebind.
        var shadowRgb = ReadBrushColor(_shadowBrushSource);
        dropShadow.Color = Color.FromArgb((byte)Math.Round(opacity * byte.MaxValue), shadowRgb.R, shadowRgb.G, shadowRgb.B);
        dropShadow.BlurRadius = blurRadius;
        dropShadow.Offset = new Vector3(0, offsetY, 0);
        dropShadow.Opacity = 1f;
        dropShadow.SourcePolicy = CompositionDropShadowSourcePolicy.InheritFromVisualContent;

        var layer = compositor.CreateLayerVisual();
        layer.Children.InsertAtTop(shapeVisual);
        layer.Shadow = dropShadow;
        return new ShadowLayer(layer, shapeVisual, geometry, fillBrush, dropShadow, opacity);
    }

    private sealed record ShadowLayer(
        LayerVisual Layer,
        ShapeVisual ShapeVisual,
        CompositionRoundedRectangleGeometry Geometry,
        CompositionColorBrush Fill,
        DropShadow Shadow,
        float Opacity)
    {
        public void Bind(Visual hostVisual, Visual trackVisual)
        {
            BindSize(Layer, hostVisual);
            BindSize(ShapeVisual, trackVisual);
            BindOffset(ShapeVisual, hostVisual, trackVisual);

            var animation = Geometry.Compositor.CreateExpressionAnimation("source.Size");
            animation.SetReferenceParameter("source", trackVisual);
            Geometry.StartAnimation("Size", animation);
        }

        public void SetFill(Color color) => Fill.Color = color;

        public void SetShadowRgb(Color rgb) =>
            Shadow.Color = Color.FromArgb((byte)Math.Round(Opacity * byte.MaxValue), rgb.R, rgb.G, rgb.B);
    }
}
