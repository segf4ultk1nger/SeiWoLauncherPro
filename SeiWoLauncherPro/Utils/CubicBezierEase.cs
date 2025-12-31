using System;
using System.Runtime.CompilerServices;

namespace SeiWoLauncherPro.Utils
{
    /// <summary>
    /// 实现 CSS 风格的 cubic-bezier(p1x, p1y, p2x, p2y)
    /// </summary>
    public readonly struct CubicBezierEase
    {
        private const double Epsilon = 1e-6; // 精度阈值

        public readonly double X1, Y1, X2, Y2;

        public CubicBezierEase(double x1, double y1, double x2, double y2)
        {
            // CSS 规范要求 X 值必须在 [0, 1] 之间
            X1 = Math.Clamp(x1, 0, 1);
            Y1 = y1;
            X2 = Math.Clamp(x2, 0, 1);
            Y2 = y2;
        }

        // === CSS 预设 (你可以直接用这些) ===
        public static readonly CubicBezierEase Linear = new(0.0, 0.0, 1.0, 1.0);
        public static readonly CubicBezierEase Ease = new(0.25, 0.1, 0.25, 1.0);
        public static readonly CubicBezierEase EaseIn = new(0.42, 0.0, 1.0, 1.0);
        public static readonly CubicBezierEase EaseOut = new(0.0, 0.0, 0.58, 1.0);
        public static readonly CubicBezierEase EaseInOut = new(0.42, 0.0, 0.58, 1.0);

        // 类似 iOS 顺滑回弹的参数
        public static readonly CubicBezierEase Smooth = new(0.33, 1, 0.68, 1);

        /// <summary>
        /// 核心计算：输入时间进度 x (0~1)，输出插值进度 y
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Solve(double timeX)
        {
            // 边界情况优化
            if (timeX <= 0) return 0;
            if (timeX >= 1) return 1;
            if (X1 == Y1 && X2 == Y2) return timeX; // 线性优化

            // 1. 求 t: 给定 x，求贝塞尔参数 t
            // x(t) = 3(1-t)^2 * t * x1 + 3(1-t) * t^2 * x2 + t^3
            double t = SolveT(timeX);

            // 2. 求 y: 将 t 带入 y(t) 公式
            return SampleCurveY(t);
        }

        private double SampleCurveX(double t) => ((3 * (1 - t) * t * X1) + (3 * (1 - t) * t * t * X2) + (t * t * t)) * (1 - t) + t * t * t; // 展开式简化版

        /// <summary>
        /// 使用霍纳法则 (Horner's Method) 优化的贝塞尔求值，减少乘法开销
        /// 展开式：B(t) = t^3(3P1 - 3P2 + 1) + t^2(3P2 - 6P1) + t(3P1)
        /// </summary>
        private double CalcBezier(double t, double p1, double p2)
        {
            double c = 3.0 * p1;
            double b = 3.0 * p2 - 6.0 * p1;
            double a = 3.0 * p1 - 3.0 * p2 + 1.0;

            return ((a * t + b) * t + c) * t;
        }

        private double SampleCurveY(double t) => CalcBezier(t, Y1, Y2);

        // 计算 x(t) 的导数，用于牛顿迭代
        private double SampleCurveDerivativeX(double t)
        {
            // dx/dt
            double ax = 1.0 - 3.0 * X2 + 3.0 * X1;
            double bx = 3.0 * X2 - 6.0 * X1;
            double cx = 3.0 * X1;
            return 3.0 * ax * t * t + 2.0 * bx * t + cx;
        }

        // 使用牛顿迭代法求解 t
        private double SolveT(double x)
        {
            double t = x; // 初始猜测

            // 8次迭代通常足够收敛
            for (int i = 0; i < 8; i++)
            {
                double x2 = CalcBezier(t, X1, X2) - x;
                if (Math.Abs(x2) < Epsilon) return t;

                double d2 = SampleCurveDerivativeX(t);
                if (Math.Abs(d2) < Epsilon) break;

                t = t - x2 / d2;
            }
            return Math.Clamp(t, 0.0, 1.0);
        }
    }
}