using System.Windows;
using System.Windows.Controls;

namespace SeiWoLauncherPro.Controls {
    public class TextOnlySegmentedTabButton : SegmentedTabButtonBase
    {
        private TextBlock _textBlock;

        public TextOnlySegmentedTabButton(string text)
        {
            _textBlock = new TextBlock
            {
                Text = text,
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = BrushNormalFg
            };
            // 核心：设置内容
            Child = _textBlock;
        }

        protected override void OnSelectionStateChanged(bool isSelected)
        {
            _textBlock.Foreground = isSelected ? BrushSelectedFg : BrushNormalFg;
        }
    }
}