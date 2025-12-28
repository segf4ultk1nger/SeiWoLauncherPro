using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SeiWoLauncherPro.Controls
{
    public abstract class SymbolicIconBase : Image
    {
        // 默认图标大小，设个常用的 16 或 24，避免出来是 0x0 看不见
        private const double DefaultIconSize = 16.0;

        public SymbolicIconBase()
        {
            // 渲染属性配置
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            Stretch = Stretch.Uniform;

            // 初始化时，如果用户没设 IconSize，先应用默认值，防止图标不显示
            // 注意：这里手动赋值是因为 DefaultValue 不会触发 OnPropertyChanged 回调
            Width = DefaultIconSize;
            Height = DefaultIconSize;

            // 监听 Loaded 是个好习惯，确保视觉树就绪后再生成重型资源
            Loaded += (s, e) => UpdateIcon();
        }

        #region Dependency Properties

        // === 1. IconColor ===
        public static readonly DependencyProperty IconColorProperty =
            DependencyProperty.Register(nameof(IconColor), typeof(Brush), typeof(SymbolicIconBase),
                new PropertyMetadata(Brushes.Black, OnIconPropertiesChanged));

        public Brush IconColor
        {
            get => (Brush)GetValue(IconColorProperty);
            set => SetValue(IconColorProperty, value);
        }

        // === 2. IconSize (New) ===
        public static readonly DependencyProperty IconSizeProperty =
            DependencyProperty.Register(nameof(IconSize), typeof(double), typeof(SymbolicIconBase),
                new PropertyMetadata(DefaultIconSize, OnIconSizeChanged));

        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        #endregion

        #region Property Change Handlers

        private static void OnIconPropertiesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SymbolicIconBase icon)
            {
                icon.UpdateIcon();
            }
        }

        // 当 IconSize 变化时，同步修改 Image 的 Width 和 Height
        private static void OnIconSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SymbolicIconBase icon && e.NewValue is double newSize)
            {
                // 这里的逻辑很简单：IconSize 掌权，强制覆盖 Width 和 Height
                // 这样你在 XAML 里只要写 IconSize="24" 就行了
                icon.Width = newSize;
                icon.Height = newSize;
            }
        }

        #endregion

        protected void UpdateIcon()
        {
            var brush = IconColor ?? Brushes.Transparent;

            // 直接设置 Image 基类的 Source
            // 因为 DrawingImage 是矢量，这里无论怎么 Resize 都不会模糊
            this.Source = CreateDrawingImage(brush);
        }

        protected abstract DrawingImage CreateDrawingImage(Brush brush);
    }
}