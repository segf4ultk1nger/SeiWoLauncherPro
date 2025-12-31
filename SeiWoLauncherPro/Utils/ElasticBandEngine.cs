using System;

namespace SeiWoLauncherPro.Utils
{
    /// <summary>
    /// 橡皮筋物理引擎
    /// <para>负责计算超出边界时的非线性阻尼位移。</para>
    /// </summary>
    public static class ElasticBandEngine
    {
        /// <summary>
        /// 计算应用阻尼后的渲染位置
        /// </summary>
        /// <param name="rawOffset">逻辑位置（手指实际拖到的位置）</param>
        /// <param name="min">边界最小值 (e.g. -800)</param>
        /// <param name="max">边界最大值 (e.g. 0)</param>
        /// <param name="viewportSize">视口尺寸 (用于计算相对拉伸比例)</param>
        /// <returns>渲染位置</returns>
        public static double ApplyTension(double rawOffset, double min, double max, double viewportSize)
        {
            // 1. 在边界内：直接返回，无阻尼
            if (rawOffset >= min && rawOffset <= max)
            {
                return rawOffset;
            }

            // 2. 超出最大值 (拉过头了，通常是左边/上边)
            if (rawOffset > max)
            {
                double overshot = rawOffset - max;
                return max + ComputeDamping(overshot, viewportSize);
            }

            // 3. 超出最小值 (拉到底了，通常是右边/下边)
            if (rawOffset < min)
            {
                double overshot = min - rawOffset;
                return min - ComputeDamping(overshot, viewportSize);
            }

            return rawOffset;
        }

        /// <summary>
        /// 核心阻尼算法 (Pan-Logarithmic Damping)
        /// <para>模拟真实的物理材料拉伸：拉得越长，需要的力呈指数级上升。</para>
        /// </summary>
        private static double ComputeDamping(double inputOvershot, double viewportSize)
        {
            if (viewportSize <= 0) return inputOvershot * 0.5; // 防御性编程

            // 算法 A: 简单线性 (你提到的)
            // return inputOvershot * 0.5;

            // 算法 B: 渐近线衰减 (SeiWo Pro 推荐)
            // 公式：y = f(x) = (1 - (1 / ((x * c / d) + 1))) * d / c
            // 效果：无论你拉多远，渲染位移永远不会超过 viewportSize

            double factor = 0.55; // 阻尼系数，越小越硬
            double maxStretch = viewportSize; // 最大允许拉出的视觉距离

            double result = maxStretch * (1.0 - Math.Exp(-(inputOvershot * factor) / maxStretch));

            return result;
        }
    }
}