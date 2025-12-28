using System;
using System.Windows;
using System.Windows.Controls;

namespace SeiWoLauncherPro.Controls
{
    /// <summary>
    /// 一个极简的 Z-Stack 面板。
    /// 行为类似 Grid，但没有 Row/Column 逻辑。
    /// 它会给所有子元素提供全部可用空间，子元素通过 Alignment 自行定位。
    /// 性能：O(N)，无多余分配。
    /// </summary>
    public class SimplePanel : Panel
    {
        // 1. 测量阶段：面板的大小 = 子元素中最大的那个宽高
        protected override Size MeasureOverride(Size availableSize)
        {
            var panelDesiredSize = new Size();
            var children = InternalChildren; // 使用 InternalChildren 避免拷贝，微优化

            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child == null) continue;

                // 给子元素无限的空间去测量它自己想要多大
                // (或者给 availableSize，看你是否希望限制子元素撑破容器)
                child.Measure(availableSize);

                // 记录最大的宽和高
                panelDesiredSize.Width = Math.Max(panelDesiredSize.Width, child.DesiredSize.Width);
                panelDesiredSize.Height = Math.Max(panelDesiredSize.Height, child.DesiredSize.Height);
            }

            return panelDesiredSize;
        }

        // 2. 排列阶段：把所有子元素铺满整个面板
        protected override Size ArrangeOverride(Size finalSize)
        {
            var children = InternalChildren;

            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child == null) continue;

                // 核心 Trick：直接给子元素整个矩形空间 (0, 0, W, H)
                // WPF 的子元素会自动根据自己的 HorizontalAlignment/VerticalAlignment
                // 在这个大矩形里找到自己的位置。
                // 比如 HorizontalAlignment="Right" 的子元素会自动贴到 finalSize.Width 处。
                child.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
            }

            return finalSize;
        }
    }
}