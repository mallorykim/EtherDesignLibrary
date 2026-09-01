using System;
using Microsoft.UI.Xaml.Media;

namespace Ether.DesignSystem.Controls;

/// <summary>
/// Creates a fully independent WinUI geometry object graph. WinUI geometries are dependency
/// objects and, unlike WPF Freezables, cannot be frozen or cloned by the platform for safe reuse.
/// </summary>
internal static class GeometryCloner
{
    internal static Geometry Clone(Geometry source)
    {
        ArgumentNullException.ThrowIfNull(source);

        Geometry clone = source switch
        {
            PathGeometry path => ClonePath(path),
            GeometryGroup group => CloneGroup(group),
            EllipseGeometry ellipse => new EllipseGeometry
            {
                Center = ellipse.Center,
                RadiusX = ellipse.RadiusX,
                RadiusY = ellipse.RadiusY,
            },
            LineGeometry line => new LineGeometry
            {
                StartPoint = line.StartPoint,
                EndPoint = line.EndPoint,
            },
            RectangleGeometry rectangle => new RectangleGeometry { Rect = rectangle.Rect },
            _ => throw new NotSupportedException($"Unsupported WinUI geometry type: {source.GetType().FullName}"),
        };

        clone.Transform = CloneTransform(source.Transform);
        return clone;
    }

    private static PathGeometry ClonePath(PathGeometry source)
    {
        var figures = new PathFigureCollection();
        foreach (var figure in source.Figures)
            figures.Add(CloneFigure(figure));

        return new PathGeometry
        {
            FillRule = source.FillRule,
            Figures = figures,
        };
    }

    private static PathFigure CloneFigure(PathFigure source)
    {
        var segments = new PathSegmentCollection();
        foreach (var segment in source.Segments)
            segments.Add(CloneSegment(segment));

        return new PathFigure
        {
            StartPoint = source.StartPoint,
            IsClosed = source.IsClosed,
            IsFilled = source.IsFilled,
            Segments = segments,
        };
    }

    private static PathSegment CloneSegment(PathSegment source) => source switch
    {
        ArcSegment arc => new ArcSegment
        {
            Point = arc.Point,
            Size = arc.Size,
            RotationAngle = arc.RotationAngle,
            IsLargeArc = arc.IsLargeArc,
            SweepDirection = arc.SweepDirection,
        },
        BezierSegment bezier => new BezierSegment
        {
            Point1 = bezier.Point1,
            Point2 = bezier.Point2,
            Point3 = bezier.Point3,
        },
        LineSegment line => new LineSegment { Point = line.Point },
        PolyBezierSegment polyBezier => new PolyBezierSegment { Points = ClonePoints(polyBezier.Points) },
        PolyLineSegment polyLine => new PolyLineSegment { Points = ClonePoints(polyLine.Points) },
        PolyQuadraticBezierSegment polyQuadratic => new PolyQuadraticBezierSegment
        {
            Points = ClonePoints(polyQuadratic.Points),
        },
        QuadraticBezierSegment quadratic => new QuadraticBezierSegment
        {
            Point1 = quadratic.Point1,
            Point2 = quadratic.Point2,
        },
        _ => throw new NotSupportedException($"Unsupported WinUI path segment type: {source.GetType().FullName}"),
    };

    private static PointCollection ClonePoints(PointCollection source)
    {
        var points = new PointCollection();
        foreach (var point in source)
            points.Add(point);

        return points;
    }

    private static GeometryGroup CloneGroup(GeometryGroup source)
    {
        var children = new GeometryCollection();
        foreach (var child in source.Children)
            children.Add(Clone(child));

        return new GeometryGroup
        {
            FillRule = source.FillRule,
            Children = children,
        };
    }

    private static Transform? CloneTransform(Transform? source) => source switch
    {
        null => null,
        CompositeTransform composite => new CompositeTransform
        {
            CenterX = composite.CenterX,
            CenterY = composite.CenterY,
            Rotation = composite.Rotation,
            ScaleX = composite.ScaleX,
            ScaleY = composite.ScaleY,
            SkewX = composite.SkewX,
            SkewY = composite.SkewY,
            TranslateX = composite.TranslateX,
            TranslateY = composite.TranslateY,
        },
        MatrixTransform matrix => new MatrixTransform { Matrix = matrix.Matrix },
        RotateTransform rotate => new RotateTransform
        {
            Angle = rotate.Angle,
            CenterX = rotate.CenterX,
            CenterY = rotate.CenterY,
        },
        ScaleTransform scale => new ScaleTransform
        {
            CenterX = scale.CenterX,
            CenterY = scale.CenterY,
            ScaleX = scale.ScaleX,
            ScaleY = scale.ScaleY,
        },
        SkewTransform skew => new SkewTransform
        {
            AngleX = skew.AngleX,
            AngleY = skew.AngleY,
            CenterX = skew.CenterX,
            CenterY = skew.CenterY,
        },
        TransformGroup group => CloneTransformGroup(group),
        TranslateTransform translate => new TranslateTransform
        {
            X = translate.X,
            Y = translate.Y,
        },
        _ => throw new NotSupportedException($"Unsupported WinUI transform type: {source.GetType().FullName}"),
    };

    private static TransformGroup CloneTransformGroup(TransformGroup source)
    {
        var children = new TransformCollection();
        foreach (var child in source.Children)
            children.Add(CloneTransform(child)!);

        return new TransformGroup { Children = children };
    }
}
