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
/// <c>EtherSegment</c> <see cref="RadioButton"/> style. This control does not add
/// selection APIs or VisualState groups on the host — native radio grouping and
/// <c>HandRadioButton</c> layer opacity remain the interaction model.
/// </remarks>
[TemplatePart(Name = ShadowHostPartName, Type = typeof(Canvas))]
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

    private Canvas? _shadowHost;
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
        SizeChanged += OnSizeChanged;
        ActualThemeChanged += OnActualThemeChanged;
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args) => ApplyThemeBrushes();

    /// <summary>
    /// The shadow caster is an opaque shape sitting behind the track surface. In light the
    /// surface (BackgroundSegmentTrack) is fully opaque and hides it, but in dark that token
    /// is only 10% alpha, so the caster shows straight through. It must therefore match the
    /// SURFACE the control sits on (BackgroundSurface), not the canvas — otherwise the
    /// translucent track reads a different shade over the caster than over the surface next to
    /// it, producing the patchy "grey base" (only in dark, and only where the caster reaches).
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
        _shadowHost = GetTemplateChild(ShadowHostPartName) as Canvas;
        _trackSurface = GetTemplateChild(TrackSurfacePartName) as Border;
        _casterBrushSource = GetTemplateChild(CasterBrushSourcePartName) as Border;
        _shadowBrushSource = GetTemplateChild(ShadowBrushSourcePartName) as Border;
        InitializeShadows();
    }

    private void OnLoaded(object sender, RoutedEventArgs args) => InitializeShadows();

    private void OnSizeChanged(object sender, SizeChangedEventArgs args) => UpdateShadowSize();

    private void InitializeShadows()
    {
        if (_shadowContainer is not null || _shadowHost is null || _trackSurface is null ||
            _trackSurface.ActualWidth <= 0 || _trackSurface.ActualHeight <= 0)
            return;

        var compositor = ElementCompositionPreview.GetElementVisual(_shadowHost).Compositor;
        // WinUI Composition's blur spread is wider than Figma's CSS shadow blur.
        // These calibrated values visually match Figma's 0 24px 36px layer.
        _farShadow = CreateShadowLayer(compositor, blurRadius: 18f, offsetY: 12f, opacity: 0.08f);
        _nearShadow = CreateShadowLayer(compositor, blurRadius: 4f, offsetY: 2f, opacity: 0.10f);

        _shadowContainer = compositor.CreateContainerVisual();
        _shadowContainer.Children.InsertAtBottom(_farShadow.Layer);
        _shadowContainer.Children.InsertAtTop(_nearShadow.Layer);
        ElementCompositionPreview.SetElementChildVisual(_shadowHost, _shadowContainer);

        ApplyThemeBrushes();
        UpdateShadowSize();
    }

    private void UpdateShadowSize()
    {
        if (_shadowContainer is null || _nearShadow is null || _farShadow is null ||
            _shadowHost is null || _trackSurface is null)
            return;

        var size = new Vector2((float)_trackSurface.ActualWidth, (float)_trackSurface.ActualHeight);
        var origin = _trackSurface.TransformToVisual(_shadowHost).TransformPoint(new Windows.Foundation.Point());
        _shadowContainer.Offset = new Vector3((float)origin.X, (float)origin.Y, 0);
        _shadowContainer.Size = size;
        _nearShadow.Resize(size);
        _farShadow.Resize(size);
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
        public void Resize(Vector2 size)
        {
            Layer.Size = size;
            ShapeVisual.Size = size;
            Geometry.Size = size;
        }

        public void SetFill(Color color) => Fill.Color = color;

        public void SetShadowRgb(Color rgb) =>
            Shadow.Color = Color.FromArgb((byte)Math.Round(Opacity * byte.MaxValue), rgb.R, rgb.G, rgb.B);
    }
}
