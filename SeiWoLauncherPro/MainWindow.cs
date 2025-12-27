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
            var mainBorder = new SmoothBorder() {
                CornerRadius = new CornerRadius(12), // 大圆角更显苹果味
                Smoothness = 0.72,
                BorderThickness = 0,
                Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
                CornerClip = true,
                Width = 400,
                Height = 600,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            mainBorder.Child = new StackPanel()
                .Children(new [] {
                    new SmoothBorder() {
                        Background = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0)),
                        Padding = new Thickness(20), // 子元素与边框的距离
                        CornerClip = true,
                        Width = 400,
                        Height = 64,

                    } as FrameworkElement,
                });
            return mainBorder;
        }
    }
}