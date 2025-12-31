using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace SeiWoLauncherPro.Controls
{
    public enum BorderPosition
    {
        Inside,
        Center,
        Outside
    }

    /// <summary>
    /// A Decorator that renders a border with continuous "Squircle" corners (super-ellipse).
    /// Includes support for multi-layer backgrounds and optimized drop shadows.
    /// </summary>
    public class SmoothBorder : Decorator
    {
        private readonly DrawingVisual _mainVisual = new DrawingVisual();
        private readonly DrawingVisual _shadowVisual = new DrawingVisual();
        private Geometry _hitTestGeometry;

        public SmoothBorder()
        {
            SetCurrentValue(BackgroundLayersProperty, new FreezableCollection<Brush>());

            // Ensure visuals are attached for rendering and hit-testing
            AddVisualChild(_shadowVisual);
            AddVisualChild(_mainVisual);
        }

        #region Dependency Properties

        // --- Appearance ---

        public static readonly DependencyProperty BackgroundProperty =
            DependencyProperty.Register(nameof(Background), typeof(Brush), typeof(SmoothBorder),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush Background
        {
            get => (Brush)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public static readonly DependencyProperty BackgroundLayersProperty =
            DependencyProperty.Register(nameof(BackgroundLayers), typeof(FreezableCollection<Brush>), typeof(SmoothBorder),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnBackgroundLayersChanged));

        public FreezableCollection<Brush> BackgroundLayers
        {
            get => (FreezableCollection<Brush>)GetValue(BackgroundLayersProperty);
            set => SetValue(BackgroundLayersProperty, value);
        }

        public static readonly DependencyProperty BorderBrushProperty =
            DependencyProperty.Register(nameof(BorderBrush), typeof(Brush), typeof(SmoothBorder),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush BorderBrush
        {
            get => (Brush)GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }

        public static readonly DependencyProperty StrokeDashArrayProperty =
            DependencyProperty.Register(nameof(StrokeDashArray), typeof(DoubleCollection), typeof(SmoothBorder),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public DoubleCollection StrokeDashArray
        {
            get => (DoubleCollection)GetValue(StrokeDashArrayProperty);
            set => SetValue(StrokeDashArrayProperty, value);
        }

        // --- Layout ---

        public static readonly DependencyProperty BorderThicknessProperty =
            DependencyProperty.Register(nameof(BorderThickness), typeof(double), typeof(SmoothBorder),
                new FrameworkPropertyMetadata(1.0,
                    FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public double BorderThickness
        {
            get => (double)GetValue(BorderThicknessProperty);
            set => SetValue(BorderThicknessProperty, value);
        }

        public static readonly DependencyProperty PaddingProperty =
            DependencyProperty.Register(nameof(Padding), typeof(Thickness), typeof(SmoothBorder),
                new FrameworkPropertyMetadata(new Thickness(0),
                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        public Thickness Padding
        {
            get => (Thickness)GetValue(PaddingProperty);
            set => SetValue(PaddingProperty, value);
        }

        public static readonly DependencyProperty BorderPositionProperty =
            DependencyProperty.Register(nameof(BorderPosition), typeof(BorderPosition), typeof(SmoothBorder),
                new FrameworkPropertyMetadata(BorderPosition.Inside, FrameworkPropertyMetadataOptions.AffectsRender));

        public BorderPosition BorderPosition
        {
            get => (BorderPosition)GetValue(BorderPositionProperty);
            set => SetValue(BorderPositionProperty, value);
        }

        public static readonly DependencyProperty CornerClipProperty =
            DependencyProperty.Register(nameof(CornerClip), typeof(bool), typeof(SmoothBorder),
                new FrameworkPropertyMetadata(false,
                    FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsArrange));

        /// <summary>
        /// If true, clips the Child content to the smooth rounded geometry.
        /// </summary>
        public bool CornerClip
        {
            get => (bool)GetValue(CornerClipProperty);
            set => SetValue(CornerClipProperty, value);
        }

        // --- Geometry (Squircle) ---

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(SmoothBorder),
                new FrameworkPropertyMetadata(new CornerRadius(0), FrameworkPropertyMetadataOptions.AffectsRender));

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public static readonly DependencyProperty SmoothnessProperty =
            DependencyProperty.Register(nameof(Smoothness), typeof(double), typeof(SmoothBorder),
                new FrameworkPropertyMetadata(0.6, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// 0.0 (Circular) to 1.0 (Highly smoothed). Default 0.6 mimics iOS continuous corners.
        /// </summary>
        public double Smoothness
        {
            get => (double)GetValue(SmoothnessProperty);
            set => SetValue(SmoothnessProperty, value);
        }

        // --- Shadow ---

        public static readonly DependencyProperty ShadowColorProperty =
            DependencyProperty.Register(nameof(ShadowColor), typeof(Color), typeof(SmoothBorder),
                new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.AffectsRender));

        public Color ShadowColor
        {
            get => (Color)GetValue(ShadowColorProperty);
            set => SetValue(ShadowColorProperty, value);
        }

        public static readonly DependencyProperty ShadowBlurRadiusProperty =
            DependencyProperty.Register(nameof(ShadowBlurRadius), typeof(double), typeof(SmoothBorder),
                new FrameworkPropertyMetadata(5.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public double ShadowBlurRadius
        {
            get => (double)GetValue(ShadowBlurRadiusProperty);
            set => SetValue(ShadowBlurRadiusProperty, value);
        }

        public static readonly DependencyProperty ShadowDepthProperty =
            DependencyProperty.Register(nameof(ShadowDepth), typeof(double), typeof(SmoothBorder),
                new FrameworkPropertyMetadata(5.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public double ShadowDepth
        {
            get => (double)GetValue(ShadowDepthProperty);
            set => SetValue(ShadowDepthProperty, value);
        }

        public static readonly DependencyProperty ShadowDirectionProperty =
            DependencyProperty.Register(nameof(ShadowDirection), typeof(double), typeof(SmoothBorder),
                new FrameworkPropertyMetadata(315.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public double ShadowDirection
        {
            get => (double)GetValue(ShadowDirectionProperty);
            set => SetValue(ShadowDirectionProperty, value);
        }

        public static readonly DependencyProperty ShadowOpacityProperty =
            DependencyProperty.Register(nameof(ShadowOpacity), typeof(double), typeof(SmoothBorder),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public double ShadowOpacity
        {
            get => (double)GetValue(ShadowOpacityProperty);
            set => SetValue(ShadowOpacityProperty, value);
        }

        public static readonly DependencyProperty ShadowNoCasterProperty =
            DependencyProperty.Register(nameof(ShadowNoCaster), typeof(bool), typeof(SmoothBorder),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// If true, cuts a hole in the shadow corresponding to the element's shape.
        /// Prevents shadow from showing through semi-transparent backgrounds.
        /// </summary>
        public bool ShadowNoCaster
        {
            get => (bool)GetValue(ShadowNoCasterProperty);
            set => SetValue(ShadowNoCasterProperty, value);
        }

        #endregion

        #region Property Change Handlers

        private static void OnBackgroundLayersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SmoothBorder border)
            {
                if (e.OldValue is INotifyCollectionChanged oldC) oldC.CollectionChanged -= border.OnBrushCollectionChanged;
                if (e.NewValue is INotifyCollectionChanged newC) newC.CollectionChanged += border.OnBrushCollectionChanged;
                border.UpdateVisuals();
            }
        }

        private void OnBrushCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => UpdateVisuals();

        #endregion

        #region Visual Tree Overrides

        protected override int VisualChildrenCount => base.VisualChildrenCount + 2;

        protected override Visual GetVisualChild(int index)
        {
            // Z-Order: Shadow (0) -> Main/Border (1) -> Child Content (2)
            if (index == 0) return _shadowVisual;
            if (index == 1) return _mainVisual;

            if (index == 2 && base.VisualChildrenCount > 0)
                return base.GetVisualChild(0);

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        #endregion

        #region Layout Overrides

        protected override Size MeasureOverride(Size constraint)
        {
            var child = Child;
            var overheadH = Padding.Left + Padding.Right + (2 * BorderThickness);
            var overheadV = Padding.Top + Padding.Bottom + (2 * BorderThickness);

            if (child != null)
            {
                var availableSize = new Size(
                    Math.Max(0, constraint.Width - overheadH),
                    Math.Max(0, constraint.Height - overheadV));

                child.Measure(availableSize);

                return new Size(
                    child.DesiredSize.Width + overheadH,
                    child.DesiredSize.Height + overheadV);
            }

            return new Size(overheadH, overheadV);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var child = Child;
            if (child != null)
            {
                var thickness = BorderThickness;
                var padding = Padding;

                var innerRect = new Rect(
                    thickness + padding.Left,
                    thickness + padding.Top,
                    Math.Max(0, finalSize.Width - (thickness * 2) - padding.Left - padding.Right),
                    Math.Max(0, finalSize.Height - (thickness * 2) - padding.Top - padding.Bottom));

                child.Arrange(innerRect);

                if (CornerClip)
                {
                    // Clip the child relative to its own coordinate space
                    var clipRect = new Rect(-innerRect.Left, -innerRect.Top, finalSize.Width, finalSize.Height);
                    child.Clip = GetSmoothGeometry(clipRect, BorderPosition.Center);
                }
                else
                {
                    child.Clip = null;
                }
            }

            // Update hit-test geometry based on the full bounds
            var rect = new Rect(0, 0, finalSize.Width, finalSize.Height);
            if (rect.Width > 0 && rect.Height > 0)
            {
                _hitTestGeometry = GetSmoothGeometry(rect, BorderPosition.Center);
            }

            return finalSize;
        }

        #endregion

        #region Rendering & Hit Testing

        protected override void OnRender(DrawingContext drawingContext)
        {
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            var rect = new Rect(0, 0, ActualWidth, ActualHeight);
            if (rect.Width == 0 || rect.Height == 0) return;

            var thickness = BorderThickness;

            // Adjust geometry rect based on BorderPosition
            Rect drawRect = rect;
            switch (BorderPosition)
            {
                case BorderPosition.Inside:
                    drawRect.Inflate(-thickness / 2, -thickness / 2);
                    break;
                case BorderPosition.Outside:
                    drawRect.Inflate(thickness / 2, thickness / 2);
                    break;
            }

            var geometry = GetSmoothGeometry(drawRect, BorderPosition);
            _hitTestGeometry = geometry; // Sync hit test geometry

            RenderShadow(geometry);
            RenderMain(geometry, thickness);
        }

        private void RenderShadow(Geometry geometry)
        {
            using (DrawingContext dc = _shadowVisual.RenderOpen())
            {
                if (ShadowOpacity <= 0)
                {
                    _shadowVisual.Effect = null;
                    return;
                }

                var effect = new DropShadowEffect
                {
                    Color = ShadowColor,
                    BlurRadius = ShadowBlurRadius,
                    ShadowDepth = ShadowDepth,
                    Direction = ShadowDirection,
                    Opacity = ShadowOpacity
                };
                if (effect.CanFreeze) effect.Freeze();
                _shadowVisual.Effect = effect;

                // Draw the caster
                dc.DrawGeometry(Brushes.Black, null, geometry);

                if (ShadowNoCaster)
                {
                    // Exclude the caster geometry to prevent shadow darkening semi-transparent backgrounds
                    var infiniteRect = new RectangleGeometry(new Rect(-10000, -10000, 20000, 20000));
                    var hollowGeometry = Geometry.Combine(infiniteRect, geometry, GeometryCombineMode.Exclude, null);
                    if (hollowGeometry.CanFreeze) hollowGeometry.Freeze();

                    _shadowVisual.Clip = hollowGeometry;
                }
                else
                {
                    _shadowVisual.Clip = null;
                }
            }
        }

        private void RenderMain(Geometry geometry, double thickness)
        {
            using (DrawingContext dc = _mainVisual.RenderOpen())
            {
                // 1. Draw Background
                if (Background != null)
                {
                    dc.DrawGeometry(Background, null, geometry);
                }

                // 2. Draw Layered Backgrounds
                var layers = BackgroundLayers;
                if (layers != null)
                {
                    foreach (var layer in layers)
                    {
                        if (layer != null) dc.DrawGeometry(layer, null, geometry);
                    }
                }

                // 3. Draw Border
                if (BorderBrush != null && thickness > 0)
                {
                    var pen = new Pen(BorderBrush, thickness);
                    var dashes = StrokeDashArray;
                    if (dashes != null && dashes.Count > 0)
                    {
                        pen.DashStyle = new DashStyle(dashes, 0);
                    }
                    if (pen.CanFreeze) pen.Freeze();

                    dc.DrawGeometry(null, pen, geometry);
                }
            }
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            if (_hitTestGeometry != null && _hitTestGeometry.FillContains(hitTestParameters.HitPoint))
            {
                return new PointHitTestResult(this, hitTestParameters.HitPoint);
            }
            return null;
        }

        #endregion

        #region Math & Geometry Generation

        private Geometry GetSmoothGeometry(Rect rect, BorderPosition pos)
        {
            var radii = CornerRadius;
            var smoothness = Math.Clamp(Smoothness, 0, 1);

            var tl = new CornerAttributes(radii.TopLeft, smoothness);
            var tr = new CornerAttributes(radii.TopRight, smoothness);
            var bl = new CornerAttributes(radii.BottomLeft, smoothness);
            var br = new CornerAttributes(radii.BottomRight, smoothness);

            var rectAttr = new SmoothRectangleAttributes(tr, br, bl, tl);
            var normRect = NormalizeCorners(rect, rectAttr);

            StreamGeometry geom = new StreamGeometry();
            using (StreamGeometryContext ctx = geom.Open())
            {
                ctx.BeginFigure(new Point(rect.Left + normRect.TopLeft.SegmentLength, rect.Top), true, true);
                DrawCornerPath(ctx, rect, normRect.TopRight, Corner.TopRight);
                DrawCornerPath(ctx, rect, normRect.BottomRight, Corner.BottomRight);
                DrawCornerPath(ctx, rect, normRect.BottomLeft, Corner.BottomLeft);
                DrawCornerPath(ctx, rect, normRect.TopLeft, Corner.TopLeft);
            }

            geom.Freeze();
            return geom;
        }

        private void DrawCornerPath(StreamGeometryContext ctx, Rect rect, CornerAttributes attributes, Corner corner)
        {
            if (attributes.Radius <= 0)
            {
                ctx.LineTo(CurveStart(rect, corner, 0), true, true);
                return;
            }

            var prms = ComputeParameters(rect, attributes);
            var points = ComputeCurvePoints(prms, rect, corner);

            var center = CenterPoint(rect, corner, prms.r);
            var startAng = StartAngle(corner);

            // 1. Line to start
            ctx.LineTo(CurveStart(rect, corner, prms.p), true, true);

            // 2. First Bezier
            ctx.BezierTo(points[1], points[2], points[0], true, true);

            // 3. Arc
            double arcEndAngle = startAng + 90 - prms.theta;
            double endRad = arcEndAngle * (Math.PI / 180.0);
            Point arcEnd = new Point(
                center.X + prms.r * Math.Cos(endRad),
                center.Y + prms.r * Math.Sin(endRad)
            );

            ctx.ArcTo(arcEnd, new Size(prms.r, prms.r), 0, false, SweepDirection.Clockwise, true, true);

            // 4. Second Bezier
            ctx.BezierTo(points[4], points[5], points[3], true, true);
        }

        // --- Core Algorithm Helper Structs ---

        private enum Corner { TopRight, BottomRight, BottomLeft, TopLeft }

        private readonly struct CornerAttributes
        {
            public readonly double Radius;
            public readonly double Smoothness;
            public readonly double SegmentLength;

            public CornerAttributes(double radius, double smoothness)
            {
                Radius = radius;
                Smoothness = smoothness;
                SegmentLength = radius * (1 + smoothness);
            }
        }

        private readonly struct SmoothRectangleAttributes
        {
            public readonly CornerAttributes TopRight;
            public readonly CornerAttributes BottomRight;
            public readonly CornerAttributes BottomLeft;
            public readonly CornerAttributes TopLeft;

            public SmoothRectangleAttributes(CornerAttributes tr, CornerAttributes br, CornerAttributes bl, CornerAttributes tl)
            {
                TopRight = tr;
                BottomRight = br;
                BottomLeft = bl;
                TopLeft = tl;
            }
        }

        private struct SmoothCornerParameters
        {
            public double a, b, c, d, p, r, theta;
        }

        private SmoothRectangleAttributes NormalizeCorners(Rect rect, SmoothRectangleAttributes rectAttr)
        {
            var tr = GetNormalizedCorner(rectAttr.TopRight, rect, rectAttr.BottomRight, rectAttr.TopLeft);
            var br = GetNormalizedCorner(rectAttr.BottomRight, rect, rectAttr.TopRight, rectAttr.BottomLeft);
            var bl = GetNormalizedCorner(rectAttr.BottomLeft, rect, rectAttr.TopLeft, rectAttr.BottomRight);
            var tl = GetNormalizedCorner(rectAttr.TopLeft, rect, rectAttr.BottomLeft, rectAttr.TopRight);

            return new SmoothRectangleAttributes(tr, br, bl, tl);
        }

        private CornerAttributes GetNormalizedCorner(CornerAttributes baseAttr, Rect rect, CornerAttributes vert, CornerAttributes horiz)
        {
            var (trR1, trS1) = CalculateNormalization(baseAttr, horiz, rect.Width);
            var (trR2, trS2) = CalculateNormalization(baseAttr, vert, rect.Height);
            return new CornerAttributes(Math.Min(trR1, trR2), Math.Min(trS1, trS2));
        }

        private (double, double) CalculateNormalization(CornerAttributes baseAttr, CornerAttributes adjacent, double edge)
        {
            if ((baseAttr.Radius + adjacent.Radius) >= edge)
            {
                double scaleFactor = edge / (baseAttr.Radius + adjacent.Radius);
                return (baseAttr.Radius * scaleFactor, 0);
            }

            if ((baseAttr.SegmentLength + adjacent.SegmentLength) > edge)
            {
                double scaleFactor = edge / (baseAttr.SegmentLength + adjacent.SegmentLength);
                return (baseAttr.Radius, (1 + baseAttr.Smoothness) * scaleFactor - 1);
            }

            return (baseAttr.Radius, baseAttr.Smoothness);
        }

        private SmoothCornerParameters ComputeParameters(Rect rect, CornerAttributes attr)
        {
            double p = (1 + attr.Smoothness) * attr.Radius;
            double angleTheta = 45 * attr.Smoothness;
            double radTheta = angleTheta * (Math.PI / 180.0);
            double radBeta = (90 * (1 - attr.Smoothness)) * (Math.PI / 180.0);

            double c = attr.Radius * Math.Tan(radTheta / 2) * Math.Cos(radTheta);
            double d = attr.Radius * Math.Tan(radTheta / 2) * Math.Sin(radTheta);
            double arcSeg = Math.Sin(radBeta / 2) * attr.Radius * Math.Sqrt(2);

            double b = (p - arcSeg - c - d) / 3;
            double a = 2 * b;

            return new SmoothCornerParameters { a = a, b = b, c = c, d = d, p = p, r = attr.Radius, theta = angleTheta };
        }

        private Point[] ComputeCurvePoints(SmoothCornerParameters prms, Rect rect, Corner corner)
        {
            double a = prms.a, b = prms.b, c = prms.c, d = prms.d, p = prms.p;
            double w = rect.Width, h = rect.Height, x = rect.X, y = rect.Y;

            Point Pt(double _x, double _y) => new Point(x + _x, y + _y);

            return corner switch
            {
                Corner.TopRight => new[] {
                    Pt(w - (p - a - b - c), d), Pt(w - (p - a), 0), Pt(w - (p - a - b), 0),
                    Pt(w, p), Pt(w, p - a - b), Pt(w, p - a)
                },
                Corner.BottomRight => new[] {
                    Pt(w - d, h - (p - a - b - c)), Pt(w, h - (p - a)), Pt(w, h - (p - a - b)),
                    Pt(w - p, h), Pt(w - (p - a - b), h), Pt(w - (p - a), h)
                },
                Corner.BottomLeft => new[] {
                    Pt(p - a - b - c, h - d), Pt(p - a, h), Pt(p - a - b, h),
                    Pt(0, h - p), Pt(0, h - (p - a - b)), Pt(0, h - (p - a))
                },
                Corner.TopLeft => new[] {
                    Pt(d, p - a - b - c), Pt(0, p - a), Pt(0, p - a - b),
                    Pt(p, 0), Pt(p - a - b, 0), Pt(p - a, 0)
                },
                _ => Array.Empty<Point>()
            };
        }

        private Point CurveStart(Rect rect, Corner corner, double p)
        {
            return corner switch
            {
                Corner.TopRight => new Point(rect.Right - p, rect.Top),
                Corner.BottomRight => new Point(rect.Right, rect.Bottom - p),
                Corner.BottomLeft => new Point(rect.Left + p, rect.Bottom),
                Corner.TopLeft => new Point(rect.Left, rect.Top + p),
                _ => new Point()
            };
        }

        private double StartAngle(Corner corner)
        {
            return corner switch
            {
                Corner.TopRight => 270,
                Corner.BottomRight => 0,
                Corner.BottomLeft => 90,
                Corner.TopLeft => 180,
                _ => 0
            };
        }

        private Point CenterPoint(Rect rect, Corner corner, double r)
        {
            return corner switch
            {
                Corner.TopRight => new Point(rect.Right - r, rect.Top + r),
                Corner.BottomRight => new Point(rect.Right - r, rect.Bottom - r),
                Corner.BottomLeft => new Point(rect.Left + r, rect.Bottom - r),
                Corner.TopLeft => new Point(rect.Left + r, rect.Top + r),
                _ => new Point()
            };
        }

        #endregion
    }
}
