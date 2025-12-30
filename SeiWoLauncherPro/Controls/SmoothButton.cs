using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

// Gemini 3 Pro 编写
// 还没改完，该文件无法运行

namespace SeiWoLauncherPro.Controls
{
    public enum ButtonVariant
    {
        Colored,    // 默认：实色背景
        Outline,    // 描边：背景透明，边框有色
        Ghost,      // 幽灵：全透明，Hover时显色
        Destructive,// 警告：红色系
        Secondary,  // 次级：灰色系
        DashedOutline // 虚线：带虚线的Outline
    }

    public class SmoothButton : Button
    {
        private SmoothBorder _borderPart;
        private ScaleTransform _scaleTransform;

        static SmoothButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SmoothButton), new FrameworkPropertyMetadata(typeof(SmoothButton)));
        }

        public SmoothButton()
        {
            // 初始化默认属性
            this.Cursor = Cursors.Hand;
            this.HorizontalContentAlignment = HorizontalAlignment.Center;
            this.VerticalContentAlignment = VerticalAlignment.Center;

            // 构建纯代码 Template
            this.Template = CreateTemplate();

            // 监听状态变化以触发动画
            this.Loaded += (s, e) => UpdateVisualState(false);
            this.MouseEnter += (s, e) => UpdateVisualState(true);
            this.MouseLeave += (s, e) => UpdateVisualState(true);

            // 绑定 Pressed 状态 (Button 自带 IsPressed，但为了更丝滑的动画，我们通过 override On... 处理)
        }

        #region Dependency Properties - Style & Variant

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(nameof(Variant), typeof(ButtonVariant), typeof(SmoothButton),
                new PropertyMetadata(ButtonVariant.Colored, OnVisualPropChanged));

        public ButtonVariant Variant
        {
            get => (ButtonVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        // --- 尺寸 ("xs", "s", "m", "l", "xl", "xxl") ---
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(nameof(Size), typeof(string), typeof(SmoothButton),
                new PropertyMetadata("m", OnSizeChanged));

        public string Size
        {
            get => (string)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        // --- Colors ---

        // 主色调 (Colored, Outline Text, etc.)
        public static readonly DependencyProperty AccentColorProperty =
            DependencyProperty.Register(nameof(AccentColor), typeof(Color), typeof(SmoothButton),
                new PropertyMetadata(Color.FromRgb(0, 0, 0), OnVisualPropChanged)); // 默认黑色

        public Color AccentColor
        {
            get => (Color)GetValue(AccentColorProperty);
            set => SetValue(AccentColorProperty, value);
        }

        // Outline 特定
        public static readonly DependencyProperty OutlineBorderThicknessProperty =
            DependencyProperty.Register(nameof(OutlineBorderThickness), typeof(double), typeof(SmoothButton),
                new PropertyMetadata(1.0, OnVisualPropChanged));

        public double OutlineBorderThickness
        {
            get => (double)GetValue(OutlineBorderThicknessProperty);
            set => SetValue(OutlineBorderThicknessProperty, value);
        }

        public static readonly DependencyProperty IsOutlineBorderHoverUseAccentColorProperty =
            DependencyProperty.Register(nameof(IsOutlineBorderHoverUseAccentColor), typeof(bool), typeof(SmoothButton),
                new PropertyMetadata(true, OnVisualPropChanged));
        public bool IsOutlineBorderHoverUseAccentColor
        {
            get => (bool)GetValue(IsOutlineBorderHoverUseAccentColorProperty);
            set => SetValue(IsOutlineBorderHoverUseAccentColorProperty, value);
        }

        public static readonly DependencyProperty GhostHoverColorProperty =
            DependencyProperty.Register(nameof(GhostHoverColor), typeof(Color), typeof(SmoothButton),
                new PropertyMetadata(Color.FromRgb(244, 244, 245), OnVisualPropChanged)); // 默认浅灰 (Zinc-100)

        /// <summary>
        /// Ghost 模式下，鼠标悬停时的背景颜色
        /// </summary>
        public Color GhostHoverColor
        {
            get => (Color)GetValue(GhostHoverColorProperty);
            set => SetValue(GhostHoverColorProperty, value);
        }

        // --- Destructive Variant Properties ---

        public static readonly DependencyProperty DestructiveAccentColorProperty =
            DependencyProperty.Register(nameof(DestructiveAccentColor), typeof(Color), typeof(SmoothButton),
                new PropertyMetadata(Color.FromRgb(239, 68, 68), OnVisualPropChanged)); // 默认红色 (Red-500)

        /// <summary>
        /// Destructive 模式的主色调（背景色）
        /// </summary>
        public Color DestructiveAccentColor
        {
            get => (Color)GetValue(DestructiveAccentColorProperty);
            set => SetValue(DestructiveAccentColorProperty, value);
        }

        // --- Secondary Variant Properties ---

        public static readonly DependencyProperty SecondaryAccentColorProperty =
            DependencyProperty.Register(nameof(SecondaryAccentColor), typeof(Color), typeof(SmoothButton),
                new PropertyMetadata(Color.FromRgb(244, 244, 245), OnVisualPropChanged)); // 默认灰白 (Zinc-100)

        /// <summary>
        /// Secondary 模式的主色调
        /// </summary>
        public Color SecondaryAccentColor
        {
            get => (Color)GetValue(SecondaryAccentColorProperty);
            set => SetValue(SecondaryAccentColorProperty, value);
        }

        // (补充) 既然提到了 SecondaryPressedColor，建议也加上，以防万一
        public static readonly DependencyProperty SecondaryPressedColorProperty =
            DependencyProperty.Register(nameof(SecondaryPressedColor), typeof(Color), typeof(SmoothButton),
                new PropertyMetadata(Color.FromRgb(228, 228, 231), OnVisualPropChanged)); // (Zinc-200)

        public Color SecondaryPressedColor
        {
             get => (Color)GetValue(SecondaryPressedColorProperty);
             set => SetValue(SecondaryPressedColorProperty, value);
        }

        // Dashed 特定
        public static readonly DependencyProperty DashArrayProperty =
            DependencyProperty.Register(nameof(DashArray), typeof(DoubleCollection), typeof(SmoothButton),
                new PropertyMetadata(new DoubleCollection { 4, 4 }, OnVisualPropChanged));

        public DoubleCollection DashArray
        {
            get => (DoubleCollection)GetValue(DashArrayProperty);
            set => SetValue(DashArrayProperty, value);
        }

        // --- SmoothBorder 透传属性 ---
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(SmoothButton),
                new PropertyMetadata(new CornerRadius(6))); // 默认圆角

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public static readonly DependencyProperty SmoothnessProperty =
            DependencyProperty.Register(nameof(Smoothness), typeof(double), typeof(SmoothButton),
                new PropertyMetadata(0.6)); // Apple Style

        public double Smoothness
        {
            get => (double)GetValue(SmoothnessProperty);
            set => SetValue(SmoothnessProperty, value);
        }

        // --- 动画配置 ---
        public static readonly DependencyProperty PressScaleProperty =
            DependencyProperty.Register(nameof(PressScale), typeof(double), typeof(SmoothButton), new PropertyMetadata(0.95));

        public static readonly DependencyProperty AnimDurationProperty =
            DependencyProperty.Register(nameof(AnimDuration), typeof(TimeSpan), typeof(SmoothButton),
                new PropertyMetadata(TimeSpan.FromMilliseconds(150)));

        #endregion

        #region Logic & Template Construction

        private static void OnVisualPropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SmoothButton)d).UpdateVisualState(false);
        }

        private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SmoothButton)d).ApplySize(e.NewValue as string);
        }

        private ControlTemplate CreateTemplate()
        {
            // 纯代码构建 Template：
            // <ControlTemplate TargetType="SmoothButton">
            //    <SmoothBorder x:Name="PART_Border" ...>
            //       <ContentPresenter ... />
            //    </SmoothBorder>
            // </ControlTemplate>

            var template = new ControlTemplate(typeof(SmoothButton));
            var borderFactory = new FrameworkElementFactory(typeof(SmoothBorder));
            borderFactory.Name = "PART_Border";

            // 绑定 SmoothBorder 的核心属性到 Button
            borderFactory.SetBinding(SmoothBorder.CornerRadiusProperty, new Binding(nameof(CornerRadius)) { Source = this });
            borderFactory.SetBinding(SmoothBorder.SmoothnessProperty, new Binding(nameof(Smoothness)) { Source = this });
            borderFactory.SetBinding(SmoothBorder.PaddingProperty, new Binding(nameof(Padding)) { Source = this });

            // 绑定虚线属性 (前提是你完成了第1步的 SmoothBorder 修改)
            borderFactory.SetBinding(SmoothBorder.StrokeDashArrayProperty, new Binding(nameof(DashArray)) { Source = this });

            // 设置 ContentPresenter
            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
            contentPresenter.SetValue(RecognizesAccessKeyProperty, true);

            borderFactory.AppendChild(contentPresenter);
            template.VisualTree = borderFactory;

            return template;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _borderPart = GetTemplateChild("PART_Border") as SmoothBorder;

            if (_borderPart != null)
            {
                // 初始化 ScaleTransform 用于点击缩小动画
                _scaleTransform = new ScaleTransform(1.0, 1.0);
                _borderPart.RenderTransformOrigin = new Point(0.5, 0.5);
                _borderPart.RenderTransform = _scaleTransform;

                // 初始化画笔，防止动画时从 null 开始报错
                _borderPart.Background = new SolidColorBrush(Colors.Transparent);
                _borderPart.BorderBrush = new SolidColorBrush(Colors.Transparent);
                this.Foreground = new SolidColorBrush(Colors.Black);
            }

            // 初始化尺寸
            ApplySize(this.Size);
            // 初始化颜色状态
            UpdateVisualState(false);
        }

        private void ApplySize(string sizeStr)
        {
            if (string.IsNullOrEmpty(sizeStr)) return;

            double height = 40;
            double fontSize = 14;
            double px = 16; // horizontal padding

            switch (sizeStr.ToLower())
            {
                case "xs": height = 24; fontSize = 12; px = 8; break;
                case "s":  height = 32; fontSize = 13; px = 12; break;
                case "m":  height = 40; fontSize = 14; px = 16; break;
                case "l":  height = 48; fontSize = 16; px = 24; break;
                case "xl": height = 56; fontSize = 18; px = 32; break;
                case "xxl": height = 64; fontSize = 20; px = 40; break;
            }

            this.Height = height;
            this.FontSize = fontSize;
            this.Padding = new Thickness(px, 0, px, 0); // Vertical 由 Center 自动处理
        }

        #endregion

        #region Interaction & Animation Logic

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            AnimateScale((double)GetValue(PressScaleProperty));
            UpdateVisualState(true);
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);
            AnimateScale(1.0);
            UpdateVisualState(true);
        }

        // 捕获鼠标离开时也要恢复缩放
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            AnimateScale(1.0);
        }

        private void AnimateScale(double targetScale)
        {
            if (_scaleTransform == null) return;

            var anim = new DoubleAnimation
            {
                To = targetScale,
                Duration = (TimeSpan)GetValue(AnimDurationProperty),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            _scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
        }

        /// <summary>
        /// 核心状态机：根据 Variant 和当前交互状态计算目标颜色并动画过渡
        /// </summary>
        private void UpdateVisualState(bool useTransition)
        {
            if (_borderPart == null) return;

            bool isHover = IsMouseOver;
            bool isPressed = IsPressed;

            Color targetBg = Colors.Transparent;
            Color targetBorder = Colors.Transparent;
            Color targetFg = Colors.Black;
            double targetThickness = 0;

            // --- 状态颜色逻辑 ---

            switch (Variant)
            {
                case ButtonVariant.Colored:
                    // 默认实色：背景Accent，字白色
                    targetBg = isPressed ? Darken(AccentColor, 0.2) : (isHover ? Darken(AccentColor, 0.1) : AccentColor);
                    targetFg = Colors.White;
                    targetThickness = 0;
                    break;

                case ButtonVariant.Destructive:
                    Color destColor = (Color)GetValue(DestructiveAccentColorProperty);
                    targetBg = isPressed ? Darken(destColor, 0.2) : (isHover ? Darken(destColor, 0.1) : destColor);
                    targetFg = Colors.White;
                    break;

                case ButtonVariant.Secondary:
                    Color secColor = (Color)GetValue(SecondaryAccentColorProperty);
                    targetBg = isPressed ? Darken(secColor, 0.1) : (isHover ? Darken(secColor, 0.05) : secColor);
                    targetFg = Colors.Black; // 这里的 Foreground 通常是黑或深灰
                    break;

                case ButtonVariant.Outline:
                case ButtonVariant.DashedOutline:
                    targetThickness = OutlineBorderThickness;
                    // 背景：Outline 模式通常 Hover 时会有淡淡的背景，或者完全透明
                    targetBg = isHover ? Color.FromArgb(20, AccentColor.R, AccentColor.G, AccentColor.B) : Colors.Transparent;

                    // 边框：
                    bool hoverUseAccent = IsOutlineBorderHoverUseAccentColor;
                    targetBorder = isHover && hoverUseAccent ? AccentColor : Color.FromArgb(100, 128, 128, 128);
                    if (!isHover) targetBorder = Color.FromArgb(80, 0, 0, 0); // 默认边框浅灰

                    // 文字：Outline 模式文字通常是 Accent Color
                    targetFg = AccentColor;
                    break;

                case ButtonVariant.Ghost:
                    Color ghostHover = (Color)GetValue(GhostHoverColorProperty);
                    targetBg = isPressed ? Darken(ghostHover, 0.1) : (isHover ? ghostHover : Colors.Transparent);
                    targetFg = Colors.Black; // 这里的文字颜色可以增加一个 GhostFg 属性，暂时默认黑
                    break;
            }

            // 虚线特殊处理
            if (Variant == ButtonVariant.DashedOutline)
            {
                // 确保 StrokeDashArray 生效
                _borderPart.StrokeDashArray = DashArray;
            }
            else
            {
                _borderPart.StrokeDashArray = null;
            }

            // --- 执行颜色动画 ---

            TimeSpan duration = useTransition ? (TimeSpan)GetValue(AnimDurationProperty) : TimeSpan.Zero;

            AnimateBrush(_borderPart, SmoothBorder.BackgroundProperty, targetBg, duration);
            AnimateBrush(_borderPart, SmoothBorder.BorderBrushProperty, targetBorder, duration);
            AnimateBrush(this, Control.ForegroundProperty, targetFg, duration);

            // Thickness 动画 (可选，这里直接设置)
            _borderPart.BorderThickness = targetThickness;
        }

        private void AnimateBrush(FrameworkElement target, DependencyProperty property, Color toColor, TimeSpan duration)
        {
            var currentBrush = target.GetValue(property) as SolidColorBrush;

            // 如果当前不是 SolidColorBrush，或者它是冻结的(Frozen)，我们需要替换它
            if (currentBrush == null || currentBrush.IsFrozen)
            {
                currentBrush = new SolidColorBrush(toColor);
                target.SetValue(property, currentBrush);
                return;
            }

            // 如果颜色没变，就不动
            if (currentBrush.Color == toColor) return;

            if (duration == TimeSpan.Zero)
            {
                currentBrush.Color = toColor;
            }
            else
            {
                var anim = new ColorAnimation(toColor, duration);
                currentBrush.BeginAnimation(SolidColorBrush.ColorProperty, anim);
            }
        }

        // 简单的颜色加深辅助函数
        private Color Darken(Color color, double factor)
        {
            float red = (float)color.R;
            float green = (float)color.G;
            float blue = (float)color.B;

            if (factor < 0) factor = 0;
            if (factor > 1) factor = 1;

            return Color.FromRgb(
                (byte)(red * (1 - factor)),
                (byte)(green * (1 - factor)),
                (byte)(blue * (1 - factor)));
        }

        #endregion
    }
}