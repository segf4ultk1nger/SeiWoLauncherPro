using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SeiWoLauncherPro.Controls {
    public class RichSegmentedTabButton : SegmentedTabButtonBase
    {
        private Path _iconPath;
        private TextBlock _textBlock;
        private Border _badgeBorder;
        private TextBlock _badgeText;
        private Ellipse _redDot;

        public RichSegmentedTabButton(Geometry iconGeometry, string text)
        {
            // 1. 主内容 (图标+文字)
            _iconPath = new Path
            {
                Data = iconGeometry,
                Width = 16, Height = 16, Stretch = Stretch.Uniform,
                Fill = BrushNormalFg,
                Margin = new Thickness(0, 0, 6, 0)
            };

            _textBlock = new TextBlock
            {
                Text = text, FontSize = 13, FontWeight = FontWeights.Medium,
                Foreground = BrushNormalFg,
                VerticalAlignment = VerticalAlignment.Center
            };

            var contentPanel = new StackPanel { Orientation = Orientation.Horizontal };
            contentPanel.Children.Add(_iconPath);
            contentPanel.Children.Add(_textBlock);

            // 2. 构造红点 (Red Dot) - 默认隐藏
            _redDot = new Ellipse
            {
                Width = 6, Height = 6,
                Fill = Brushes.Red,
                Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Left, // 稍微有些偏移逻辑
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(12, -2, 0, 0) // 手动调整位置到 Icon 右上角
            };

            // 3. 构造 Badge (数字角标) - 默认隐藏
            _badgeText = new TextBlock {
                Text = "0", FontSize = 9, Foreground = Brushes.White,
                FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center
            };
            _badgeBorder = new Border
            {
                Background = Brushes.Red,
                CornerRadius = new CornerRadius(7),
                MinWidth = 14, Height = 14,
                Padding = new Thickness(3, 0, 3, 0),
                Child = _badgeText,
                Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, -6, -8, 0) // 悬挂在右上角
            };

            // 4. 组装层叠布局
            var rootGrid = new Grid();
            rootGrid.Children.Add(contentPanel); // Layer 0: Content
            rootGrid.Children.Add(_redDot);      // Layer 1: Red Dot
            rootGrid.Children.Add(_badgeBorder); // Layer 2: Badge

            Child = rootGrid;
        }


        // --- 状态更新 ---

        protected override void OnSelectionStateChanged(bool isSelected)
        {
            var brush = isSelected ? BrushSelectedFg : BrushNormalFg;
            _textBlock.Foreground = brush;
            _iconPath.Fill = brush;
        }

        // --- 公开方法供外部调用 ---

        public void SetRedDot(bool show)
        {
            _redDot.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        public void SetBadge(int count)
        {
            if (count <= 0)
            {
                _badgeBorder.Visibility = Visibility.Collapsed;
            }
            else
            {
                _badgeText.Text = count > 99 ? "99+" : count.ToString();
                _badgeBorder.Visibility = Visibility.Visible;
            }
        }
    }
}