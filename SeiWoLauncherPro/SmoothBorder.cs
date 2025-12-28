using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects; // 添加 Effects 命名空间

namespace SeiWoLauncherPro
{
    public enum BorderPosition
    {
        Inside,
        Center,
        Outside
    }

    /// <summary>
    /// A Decorator that draws a border with "Smooth" (Squircle/Continuous) corners.
    /// Logic adapted from Swift SmoothRoundedRectangle.
    /// </summary>
    public class SmoothBorder : Decorator
    {
        // 定义内部 Visuals
        private readonly DrawingVisual _mainVisual = new DrawingVisual();
        private readonly DrawingVisual _shadowVisual = new DrawingVisual();

        #region Dependency Properties

        public static readonly DependencyProperty BackgroundProperty =
            DependencyProperty.Register(nameof(Background), typeof(Brush), typeof(SmoothBorder),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush Background
        {
            get => (Brush)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public static readonly DependencyProperty BorderBrushProperty =
            DependencyProperty.Register(nameof(BorderBrush), typeof(Brush), typeof(SmoothBorder),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush BorderBrush
        {
            get => (Brush)GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }

        public static readonly DependencyProperty BorderThicknessProperty =
            DependencyProperty.Register(nameof(BorderThickness), typeof(double), typeof(SmoothBorder),
                new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public double BorderThickness
        {
            get => (double)GetValue(BorderThicknessProperty);
            set => SetValue(BorderThicknessProperty, value);
        }

        public static readonly DependencyProperty PaddingProperty =
            DependencyProperty.Register(nameof(Padding), typeof(Thickness), typeof(SmoothBorder),
                new FrameworkPropertyMetadata(new Thickness(0), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        public Thickness Padding
        {
            get => (Thickness)GetValue(PaddingProperty);
            set => SetValue(PaddingProperty, value);
        }

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
        /// Factor between 0 (Circular) and 1.0 (Highly smoothed). Default is 0.6 (Apple Continuous style).
        /// </summary>
        public double Smoothness
        {
            get => (double)GetValue(SmoothnessProperty);
            set => SetValue(SmoothnessProperty, value);
        }

        public static readonly DependencyProperty BorderPositionProperty =
            DependencyProperty.Register(nameof(BorderPosition), typeof(BorderPosition), typeof(SmoothBorder),
                new FrameworkPropertyMetadata(BorderPosition.Inside, FrameworkPropertyMetadataOptions.AffectsRender));

        public BorderPosition BorderPosition
        {
            get => (BorderPosition)GetValue(BorderPositionProperty);
            set => SetValue(BorderPositionProperty, value);
        }

        // 修改 CornerClip 的 Metadata 为 AffectsArrange，因为我们需要在 Arrange 中重新计算子元素的 Clip
        public static readonly DependencyProperty CornerClipProperty =
            DependencyProperty.Register(nameof(CornerClip), typeof(bool), typeof(SmoothBorder),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsArrange));

        public bool CornerClip
        {
            get => (bool)GetValue(CornerClipProperty);
            set => SetValue(CornerClipProperty, value);
        }

        // --- 新增 Shadow 属性 ---

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

        #endregion

        #region Visual Tree Overrides

        // 重写 VisualChildrenCount，增加 MainVisual 和 ShadowVisual
        protected override int VisualChildrenCount => base.VisualChildrenCount + 2;

        // 重写 GetVisualChild 以控制绘制顺序
        protected override Visual GetVisualChild(int index)
        {
            // 顺序：
            // 0: Shadow (最底层)
            // 1: Main (背景与边框，遮挡 Shadow 的实体部分)
            // 2: Child (内容，最顶层)
            
            if (index == 0) return _shadowVisual;
            if (index == 1) return _mainVisual;
            
            // Decorator 的 Child 在 base.GetVisualChild(0) 中
            // 如果 Child 存在，它是第3个元素 (index 2)
            if (index == 2 && base.VisualChildrenCount > 0)
            {
                return base.GetVisualChild(0);
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        #endregion

        #region Layout Overrides

        protected override Size MeasureOverride(Size constraint)
        {
            var child = Child;
            var borderThickness = BorderThickness;
            var padding = Padding;

            // Calculate total size consumed by border and padding
            var horizontalOverhead = padding.Left + padding.Right + (2 * borderThickness);
            var verticalOverhead = padding.Top + padding.Bottom + (2 * borderThickness);

            if (child != null)
            {
                var availableSize = new Size(
                    Math.Max(0, constraint.Width - horizontalOverhead),
                    Math.Max(0, constraint.Height - verticalOverhead));

                child.Measure(availableSize);

                return new Size(
                    child.DesiredSize.Width + horizontalOverhead,
                    child.DesiredSize.Height + verticalOverhead);
            }

            return new Size(horizontalOverhead, verticalOverhead);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var child = Child;
            if (child != null)
            {
                var borderThickness = BorderThickness;
                var padding = Padding;

                // Border logic: Usually standard WPF Border draws "Inside" relative to layout bounds.
                // We offset the child by Thickness + Padding.
                var innerRect = new Rect(
                    borderThickness + padding.Left,
                    borderThickness + padding.Top,
                    Math.Max(0, finalSize.Width - (borderThickness * 2) - padding.Left - padding.Right),
                    Math.Max(0, finalSize.Height - (borderThickness * 2) - padding.Top - padding.Bottom));

                child.Arrange(innerRect);

                // Handle Clipping logic for Child specifically
                if (CornerClip)
                {
                    // 我们需要将 Clip 应用于 Child，而不是 this，否则会裁掉外阴影。
                    // 计算相对于 Child 的几何图形（反向平移）
                    var clipRect = new Rect(
                        -innerRect.Left, 
                        -innerRect.Top, 
                        finalSize.Width, 
                        finalSize.Height);
                    
                    var clipGeometry = GetSmoothGeometry(clipRect, BorderPosition.Center);
                    child.Clip = clipGeometry;
                }
                else
                {
                    child.Clip = null;
                }
            }

            // 移除原本对 this.Clip 的设置，因为它会裁剪掉阴影
            // if (CornerClip) ... this.Clip = ... 

            return finalSize;
        }

        #endregion

        #region Rendering

        protected override void OnRender(DrawingContext drawingContext)
        {
            // OnRender 不再直接绘制，而是触发我们自定义 Visual 的更新。
            // 实际上由于使用了 AffectsRender，每次属性变更都会调用 OnRender。
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            var rect = new Rect(0, 0, ActualWidth, ActualHeight);
            if (rect.Width == 0 || rect.Height == 0) return;

            var thickness = BorderThickness;
            var brush = BorderBrush;
            var background = Background;

            // Adjust Rect based on BorderPosition
            Rect drawRect = rect;
            switch (BorderPosition)
            {
                case BorderPosition.Inside:
                    drawRect.Inflate(-thickness / 2, -thickness / 2);
                    break;
                case BorderPosition.Center:
                    break;
                case BorderPosition.Outside:
                    drawRect.Inflate(thickness / 2, thickness / 2);
                    break;
            }

            var geometry = GetSmoothGeometry(drawRect, BorderPosition);

            // 1. Update Shadow Visual (最底层)
            using (DrawingContext dc = _shadowVisual.RenderOpen())
            {
                if (ShadowOpacity > 0)
                {
                    var effect = new DropShadowEffect
                    {
                        Color = ShadowColor,
                        BlurRadius = ShadowBlurRadius,
                        ShadowDepth = ShadowDepth,
                        Direction = ShadowDirection,
                        Opacity = ShadowOpacity
                    };
                    
                    // 必须 Freeze 否则可能有性能问题或线程问题
                    if (effect.CanFreeze) effect.Freeze();
                    _shadowVisual.Effect = effect;

                    // 绘制阴影产生源（Caster）。
                    // 我们绘制一个黑色的形状，它会被 MainVisual 的背景遮挡，只露出 Effect 产生的阴影。
                    // 注意：如果 Background 是半透明的，可能会看到下面的黑色 Caster。这是 WPF 实现此类阴影的常见限制。
                    dc.DrawGeometry(Brushes.Black, null, geometry);
                }
                else
                {
                    _shadowVisual.Effect = null;
                }
            }

            // 2. Update Main Visual (中间层 - 背景和边框)
            using (DrawingContext dc = _mainVisual.RenderOpen())
            {
                if (background != null || (brush != null && thickness > 0))
                {
                    Pen pen = null;
                    if (brush != null && thickness > 0)
                    {
                        pen = new Pen(brush, thickness);
                        if (pen.CanFreeze) pen.Freeze();
                    }
                    dc.DrawGeometry(background, pen, geometry);
                }
            }
        }

        #endregion

        #region Math & Geometry Generation

        private Geometry GetSmoothGeometry(Rect rect, BorderPosition pos)
        {
            var radii = CornerRadius;
            var smoothness = Math.Max(0, Math.Min(1, Smoothness));

            // Map standard CornerRadius to internal structs
            var tl = new CornerAttributes(radii.TopLeft, smoothness);
            var tr = new CornerAttributes(radii.TopRight, smoothness);
            var bl = new CornerAttributes(radii.BottomLeft, smoothness);
            var br = new CornerAttributes(radii.BottomRight, smoothness);

            var rectAttr = new SmoothRectangleAttributes(tr, br, bl, tl);

            // Normalize corners (handle overlapping radii)
            var normRect = NormalizeCorners(rect, rectAttr);

            StreamGeometry geom = new StreamGeometry();

            using (StreamGeometryContext ctx = geom.Open())
            {
                // Start Point: Top edge, after top-left corner
                ctx.BeginFigure(new Point(rect.Left + normRect.TopLeft.SegmentLength, rect.Top), true, true);

                // Draw corners
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
            if (attributes.Radius > 0)
            {
                var prms = ComputeParameters(rect, attributes);
                var points = ComputeCurvePoints(prms, rect, corner);

                // Unpack params for Arc calculation
                var center = CenterPoint(rect, corner, prms.r);
                var startAng = StartAngle(corner);

                // 1. Line to start of curve
                var curveStartPt = CurveStart(rect, corner, prms.p);
                ctx.LineTo(curveStartPt, true, true);

                // 2. First Bezier (Ramp up)
                ctx.BezierTo(points[1], points[2], points[0], true, true);

                // 3. Circular Arc
                double arcStartAngle = startAng + prms.theta;
                double arcEndAngle = startAng + 90 - prms.theta;

                // Calculate endpoint of arc
                double endRad = arcEndAngle * (Math.PI / 180.0);
                Point arcEnd = new Point(
                    center.X + prms.r * Math.Cos(endRad),
                    center.Y + prms.r * Math.Sin(endRad)
                );

                ctx.ArcTo(
                    arcEnd,
                    new Size(prms.r, prms.r),
                    0,
                    false, // isLargeArc
                    SweepDirection.Clockwise,
                    true,
                    true
                );

                // 4. Second Bezier (Ramp down)
                ctx.BezierTo(points[4], points[5], points[3], true, true);
            }
            else
            {
                // Square corner
                ctx.LineTo(CurveStart(rect, corner, 0), true, true);
            }
        }

        // --- Core Algorithm Ports (Structures & Logic) ---

        private enum Corner { TopRight, BottomRight, BottomLeft, TopLeft }

        private struct CornerAttributes
        {
            public double Radius;
            public double Smoothness;
            public double SegmentLength;

            public CornerAttributes(double radius, double smoothness)
            {
                Radius = radius;
                Smoothness = smoothness;
                SegmentLength = radius * (1 + smoothness);
            }
        }

        private struct SmoothRectangleAttributes
        {
            public CornerAttributes TopRight;
            public CornerAttributes BottomRight;
            public CornerAttributes BottomLeft;
            public CornerAttributes TopLeft;

            public SmoothRectangleAttributes(CornerAttributes tr, CornerAttributes br, CornerAttributes bl, CornerAttributes tl)
            {
                TopRight = tr; BottomRight = br; BottomLeft = bl; TopLeft = tl;
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

        private CornerAttributes GetNormalizedCorner(CornerAttributes baseAttr, Rect rect, CornerAttributes vertNeighbour, CornerAttributes horizNeighbour)
        {
            var (trR1, trS1) = CalculateNormalization(baseAttr, horizNeighbour, rect.Width);
            var (trR2, trS2) = CalculateNormalization(baseAttr, vertNeighbour, rect.Height);
            return new CornerAttributes(Math.Min(trR1, trR2), Math.Min(trS1, trS2));
        }

        private (double, double) CalculateNormalization(CornerAttributes baseAttr, CornerAttributes adjacent, double edge)
        {
            if ((baseAttr.Radius + adjacent.Radius) >= edge)
            {
                double scaleFactor = edge / (baseAttr.Radius + adjacent.Radius);
                return (baseAttr.Radius * scaleFactor, 0);
            }
            else if ((baseAttr.SegmentLength + adjacent.SegmentLength) > edge)
            {
                double scaleFactor = edge / (baseAttr.SegmentLength + adjacent.SegmentLength);
                return (baseAttr.Radius, (1 + baseAttr.Smoothness) * scaleFactor - 1);
            }
            else
            {
                return (baseAttr.Radius, baseAttr.Smoothness);
            }
        }

        private SmoothCornerParameters ComputeParameters(Rect rect, CornerAttributes attr)
        {
            double smoothnessFactor = attr.Smoothness;
            double p = (1 + smoothnessFactor) * attr.Radius;

            double angleBeta = 90 * (1 - smoothnessFactor);
            double angleTheta = 45 * smoothnessFactor;

            double radTheta = angleTheta * (Math.PI / 180.0);
            double radBeta = angleBeta * (Math.PI / 180.0);

            // Swift: let c = radius * tan(theta/2) * cos(theta)
            double c = attr.Radius * Math.Tan(radTheta / 2) * Math.Cos(radTheta);
            double d = attr.Radius * Math.Tan(radTheta / 2) * Math.Sin(radTheta);

            // Swift: let arcSeg = sin(angleBeta / 2) * radius * sqrt(2)
            double arcSeg = Math.Sin(radBeta / 2) * attr.Radius * Math.Sqrt(2);

            double b = (p - arcSeg - c - d) / 3;
            double a = 2 * b;

            return new SmoothCornerParameters { a = a, b = b, c = c, d = d, p = p, r = attr.Radius, theta = angleTheta };
        }

        private Point[] ComputeCurvePoints(SmoothCornerParameters prms, Rect rect, Corner corner)
        {
            double a = prms.a, b = prms.b, c = prms.c, d = prms.d, p = prms.p;
            double w = rect.Width;
            double h = rect.Height;
            double x = rect.X;
            double y = rect.Y;

            Point Pt(double _x, double _y) => new Point(x + _x, y + _y);

            switch (corner)
            {
                case Corner.TopRight:
                    return new Point[] {
                        Pt(w - (p - a - b - c), d),
                        Pt(w - (p - a), 0),
                        Pt(w - (p - a - b), 0),
                        Pt(w, p),
                        Pt(w, p - a - b),
                        Pt(w, p - a)
                    };
                case Corner.BottomRight:
                    return new Point[] {
                        Pt(w - d, h - (p - a - b - c)),
                        Pt(w, h - (p - a)),
                        Pt(w, h - (p - a - b)),
                        Pt(w - p, h),
                        Pt(w - (p - a - b), h),
                        Pt(w - (p - a), h)
                    };
                case Corner.BottomLeft:
                    return new Point[] {
                        Pt(p - a - b - c, h - d),
                        Pt(p - a, h),
                        Pt(p - a - b, h),
                        Pt(0, h - p),
                        Pt(0, h - (p - a - b)),
                        Pt(0, h - (p - a))
                    };
                case Corner.TopLeft:
                    return new Point[] {
                        Pt(d, p - a - b - c),
                        Pt(0, p - a),
                        Pt(0, p - a - b),
                        Pt(p, 0),
                        Pt(p - a - b, 0),
                        Pt(p - a, 0)
                    };
                default: return new Point[0];
            }
        }

        private Point CurveStart(Rect rect, Corner corner, double p)
        {
            double w = rect.Width;
            double h = rect.Height;
            double x = rect.X;
            double y = rect.Y;

            switch (corner)
            {
                case Corner.TopRight: return new Point(x + w - p, y + 0);
                case Corner.BottomRight: return new Point(x + w, y + h - p);
                case Corner.BottomLeft: return new Point(x + p, y + h);
                case Corner.TopLeft: return new Point(x + 0, y + p);
                default: return new Point();
            }
        }

        private double StartAngle(Corner corner)
        {
            switch (corner)
            {
                case Corner.TopRight: return 270;
                case Corner.BottomRight: return 0;
                case Corner.BottomLeft: return 90;
                case Corner.TopLeft: return 180;
                default: return 0;
            }
        }

        private Point CenterPoint(Rect rect, Corner corner, double radius)
        {
            double w = rect.Width;
            double h = rect.Height;
            double x = rect.X;
            double y = rect.Y;

            switch (corner)
            {
                case Corner.TopRight: return new Point(x + w - radius, y + radius);
                case Corner.BottomRight: return new Point(x + w - radius, y + h - radius);
                case Corner.BottomLeft: return new Point(x + radius, y + h - radius);
                case Corner.TopLeft: return new Point(x + radius, y + radius);
                default: return new Point();
            }
        }

        #endregion
    }
}