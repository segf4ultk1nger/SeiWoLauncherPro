using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace SeiWoLauncherPro.Utils
{
    /// <summary>
    /// 通用动力滚动控制器 (The Kinetic Engine)
    /// <para>负责处理触摸/鼠标拖拽、速度采样、惯性计算以及磁吸对齐 (Snapping)。</para>
    /// </summary>
    public class KineticScrollProvider : IDisposable
    {
        // === 核心依赖 ===
        private readonly FrameworkElement _inputSource;   // 接收事件的容器 (如 Viewport)
        private readonly TranslateTransform _targetTransform; // 被移动的对象 (如 Track)
        private readonly VelocityTracker _tracker;       // 刚才写的速度采样器
        private readonly SmoothAnimator _animator;       // 之前的动画驱动器

        // === 物理参数 ===
        public double SnapInterval { get; set; } = 0;    // 分页宽度 (0 表示不分页，自由滚动)
        public double MinOffset { get; set; } = -1000;   // 滚动边界 (通常是 ContentWidth - ViewportWidth)
        public double MaxOffset { get; set; } = 0;       // 滚动起点 (通常是 0)
        public double DragThreshold { get; set; } = 5.0; // 防抖阈值 (px)

        // === 内部状态 ===
        private bool _isDragging;
        private Point _lastMousePos;
        private double _dragStartOffset;
        private Point _dragStartMousePos;

        // [New] 逻辑偏移量：这是数学上的真实位置，包含了用户把鼠标甩出屏幕几公里的距离
        private double _logicalOffset;

        // [New] 视口宽度：用于计算阻尼比例
        private double _viewportSize;

        // 标记是否被动量滚动接管
        private bool _isInInertia;

        // 贝塞尔曲线：模拟 iOS UIScrollView 的减速曲线 (QuartOut)
        private static readonly CubicBezierEase _decelerationCurve = new CubicBezierEase(0.25, 1, 0.5, 1);
        // 贝塞尔曲线：回弹曲线 (Spring-like)
        private static readonly CubicBezierEase _springCurve = new CubicBezierEase(0.33, 1, 0.68, 1);

        public KineticScrollProvider(FrameworkElement inputSource, TranslateTransform targetTransform)
        {
            _inputSource = inputSource;
            _targetTransform = targetTransform;

            _tracker = new VelocityTracker();
            _animator = new SmoothAnimator();

            // 挂载核心渲染循环 (Heartbeat)
            CompositionTarget.Rendering += OnRendering;

            // 挂载输入事件
            _inputSource.PreviewMouseLeftButtonDown += OnDown;
            _inputSource.PreviewMouseMove += OnMove;
            _inputSource.PreviewMouseLeftButtonUp += OnUp;

            // 防止鼠标移出窗口导致状态卡死
            _inputSource.MouseLeave += OnUp;

            _viewportSize = inputSource.ActualWidth;

            // 监听视口大小变化，防止动态调整窗口时阻尼失效
            inputSource.SizeChanged += (s, e) => _viewportSize = e.NewSize.Width;
        }

        #region Input Pipeline (输入管道)

        private void OnDown(object sender, MouseButtonEventArgs e)
        {
            _animator.Interrupt();
            _isInInertia = false;

            _lastMousePos = e.GetPosition(_inputSource);
            _dragStartMousePos = _lastMousePos;

            // [Key Change]
            // 按下时，逻辑位置必须重置为当前的渲染位置
            // 否则如果上次回弹没结束就按下，会导致位置跳变
            _logicalOffset = _targetTransform.X;

            _isDragging = false;

            _tracker.Clear();
            _tracker.AddPosition(_logicalOffset); // 追踪逻辑位置还是渲染位置？通常追踪渲染位置更符合直觉

            _inputSource.CaptureMouse();
        }

        private void OnMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;

            var currentPos = e.GetPosition(_inputSource);
            double deltaX = currentPos.X - _lastMousePos.X;

            if (!_isDragging)
            {
                if (Math.Abs(currentPos.X - _dragStartMousePos.X) > DragThreshold) _isDragging = true;
                else return;
            }

            // [Key Change]
            // 不再在这里乘系数，而是直接累加到逻辑偏移量
            _logicalOffset += deltaX;

            // 计算带阻尼的渲染位置
            double renderOffset = ElasticBandEngine.ApplyTension(
                _logicalOffset,
                MinOffset,
                MaxOffset,
                _viewportSize
            );

            SetOffset(renderOffset);

            // 速度追踪通常追踪“手”的速度，所以喂 _logicalOffset 或者 deltaX 累加值
            // 但为了让抛掷力度自然，我们这里追踪 renderOffset 也没问题，
            // 不过追踪逻辑位置能让回弹时的初速度更真实。
            // 这里建议追踪 RenderOffset，因为最终决定惯性的是屏幕上的像素移动。
            _tracker.AddPosition(renderOffset);

            _lastMousePos = currentPos;
        }

        private void OnUp(object sender, MouseEventArgs e)
        {
            _inputSource.ReleaseMouseCapture();
            if (!_isDragging) return;
            _isDragging = false;

            double velocity = _tracker.ComputeVelocity();

            // 松手时，逻辑位置和渲染位置可能不一致（如果在拉伸状态）
            // 动画开始时，我们以当前的 RenderOffset 为起点
            SnapToDestination(velocity);
        }

        #endregion

        #region Physics Logic (物理大脑)

        private void SnapToDestination(double velocity)
        {
            double current = _targetTransform.X;
            double target = current;
            double duration = 500; // ms
            CubicBezierEase easing = _decelerationCurve;

            // A. 边界回弹 (Priority 1)
            // 如果松手时已经在界外，必须弹回来，忽略速度
            if (current > MaxOffset)
            {
                target = MaxOffset;
                easing = _springCurve; // 用 Q 弹的曲线
                duration = 400;
            }
            else if (current < MinOffset)
            {
                target = MinOffset;
                easing = _springCurve;
                duration = 400;
            }
            // B. 惯性与分页 (Priority 2)
            else
            {
                // 预测停靠点：当前位置 + (速度 * 投掷系数)
                // 0.25 表示“让子弹飞一会儿”的动量权重
                double projected = current + (velocity * 0.25);

                // 如果启用了分页 (SnapInterval > 0)
                if (SnapInterval > 0)
                {
                    // 计算最近的页码索引
                    // 假设向左滑是负数，所以除以 -SnapInterval
                    double pageIndex = Math.Round(-projected / SnapInterval);

                    // 约束页码范围 (防止飞太远)
                    // 这里简化处理，实际可以通过 Min/MaxOffset 算出来
                    // double maxPage = Math.Abs(MinOffset / SnapInterval);
                    // pageIndex = Math.Clamp(pageIndex, 0, maxPage);

                    target = -pageIndex * SnapInterval;

                    // 约束目标在边界内
                    target = Math.Clamp(target, MinOffset, MaxOffset);
                }
                else
                {
                    // 自由滚动模式 (Free Scroll)
                    target = projected;
                    target = Math.Clamp(target, MinOffset, MaxOffset);
                }

                // 根据距离动态调整动画时长，保持恒定速度感
                double distance = Math.Abs(target - current);
                duration = Math.Clamp(distance * 1.5, 300, 800); // 最少300ms，最多800ms
            }

            // 启动引擎
            _isInInertia = true;
            _animator.Start(current, target, duration, easing);
        }

        #endregion

        #region Rendering Loop (驱动层)

        private void OnRendering(object sender, EventArgs e)
        {
            // 只有当我们在惯性/回弹状态，且动画器正在跑时才更新
            if (_isInInertia && _animator.Update())
            {
                SetOffset(_animator.CurrentValue);
            }
        }

        private void SetOffset(double x)
        {
            _targetTransform.X = x;
            // 这里可以触发 OffsetChanged 事件供外部订阅
        }

        #endregion

        public void Dispose()
        {
            CompositionTarget.Rendering -= OnRendering;
            _inputSource.PreviewMouseLeftButtonDown -= OnDown;
            _inputSource.PreviewMouseMove -= OnMove;
            _inputSource.PreviewMouseLeftButtonUp -= OnUp;
            _inputSource.MouseLeave -= OnUp;
        }
    }
}