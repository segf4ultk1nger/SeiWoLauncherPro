using SeiWoLauncherPro.Controls.SymbolicIcons;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SeiWoLauncherPro.Controls
{
    public class IconTextSegmentedTabButton : SegmentedTabButtonBase
    {
        // 替换 Path 为我们的自定义 Symbolic 控件
        private readonly SymbolicIconBase _icon;
        private readonly TextBlock _textBlock;

        private UIElement _layoutRoot;

        public IconTextSegmentedTabButton(SymbolicIconBase symIcon, string text)
        {
            // 初始化自定义 Icon 控件
            _icon = symIcon;

            _textBlock = new TextBlock
            {
                Text = text,
                FontSize = 14,
                Margin = new Thickness(5,0,0,0),
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = BrushNormalFg
            };

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Background = Brushes.Transparent,
            };
            panel.Children.Add(_icon);
            panel.Children.Add(_textBlock);

            OnSelectionStateChanged(IsSelected);

            // 设置 Button 的 Content (或者基类的 Child)
            Child = new SimplePanel() {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = Brushes.Transparent,
            }.Children(panel);
        }

        // 这里的逻辑保持不变，但操作的是我们的 IconColor 属性
        protected override void OnSelectionStateChanged(bool isSelected)
        {
            var brush = isSelected ? BrushSelectedFg : BrushNormalFg;

            _textBlock.Foreground = brush;

            // 这里会触发 SymbolicIconGeneratorBase 的属性回调，重新生成 DrawingImage
            _icon.IconColor = brush;
        }
    }
}