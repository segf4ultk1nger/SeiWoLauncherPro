using SeiWoLauncherPro.Controls;
using SeiWoLauncherPro.Controls.SymbolicIcons;
using SeiWoLauncherPro.Utils;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SeiWoLauncherPro {
    public class MainWindow : WindowBase {
        private readonly MainWindowViewModel _viewModel;

        // 依赖注入发生在这里
        public MainWindow(MainWindowViewModel viewModel) {
            _viewModel = viewModel;

            SizeToContent = SizeToContent.WidthAndHeight;

            // 将 DataContext 绑定到注入的对象上
            this.DataContext = _viewModel;
        }

        protected override FrameworkElement Build() {

            var _blurProvider = new DesktopBlurProvider(this);

            var mainBorder = new SmoothBorder() {
                CornerRadius = new CornerRadius(16), // 大圆角更显苹果味
                Smoothness = 0.6,
                BorderThickness = 0,
                // Background = new SolidColorBrush(Color.FromArgb(148, 0, 0, 0)),
                Background = _blurProvider.BlurredBackgroundBrush,
                CornerClip = true,
                Width = 400,
                Height = 600,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                ShadowColor = Colors.Black,
                ShadowOpacity = 0.4,
                ShadowBlurRadius = 15,
                ShadowDepth = 0,
                ShadowNoCaster = true,
                MultiBackgrounds = new FreezableCollection<Brush>(new [] {
                    new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
                })
            };

            SimplePanel bottomPanel = new SimplePanel();
            mainBorder.Child = new DockPanel()
                .Children(new[] {
                    // Bottom Dock
                    new SmoothBorder() {
                        Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
                        CornerClip = true,
                        Width = 400,
                        Height = 64,
                    }.With(s => {
                        DockPanel.SetDock(s, Dock.Bottom);
                        s.Child = new SimplePanel().Assign(out bottomPanel);
                    }) as FrameworkElement,
                    // Top Dock
                    new SmoothBorder() {
                        Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
                        Padding = new Thickness(20), // 子元素与边框的距离
                        CornerClip = true,
                        Width = 400,
                        Height = 64,
                    }.With(s => {
                        DockPanel.SetDock(s, Dock.Top);
                    }) as FrameworkElement,
                    new StackPanel()
                });

            bottomPanel.Children(new[] {
                new SegmentedTabButtonGroup() {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = 168,
                    Margin = new Thickness(12, 0, 0, 0)
                }.With(s => {
                    s.AddButton(new IconTextSegmentedTabButton(new SymbolicDesktopIcon() {
                        IconColor = Brushes.White, IconSize = 19
                    }, "桌面"), isSelected: true);
                    s.AddButton(new IconTextSegmentedTabButton(new SymbolicFolderIcon() {
                        IconColor = Brushes.White, IconSize = 19
                    }, "文件"), isSelected: false);
                }) as FrameworkElement,
            });
            return mainBorder;
        }
    }
}