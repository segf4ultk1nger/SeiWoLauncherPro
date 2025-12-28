using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SeiWoLauncherPro.Controls
{
    public abstract class SegmentedTabButtonBase : SmoothBorder
    {
        private bool _isSelected;

        // 定义 Apple 风格的配色常量
        protected static readonly Brush BrushSelectedBg = new SolidColorBrush(Color.FromArgb(188, 47,47,47));
        protected static readonly Brush BrushSelectedBorder = new SolidColorBrush(Color.FromRgb(129,129,129));
        protected static readonly Brush BrushNormalBg = Brushes.Transparent;
        protected static readonly Brush BrushSelectedFg = Brushes.White;
        protected static readonly Brush BrushNormalFg = new SolidColorBrush(Color.FromRgb(148, 148, 148));

        public event EventHandler Click;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    // 1. 基类负责处理容器背景
                    Background = _isSelected ? BrushSelectedBg : BrushNormalBg;
                    BorderBrush = _isSelected ? BrushSelectedBorder : new SolidColorBrush(Colors.Transparent);
                    // 2. 通知子类处理内容的前景色/填充色
                    OnSelectionStateChanged(_isSelected);
                }
            }
        }

        protected SegmentedTabButtonBase()
        {
            // 通用容器样式
            CornerRadius = new CornerRadius(6);
            Smoothness = 0.6;
            Margin = new Thickness(2);
            Padding = new Thickness(3, 0, 3, 0);
            Background = BrushNormalBg;
            Cursor = Cursors.Hand;
            SnapsToDevicePixels = true;
            BorderThickness = 1.0;
            BorderBrush = new SolidColorBrush(Colors.Transparent);
            BorderPosition = BorderPosition.Center;

            // 鼠标点击事件
            MouseLeftButtonDown += (s, e) => Click?.Invoke(this, EventArgs.Empty);

        }

        /// <summary>
        /// 子类必须实现这个方法来响应选中状态的变化（改字色、改Icon色等）
        /// </summary>
        protected abstract void OnSelectionStateChanged(bool isSelected);
    }
}