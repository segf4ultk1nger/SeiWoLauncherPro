using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SeiWoLauncherPro.Controls;
using SeiWoLauncherPro.Controls.SymbolicIcons;
using SeiwoLauncherPro.Utils;
using SeiWoLauncherPro.Utils;

namespace SeiWoLauncherPro {
    public class MainWindow : WindowBase {
        // Constants
        private const double BlurDownsampleScale = 4.0;
        private const double WindowWidth = 400;
        private const double WindowHeight = 600;
        private const double BarHeight = 64;

        // Fields
        private readonly MainWindowViewModel _viewModel;
        private SmoothBorder _mainBorder;
        private SimplePanel _bottomPanel; // 提升为字段，方便后续访问

        private static readonly Brush _cachedNoiseBrush = NoiseTextureGenerator.CreateNoiseBrush(0.04);

        public MainWindow(MainWindowViewModel viewModel) {
            _viewModel = viewModel;
            this.DataContext = _viewModel;

            SizeToContent = SizeToContent.WidthAndHeight;

            // 初始化模糊快照
            DesktopBlurProvider.Instance.RefreshSnapshot();
        }

        private FrameworkElement CreateDemoPage(string title, Color bg)
        {
            // 不要计算 centerHeight 了，依靠布局系统
            var page = new Border
            {
                Width = WindowWidth,

                Background = new SolidColorBrush(bg),
                BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Child = new TextBlock
                {
                    Text = title,
                    Foreground = Brushes.White,
                    FontSize = 32,
                    FontWeight = FontWeights.Thin,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Opacity = 0.8,
                    Effect = new System.Windows.Media.Effects.DropShadowEffect // 加点阴影更有质感
                    {
                        BlurRadius = 10,
                        ShadowDepth = 2,
                        Opacity = 0.5
                    }
                }
            };

            return page;
        }

        protected override FrameworkElement Build() {
            // 1. 初始化主容器
            _mainBorder = CreateMainContainer();

            // 2. 构建内部布局
            var dockPanel = new DockPanel();

            // Bottom Dock (包含 Tab 按钮)
            var bottomBar = CreateDockBar(Dock.Bottom);
            _bottomPanel = new SimplePanel();
            bottomBar.Child = _bottomPanel;
            InitializeBottomTabs(_bottomPanel);
            _bottomPanel.Children.Add(InitializeBottomDockIconGroup());

            // Top Dock
            var topBar = CreateDockBar(Dock.Top);
            topBar.Padding = new Thickness(20);

            var scrollView = new KineticScrollView
            {
                Orientation = Orientation.Horizontal, // 横向滚动
                ScrollMode = ScrollMode.Paging,       // 开启分页模式 (Snap)
                ClipToBounds = true,                  // 确保不溢出
                // Background = Brushes.Transparent   // 已经在 Template 里设了，这里不用管
            };

            // 里面的内容 (Mover)
            var contentStack = new StackPanel { Orientation = Orientation.Horizontal };

            // 塞几个演示页面
            contentStack.Children.Add(CreateDemoPage("PAGE 1", Color.FromArgb(64,255,0,0)));
            contentStack.Children.Add(CreateDemoPage("PAGE 2", Color.FromArgb(64,0,255,0)));
            contentStack.Children.Add(CreateDemoPage("PAGE 3", Colors.Transparent));

            // 填装
            scrollView.Content = contentStack;

            var centerContent = new SimplePanel() {
                Width = 400,
                Height = 200
            };

            var pageDots = new SmoothPageIndicator() {
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            pageDots.AttachTo(scrollView);

            centerContent.Children.Add(scrollView);
            centerContent.Children.Add(pageDots);

            // 放入中间
            dockPanel.Children.Add(bottomBar);
            dockPanel.Children.Add(topBar);
            dockPanel.Children.Add(centerContent);
            _mainBorder.Child = dockPanel;

            // 4. 绑定事件
            BindLifecycleEvents();

            return _mainBorder;
        }

        #region UI Construction Helpers

        private SmoothBorder CreateMainContainer() {
            return new SmoothBorder {
                CornerRadius = new CornerRadius(16),
                Smoothness = 0.6,
                BorderThickness = 0,
                CornerClip = true,
                Width = WindowWidth,
                Height = WindowHeight,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                ShadowColor = Colors.Black,
                ShadowOpacity = 0.4,
                ShadowBlurRadius = 15,
                ShadowDepth = 0,
                ShadowNoCaster = true,
                BackgroundLayers = new FreezableCollection<Brush> {
                    _cachedNoiseBrush,
                    new SolidColorBrush(Color.FromArgb(128, 0, 0, 0))
                }
            };
        }

        private SmoothBorder CreateDockBar(Dock dockPos) {
            var border = new SmoothBorder {
                Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
                CornerClip = true,
                Width = WindowWidth,
                Height = BarHeight
            };
            DockPanel.SetDock(border, dockPos);
            return border;
        }

        private void InitializeBottomTabs(Panel container) {
            var tabGroup = new SegmentedTabButtonGroup {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 168,
                Margin = new Thickness(12, 0, 0, 0)
            };

            tabGroup.AddButton(new IconTextSegmentedTabButton(
                new SymbolicDesktopIcon { IconColor = Brushes.White, IconSize = 19 }, "桌面"),
                isSelected: true);

            tabGroup.AddButton(new IconTextSegmentedTabButton(
                new SymbolicFolderIcon { IconColor = Brushes.White, IconSize = 19 }, "文件"),
                isSelected: false);

            container.Children.Add(tabGroup);
        }

        private FrameworkElement InitializeBottomDockIconGroup()
        {
            // 创建一个容器来横向排列这些图标
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right, // 整个组靠右
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 16, 0) // 给右边留点呼吸空间
            };

            // --- 局部工厂函数 (Magic Happens Here) ---
            // 专门生产符合你要求的 GhostButton，避免重复代码
            GhostIconButton CreateDockToolBtn(SymbolicIconBase icon, Action<object, RoutedEventArgs> onClick, int? iconSize=null)
            {
                var btn = new GhostIconButton
                {
                    // 核心属性
                    Icon = icon,
                    IsDarkMode = true,       // 强制深色模式 (White Icon)
                    Foreground = Brushes.White,

                    // 尺寸 & 样式
                    IconSize = iconSize??22,           // 24对于36的框来说有点太挤了，22视觉平衡更好，你也可以改回24
                    Width = 36,
                    Height = 36,
                    CornerRadius = new CornerRadius(6),

                    // 布局
                    Margin = new Thickness(4, 0, 0, 0), // 按钮之间的间距
                };

                // 绑定点击事件
                if (onClick != null) btn.Click += new RoutedEventHandler(onClick);

                return btn;
            }

            // --- 批量生产 ---

            // 1. 壁纸/图片
            panel.Children.Add(CreateDockToolBtn(new SymbolicPictureIcon(), (s, e) => {
                // TODO: 打开壁纸切换器
                System.Diagnostics.Debug.WriteLine("Wallpaper Clicked");
            }, iconSize:26));

            // 2. 锁定
            panel.Children.Add(CreateDockToolBtn(new SymbolicLockIcon(), (s, e) => {

            }));

            // 3. 设置
            panel.Children.Add(CreateDockToolBtn(new SymbolicSettingsCogIcon(), (s, e) => {
                // TODO: 打开设置面板
                System.Diagnostics.Debug.WriteLine("Settings Clicked");
            }, iconSize:25));

            return panel;
        }

        private void BindLifecycleEvents() {
            // 尺寸、位置变化及加载完成时更新模糊
            _mainBorder.SizeChanged += (s, e) => UpdateBlurEffect();
            _mainBorder.Loaded += (s, e) => UpdateBlurEffect();
            this.LocationChanged += (s, e) => {
                UpdateBlurEffect();
                // PrintDebugCoordinates(); // Debug usage only
            };
        }

        #endregion

        #region Logic & Effects

        private void UpdateBlurEffect() {
            if (_mainBorder == null || DesktopBlurProvider.Instance.BlurredBrush == null) return;

            var dpi = VisualTreeHelper.GetDpi(this);
            Point borderToWindow = CoordinateHelper.GetRelativeToWindow(_mainBorder);
            Rect windowToWallpaper = CoordinateHelper.GetRelativeRectToWallpaper(this);

            // 计算物理像素坐标
            double rawPxX = windowToWallpaper.X + (borderToWindow.X * dpi.DpiScaleX);
            double rawPxY = windowToWallpaper.Y + (borderToWindow.Y * dpi.DpiScaleY);
            double rawPxWidth = _mainBorder.ActualWidth * dpi.DpiScaleX;
            double rawPxHeight = _mainBorder.ActualHeight * dpi.DpiScaleY;

            if (rawPxWidth <= 0 || rawPxHeight <= 0) return;

            // 计算下采样后的 Viewbox
            double finalX = rawPxX / BlurDownsampleScale;
            double finalY = rawPxY / BlurDownsampleScale;
            double finalW = rawPxWidth / BlurDownsampleScale;
            double finalH = rawPxHeight / BlurDownsampleScale;

            // 应用画笔
            var brush = DesktopBlurProvider.Instance.BlurredBrush.Clone();
            brush.ViewboxUnits = BrushMappingMode.Absolute;
            brush.Viewbox = new Rect(finalX, finalY, finalW, finalH);
            brush.ViewportUnits = BrushMappingMode.RelativeToBoundingBox;
            brush.Viewport = new Rect(0, 0, 1, 1);
            brush.Stretch = Stretch.Fill;

            _mainBorder.Background = brush;
        }

        #endregion

        #region Debugging Tools

        [Conditional("DEBUG")]
        private void PrintDebugCoordinates() {
            if (_mainBorder == null || !this.IsLoaded) return;
            try {
                // A. mainBorder 相对窗口位置 (WPF 逻辑单位)
                Point borderToWindow = CoordinateHelper.GetRelativeToWindow(_mainBorder);

                // B. 窗口相对屏幕位置 (物理像素)
                Rect windowToScreen = CoordinateHelper.GetWindowScreenRect(this);

                // C. 窗口相对桌面壁纸窗口位置 (物理像素)
                Rect windowToWallpaper = CoordinateHelper.GetRelativeRectToWallpaper(this);

                // D. mainBorder 相对桌面壁纸窗口位置 (物理像素计算)
                // 算法：窗口相对壁纸的起点 + (mainBorder相对窗口的起点 * DPI缩放)
                var dpi = VisualTreeHelper.GetDpi(this);
                double borderToWallpaperX = windowToWallpaper.X + (borderToWindow.X * dpi.DpiScaleX);
                double borderToWallpaperY = windowToWallpaper.Y + (borderToWindow.Y * dpi.DpiScaleY);

                System.Diagnostics.Debug.WriteLine("------------------------------------------");
                System.Diagnostics.Debug.WriteLine($"[1] Border to Window (WPF): {borderToWindow.X:F2}, {borderToWindow.Y:F2}");
                System.Diagnostics.Debug.WriteLine($"[2] Window to Screen (PX):  X:{windowToScreen.X:F0}, Y:{windowToScreen.Y:F0}");
                System.Diagnostics.Debug.WriteLine($"[3] Window to Wallpaper(PX):X:{windowToWallpaper.X:F0}, Y:{windowToWallpaper.Y:F0}");
                System.Diagnostics.Debug.WriteLine($"[4] Border to Wallpaper(PX):X:{borderToWallpaperX:F0}, Y:{borderToWallpaperY:F0}");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Debug Output Error: {ex.Message}");
            }
        }

        #endregion
    }
}