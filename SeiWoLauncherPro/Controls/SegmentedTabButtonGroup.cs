using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace SeiWoLauncherPro.Controls
{
    public sealed class SegmentedTabButtonGroup : SmoothBorder
    {
        // 存放所有子按钮的容器，用 UniformGrid 很有苹果那种等分的感觉
        private readonly UniformGrid _layoutRoot;
        private readonly List<SegmentedTabButtonBase> _items = new List<SegmentedTabButtonBase>();

        // 选中项改变事件
        public event Action<int, SegmentedTabButtonBase>? SelectionChanged;

        public SegmentedTabButtonGroup()
        {
            // 1. 容器样式 (也就是图里那个深黑色的底槽)
            Background = new SolidColorBrush(Color.FromArgb(128, 66,66,66)); // 很深的灰
            CornerRadius = new CornerRadius(10);
            Smoothness = 0.6;
            Padding = new Thickness(2); // 内边距，让里面的按钮不顶边
            SnapsToDevicePixels = true;

            // 限制一下高度，不设也行，但这种控件通常高度固定
            Height = 44;

            _layoutRoot = new UniformGrid { Rows = 1 };
            Child = _layoutRoot;
        }

        public void AddButton(SegmentedTabButtonBase button, bool isSelected = false)
        {
            button.Click += (s, e) => OnItemClicked(button);

            _layoutRoot.Children.Add(button);
            _items.Add(button);

            // 初始状态
            if (isSelected || _items.Count == 1)
            {
                SetSelection(_items.IndexOf(button));
            }
            else
            {
                button.IsSelected = false;
            }
        }

        private void OnItemClicked(SegmentedTabButtonBase clickedItem)
        {
            var index = _items.IndexOf(clickedItem);
            if (index >= 0)
            {
                SetSelection(index);
                SelectionChanged?.Invoke(index, clickedItem);
            }
        }

        private void SetSelection(int index)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                // 这里调用 Base 的 IsSelected，触发多态的 OnSelectionStateChanged
                _items[i].IsSelected = (i == index);
            }
        }

        // 辅助方法：获取指定类型的按钮以便操作 Badge
        public T GetButton<T>(int index) where T : SegmentedTabButtonBase
        {
            if (index >= 0 && index < _items.Count)
                return _items[index] as T;
            return null;
        }
    }

}