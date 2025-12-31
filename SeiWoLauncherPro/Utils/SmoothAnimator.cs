using System;
using System.Diagnostics;

namespace SeiWoLauncherPro.Utils
{
    public class SmoothAnimator
    {
        // 状态
        public bool IsRunning { get; private set; }
        public double CurrentValue { get; private set; }

        // 动画参数
        private double _startValue;
        private double _endValue;
        private double _durationMs;
        private long _startTimestamp;
        private CubicBezierEase _easing;

        // 默认使用 CSS EaseOut
        public SmoothAnimator()
        {
            _easing = CubicBezierEase.EaseOut;
        }

        /// <summary>
        /// 启动或重启动画
        /// </summary>
        /// <param name="from">起始值 (通常传入当前的 CurrentValue 以保证连贯)</param>
        /// <param name="to">目标值</param>
        /// <param name="durationMs">毫秒时长</param>
        /// <param name="easing">缓动函数 (可选)</param>
        public void Start(double from, double to, double durationMs, CubicBezierEase? easing = null)
        {
            _startValue = from;
            _endValue = to;
            _durationMs = durationMs;
            _easing = easing ?? CubicBezierEase.EaseOut;

            // 当前值立即对齐起点
            CurrentValue = from;

            // 记录高精度时间戳
            _startTimestamp = Stopwatch.GetTimestamp();
            IsRunning = true;
        }

        /// <summary>
        /// 立即打断动画，停在当前位置
        /// </summary>
        public void Interrupt()
        {
            IsRunning = false;
            // CurrentValue 保持不变，正好作为下一次 Start 的 from 参数
        }

        /// <summary>
        /// 在帧循环中调用此方法更新数值
        /// </summary>
        /// <returns>如果动画仍在运行返回 true，结束返回 false</returns>
        public bool Update()
        {
            if (!IsRunning) return false;

            // 1. 计算时间进度 [0, 1]
            long currentTimestamp = Stopwatch.GetTimestamp();
            double elapsedMs = (currentTimestamp - _startTimestamp) * 1000.0 / Stopwatch.Frequency;

            double progressRaw = elapsedMs / _durationMs;

            // 2. 检查结束
            if (progressRaw >= 1.0)
            {
                CurrentValue = _endValue;
                IsRunning = false;
                return false; // 本帧结束
            }

            // 3. 应用贝塞尔插值
            double easedProgress = _easing.Solve(progressRaw);

            // 4. Lerp (Linear Interpolation)
            CurrentValue = _startValue + (_endValue - _startValue) * easedProgress;

            return true;
        }
    }
}