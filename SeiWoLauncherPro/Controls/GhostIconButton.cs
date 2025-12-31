using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation; // 引入动画库

namespace SeiWoLauncherPro.Controls
{
    /// <summary>
    /// 一个模仿 shadcn/ui 的 Ghost 风格图标按钮 (Pro版)
    /// <para>特点：支持暗黑模式切换，物理回弹按压动画，纯代码构建</para>
    /// </summary>
    public class GhostIconButton : Button
    {
        // === 预热静态画笔资源 (Freeze for Performance) ===
        // Light Mode: 基于黑色的半透明
        private static readonly Brush LightHoverBrush = new SolidColorBrush(Color.FromArgb(25, 0, 0, 0));
        private static readonly Brush LightPressedBrush = new SolidColorBrush(Color.FromArgb(45, 0, 0, 0));

        // Dark Mode: 基于白色的半透明 (在深色磨砂背景上更清晰)
        private static readonly Brush DarkHoverBrush = new SolidColorBrush(Color.FromArgb(35, 255, 255, 255));
        private static readonly Brush DarkPressedBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));

        static GhostIconButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GhostIconButton), new FrameworkPropertyMetadata(typeof(GhostIconButton)));

            // 冻结静态资源
            if (LightHoverBrush.CanFreeze) LightHoverBrush.Freeze();
            if (LightPressedBrush.CanFreeze) LightPressedBrush.Freeze();
            if (DarkHoverBrush.CanFreeze) DarkHoverBrush.Freeze();
            if (DarkPressedBrush.CanFreeze) DarkPressedBrush.Freeze();
        }

        public GhostIconButton()
        {
            // 1. 基础属性
            Background = Brushes.Transparent;
            BorderThickness = new Thickness(0);
            HorizontalContentAlignment = HorizontalAlignment.Center;
            VerticalContentAlignment = VerticalAlignment.Center;

            Width = 32;
            Height = 32;

            // 2. 动画准备：设置中心点，否则缩放会偏向左上角
            RenderTransformOrigin = new Point(0.5, 0.5);
            RenderTransform = new ScaleTransform(1.0, 1.0);

            // 3. 构建模板
            this.Template = CreateGhostTemplate();

            // 4. 初始化一次颜色状态
            UpdateVisualState(false);
        }

        #region Dependency Properties

        // === 1. Icon Integration ===
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(SymbolicIconBase), typeof(GhostIconButton),
                new PropertyMetadata(null, OnIconChanged));

        public SymbolicIconBase Icon
        {
            get => (SymbolicIconBase)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        // === 2. Styling Props ===
        public static readonly DependencyProperty IconSizeProperty =
            DependencyProperty.Register(nameof(IconSize), typeof(double), typeof(GhostIconButton),
                new PropertyMetadata(16.0));

        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(GhostIconButton),
                new PropertyMetadata(new CornerRadius(4)));

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        // === 3. Theme Mode (New) ===
        public static readonly DependencyProperty IsDarkModeProperty =
            DependencyProperty.Register(nameof(IsDarkMode), typeof(bool), typeof(GhostIconButton),
                new PropertyMetadata(false, (d, e) => ((GhostIconButton)d).UpdateVisualState(false)));

        /// <summary>
        /// 是否为暗色模式（决定 Ghost 效果是白色高亮还是黑色高亮）
        /// </summary>
        public bool IsDarkMode
        {
            get => (bool)GetValue(IsDarkModeProperty);
            set => SetValue(IsDarkModeProperty, value);
        }

        // === 4. Animation Props (New) ===
        public static readonly DependencyProperty PressedScaleProperty =
            DependencyProperty.Register(nameof(PressedScale), typeof(double), typeof(GhostIconButton),
                new PropertyMetadata(0.9)); // 默认按下缩小到 90%

        /// <summary>
        /// 按下时的缩放比例 (0.0 - 1.0)
        /// </summary>
        public double PressedScale
        {
            get => (double)GetValue(PressedScaleProperty);
            set => SetValue(PressedScaleProperty, value);
        }

        public static readonly DependencyProperty AnimationDurationProperty =
            DependencyProperty.Register(nameof(AnimationDuration), typeof(TimeSpan), typeof(GhostIconButton),
                new PropertyMetadata(TimeSpan.FromMilliseconds(150)));

        /// <summary>
        /// 动画持续时间
        /// </summary>
        public TimeSpan AnimationDuration
        {
            get => (TimeSpan)GetValue(AnimationDurationProperty);
            set => SetValue(AnimationDurationProperty, value);
        }

        #endregion

        #region Logic & Visual States

        private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var btn = (GhostIconButton)d;
            if (e.NewValue is SymbolicIconBase newIcon)
            {
                // 绑定 Foreground -> IconColor
                newIcon.SetBinding(SymbolicIconBase.IconColorProperty, new Binding(nameof(Foreground)) { Source = btn, Mode = BindingMode.OneWay });
                // 绑定 IconSize
                newIcon.SetBinding(SymbolicIconBase.IconSizeProperty, new Binding(nameof(IconSize)) { Source = btn, Mode = BindingMode.OneWay });

                btn.Content = newIcon;
            }
            else
            {
                btn.Content = null;
            }
        }

        // 统一的状态更新逻辑
        private void UpdateVisualState(bool isPressed)
        {
            // 1. 处理背景色 (Ghost Effect)
            if (isPressed)
            {
                Background = IsDarkMode ? DarkPressedBrush : LightPressedBrush;
            }
            else if (IsMouseOver)
            {
                Background = IsDarkMode ? DarkHoverBrush : LightHoverBrush;
            }
            else
            {
                Background = Brushes.Transparent;
            }

            // 2. 如果用户没有显式设置 Foreground，我们可以根据模式自动微调文字/图标颜色
            // (这里选择保留用户设置的 Foreground，或者你可以加入默认逻辑)
        }

        // 动画核心：按下 (Scale Down)
        private void AnimatePress()
        {
            var anim = new DoubleAnimation(PressedScale, AnimationDuration)
            {
                // QuadraticEase: 平滑加速收缩
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            ApplyAnim(anim);
        }

        // 动画核心：松开 (Scale Up with Bounce)
        private void AnimateRelease()
        {
            var anim = new DoubleAnimation(1.0, AnimationDuration)
            {
                // BackEase: 回弹效果 (Amplitude 越大弹得越狠，0.3 比较克制)
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.4 }
            };
            ApplyAnim(anim);
        }

        private void ApplyAnim(DoubleAnimation anim)
        {
            // 必须应用到 ScaleTransform 上
            RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
            RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            UpdateVisualState(false);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            UpdateVisualState(false);
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            UpdateVisualState(true); // 切换深色背景
            AnimatePress();          // 触发收缩动画
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);
            UpdateVisualState(false); // 恢复悬停背景
            AnimateRelease();         // 触发回弹动画
        }

        #endregion

        #region Template Factory

        private ControlTemplate CreateGhostTemplate()
        {
            var template = new ControlTemplate(typeof(GhostIconButton));

            var borderFactory = new FrameworkElementFactory(typeof(Border));
            // 这里的绑定至关重要，让 Code-behind 的属性变化能映射到 Visual Tree
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(BackgroundProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new TemplateBindingExtension(CornerRadiusProperty));
            borderFactory.SetValue(Border.SnapsToDevicePixelsProperty, true);

            var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetValue(HorizontalAlignmentProperty, new TemplateBindingExtension(HorizontalContentAlignmentProperty));
            contentFactory.SetValue(VerticalAlignmentProperty, new TemplateBindingExtension(VerticalContentAlignmentProperty));

            borderFactory.AppendChild(contentFactory);
            template.VisualTree = borderFactory;

            return template;
        }

        #endregion
    }
}