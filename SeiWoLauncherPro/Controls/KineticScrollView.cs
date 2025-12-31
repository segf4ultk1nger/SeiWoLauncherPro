using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SeiWoLauncherPro.Utils;

namespace SeiWoLauncherPro.Controls {
    public enum ScrollMode {
        Free, // 无极滚动
        Paging, // 分页磁吸
    }

    public enum DefaultPageStrategy {
        First, // 默认：第一页 (Index 0)
        Last, // 最后一页
        Custom // 自定义 Index
    }

    public enum InteractionState {
        Idle, // 静止
        Dragging, // 手指拖拽中
        Flinging, // 惯性滚动/动画中
    }

    // [New Feature] 轻量级事件参数，避免大量装箱
    public class ScrollChangedEventArgs : EventArgs {
        public double Offset { get; }
        public ScrollChangedEventArgs(double offset) => Offset = offset;
    }

    public class PageChangedEventArgs : EventArgs {
        public int NewIndex { get; }
        public int OldIndex { get; }

        public PageChangedEventArgs(int newIndex, int oldIndex) {
            NewIndex = newIndex;
            OldIndex = oldIndex;
        }
    }

    public class KineticScrollView : ContentControl, IDisposable {
        // === 1. 配置属性 (已扩展) ===
        public ScrollMode ScrollMode { get; set; } = ScrollMode.Paging;
        public Orientation Orientation { get; set; } = Orientation.Horizontal;
        public double PageSize { get; set; } = 0;
        public bool CanMouseWheel { get; set; } = true;

        // 1. 默认页面策略
        public DefaultPageStrategy DefaultPageStrategy { get; set; } = DefaultPageStrategy.First;
        public int CustomDefaultPageIndex { get; set; } = 0;

        // 2. 公开只读状态 (High Performance Access)
        public double CurrentOffset => _logicalOffset;

        private int _currentPageIndex = 0;
        public int CurrentPageIndex => _currentPageIndex; // 现在的 Getter 直接返回缓存值，不再实时计算

        public double ViewportWidth => _viewport?.ActualWidth ?? 0;
        public double ExtentWidth => _mover?.ActualWidth ?? 0;

        public double ViewportHeight => _viewport?.ActualHeight ?? 0;
        public double ExtentHeight => _mover?.ActualHeight ?? 0;

        // 3. 交互状态机
        private InteractionState _interactionState = InteractionState.Idle;

        public InteractionState InteractionState {
            get => _interactionState;
            private set {
                if (_interactionState != value) {
                    _interactionState = value;
                    StateChanged?.Invoke(this, value);
                }
            }
        }

        // 4. 事件定义
        public event EventHandler<ScrollChangedEventArgs> ScrollChanged;
        public event EventHandler<PageChangedEventArgs> PageChanged;
        public event EventHandler<InteractionState> StateChanged;

        // 5. 初始化标志位
        private bool _hasAppliedDefaultLayout = false;

        // [New Feature]: 临时禁止触摸或拖拽
        // 这里分离了 IsScrollEnabled (总开关) 和 IsDragEnabled (仅禁止手势拖拽但允许滚轮/API)
        // 根据你的描述 "临时禁止触摸或者鼠标拖拽"，通常是指禁止手势交互。
        public bool IsDragEnabled { get; set; } = true;

        // === 2. 物理引擎组件 & [New Feature] Easing 自定义 ===
        private readonly VelocityTracker _tracker;
        private readonly SmoothAnimator _animator;

        // 将 static readonly 改为 public property，允许外部 Override
        // 保持了你原本的参数作为默认值
        public CubicBezierEase EaseFriction { get; set; } = new CubicBezierEase(0.25, 1, 0.5, 1);
        public CubicBezierEase EaseSpring { get; set; } = new CubicBezierEase(0.33, 1, 0.68, 1);
        public CubicBezierEase EaseWheel { get; set; } = CubicBezierEase.EaseOut;

        // === 3. 视觉组件 ===
        private FrameworkElement _mover;
        private TranslateTransform _transform;
        private FrameworkElement _viewport;

        // === 4. 运行时状态 ===
        private double _logicalOffset = 0;
        private double _minOffset = 0;
        private double _maxOffset = 0;

        private bool _isDragging = false;
        private Point _lastPos;
        private Point _startDragPos;
        private const double DragThreshold = 6.0;

        private double _wheelTarget = 0;

        // 方便获取视口尺寸的 Helper
        private double ViewportSize => Orientation == Orientation.Horizontal
            ? (_viewport?.ActualWidth ?? 0)
            : (_viewport?.ActualHeight ?? 0);

        // 方便获取当前页面大小的 Helper
        private double ActualPageSize {
            get {
                double size = PageSize > 0 ? PageSize : ViewportSize;
                return size <= 0 ? 1 : size;
            }
        }

        public KineticScrollView() {
            _tracker = new VelocityTracker();
            _animator = new SmoothAnimator();

            this.Template = CreateTemplate();

            CompositionTarget.Rendering += OnRendering;
            this.SizeChanged += (s, e) => RecalculateBounds();
            this.Loaded += (s, e) => RecalculateBounds();
        }

        // ... [CreateTemplate, OnApplyTemplate, OnMoverSizeChanged, RecalculateBounds 保持不变] ...
        // 为了篇幅省略这部分未修改代码，请保留你原有的实现

        private ControlTemplate CreateTemplate() {
            // (保持原样...)
            var template = new ControlTemplate(typeof(KineticScrollView));
            var viewportFactory = new FrameworkElementFactory(typeof(Grid));
            viewportFactory.SetValue(ClipToBoundsProperty, true);
            viewportFactory.SetValue(BackgroundProperty, Brushes.Transparent);
            viewportFactory.Name = "PART_Viewport";
            var canvasFactory = new FrameworkElementFactory(typeof(Canvas));
            var moverFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            moverFactory.Name = "PART_Mover";
            moverFactory.SetValue(Canvas.TopProperty, 0.0);
            moverFactory.SetValue(Canvas.LeftProperty, 0.0);
            canvasFactory.AppendChild(moverFactory);
            viewportFactory.AppendChild(canvasFactory);
            template.VisualTree = viewportFactory;
            return template;
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            _viewport = this.Template.FindName("PART_Viewport", this) as FrameworkElement;
            _mover = this.Template.FindName("PART_Mover", this) as FrameworkElement;
            if (_mover != null) {
                _transform = new TranslateTransform();
                _mover.RenderTransform = _transform;
                _mover.SizeChanged -= OnMoverSizeChanged;
                _mover.SizeChanged += OnMoverSizeChanged;
            }
        }

        private void OnMoverSizeChanged(object sender, SizeChangedEventArgs e) => RecalculateBounds();

        private void RecalculateBounds() {
            if (_mover == null || _viewport == null) return;
            double viewportSize =
                Orientation == Orientation.Horizontal ? _viewport.ActualWidth : _viewport.ActualHeight;
            double contentSize = Orientation == Orientation.Horizontal ? _mover.ActualWidth : _mover.ActualHeight;

            // 避免无效计算
            if (viewportSize == 0 || contentSize == 0) return;

            // 1. 计算边界
            if (contentSize <= viewportSize) {
                _minOffset = 0;
                _maxOffset = 0;
            } else {
                _minOffset = -(contentSize - viewportSize);
                _maxOffset = 0;
            }

            // 2. [Modified] 首次加载应用默认页面策略 (Bootstrapping)
            if (!_hasAppliedDefaultLayout) {
                // 确保由于 Layout 异步性，只有在确实有内容可滚时才执行跳转
                if (_minOffset < 0 || (ScrollMode == ScrollMode.Paging && ActualPageSize > 0)) {
                    ApplyDefaultPageStrategy();
                    _hasAppliedDefaultLayout = true;
                }
            } else {
                // 非首次加载（如窗口Resize），仅做越界修正
                if (_logicalOffset < _minOffset) UpdateStateImmediate(_minOffset);
                if (_logicalOffset > _maxOffset) UpdateStateImmediate(_maxOffset);
            }
        }

        // [New Helper] 执行默认页跳转逻辑
        private void ApplyDefaultPageStrategy() {
            double targetOffset = 0;

            if (ScrollMode == ScrollMode.Paging) {
                int targetIndex = 0;
                switch (DefaultPageStrategy) {
                    case DefaultPageStrategy.First:
                        targetIndex = 0;
                        break;
                    case DefaultPageStrategy.Last:
                        // 计算最大页数
                        double contentLen = Orientation == Orientation.Horizontal
                            ? _mover.ActualWidth
                            : _mover.ActualHeight;
                        targetIndex = (int)Math.Max(0, Math.Ceiling(contentLen / ActualPageSize) - 1);
                        break;
                    case DefaultPageStrategy.Custom:
                        targetIndex = CustomDefaultPageIndex;
                        break;
                }

                targetOffset = -(targetIndex * ActualPageSize);
            } else {
                // Free Mode 下也可以简单支持一下
                if (DefaultPageStrategy == DefaultPageStrategy.Last) targetOffset = _minOffset;
            }

            // 强制瞬移，不播放动画
            UpdateStateImmediate(targetOffset);

            // 强制更新一次 PageIndex
            CalculateCurrentPageIndex(targetOffset);
        }


        // --- [Modified] Input Pipeline ---

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e) {
            if (!IsDragEnabled) return;

            _animator.Interrupt();
            InteractionState = InteractionState.Dragging; // [State Update]

            double currentVisualOffset = Orientation == Orientation.Horizontal ? _transform.X : _transform.Y;

            if (currentVisualOffset >= _minOffset && currentVisualOffset <= _maxOffset) {
                _logicalOffset = currentVisualOffset;
            } else {
                _logicalOffset = currentVisualOffset;
            }

            _wheelTarget = _logicalOffset;
            _lastPos = e.GetPosition(this);
            _startDragPos = _lastPos;
            _isDragging = false; // Reset drag state

            _tracker.Clear();
            _tracker.AddPosition(_logicalOffset);

            CaptureMouse();
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e) {
            if (IsMouseCaptured && IsDragEnabled) // Check flag again for safety
            {
                var currentPos = e.GetPosition(this);

                if (!_isDragging) {
                    double dist = (currentPos - _startDragPos).Length;
                    if (dist > DragThreshold) {
                        _isDragging = true;
                        _lastPos = currentPos;
                    } else return;
                }

                double delta = Orientation == Orientation.Horizontal
                    ? currentPos.X - _lastPos.X
                    : currentPos.Y - _lastPos.Y;

                _logicalOffset += delta;

                double renderOffset =
                    ElasticBandEngine.ApplyTension(_logicalOffset, _minOffset, _maxOffset, ViewportSize);

                UpdateVisual(renderOffset);
                _tracker.AddPosition(renderOffset);

                _lastPos = currentPos;
            }
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e) {
            ReleaseMouseCapture();

            if (_isDragging) {
                e.Handled = true;
                _isDragging = false;

                // 如果松手时没有速度，或者被禁用了，状态归为 Idle
                // 否则在 DecideDestination 里会设为 Flinging
                if (!IsDragEnabled) {
                    InteractionState = InteractionState.Idle; // [State Update]
                } else {
                    double velocity = _tracker.ComputeVelocity();
                    DecideDestination(velocity);
                }
            } else {
                InteractionState = InteractionState.Idle; // [State Update] - 只是点击，未拖拽
            }
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e) {
            if (!CanMouseWheel) return;
            if (IsMouseCaptured) return;

            // [Modified]: 使用属性 EaseFriction / EaseWheel
            CubicBezierEase easing = ScrollMode == ScrollMode.Paging ? EaseFriction : EaseWheel;
            double duration = ScrollMode == ScrollMode.Paging ? 500 : 300;

            if (ScrollMode == ScrollMode.Free) {
                double scrollAmount = e.Delta > 0 ? 100 : -100;
                _wheelTarget += scrollAmount;
            } else {
                double pageSize = ActualPageSize;
                double currentTargetPage = -_wheelTarget / pageSize;
                int targetIndex = (int)Math.Round(currentTargetPage);

                if (e.Delta < 0) targetIndex++;
                else targetIndex--;

                double rawTarget = -targetIndex * pageSize;
                _wheelTarget = Math.Round(rawTarget);
            }

            _wheelTarget = Math.Clamp(_wheelTarget, _minOffset, _maxOffset);
            _animator.Start(_logicalOffset, _wheelTarget, duration, easing);

            e.Handled = true;
        }

        private void DecideDestination(double velocity) {
            double currentVisual = Orientation == Orientation.Horizontal ? _transform.X : _transform.Y;
            double target = currentVisual;
            double duration = 600;
            // [Modified]: 使用属性
            CubicBezierEase easing = EaseFriction;

            if (currentVisual > _maxOffset) {
                target = _maxOffset;
                easing = EaseSpring; // [Modified]
                duration = 500;
            } else if (currentVisual < _minOffset) {
                target = _minOffset;
                easing = EaseSpring; // [Modified]
                duration = 500;
            } else {
                if (ScrollMode == ScrollMode.Free) {
                    target = currentVisual + (velocity * 0.4);
                } else {
                    double pageSize = ActualPageSize;
                    double pageFloat = -currentVisual / pageSize;
                    int targetPage = (int)Math.Floor(pageFloat + 0.5);

                    if (Math.Abs(velocity) > 600) {
                        if (velocity < 0) {
                            if (targetPage == (int)Math.Floor(pageFloat)) targetPage++;
                        } else {
                            if (targetPage == (int)Math.Ceiling(pageFloat)) targetPage--;
                        }
                    }

                    target = -targetPage * pageSize;
                }

                target = Math.Clamp(target, _minOffset, _maxOffset);
                double dist = Math.Abs(target - currentVisual);
                duration = Math.Clamp(dist * 1.5, 400, 800);
            }

            _wheelTarget = target;

            // 在开启动画前设置状态
            InteractionState = InteractionState.Flinging; // [State Update]
            _animator.Start(currentVisual, target, duration, easing);
        }

        // === [New Feature] Standard API Implementation ===

        /// <summary>
        /// 统一的内部滚动处理，处理动画与状态同步
        /// </summary>
        private void RequestScroll(double targetOffset, bool withAnimation, double durationMs = 500) {
            // 越界钳制
            targetOffset = Math.Clamp(targetOffset, _minOffset, _maxOffset);

            if (withAnimation) {
                _wheelTarget = targetOffset;
                // 默认使用 EaseFriction (你可以根据需要重载参数)
                _animator.Start(_logicalOffset, targetOffset, durationMs, EaseFriction);
            } else {
                UpdateStateImmediate(targetOffset);
            }
        }

        /// <summary>
        /// 瞬间更新状态，重置所有物理追踪器，防止瞬移产生的巨大速度
        /// </summary>
        private void UpdateStateImmediate(double targetOffset) {
            _animator.Interrupt();
            _logicalOffset = targetOffset;
            _wheelTarget = targetOffset;
            UpdateVisual(targetOffset);
            _tracker.Clear();
            _tracker.AddPosition(targetOffset);
        }

        // --- Free Mode API ---

        public void ScrollToOffset(double offset, bool animate = true)
            => RequestScroll(offset, animate);

        public void ScrollToLeft(bool animate = true)
            => RequestScroll(0, animate); // 左侧即 0

        public void ScrollToTop(bool animate = true)
            => RequestScroll(0, animate); // 顶部即 0

        public void ScrollToRight(bool animate = true)
            => RequestScroll(_minOffset, animate); // 右侧即最小值 (负数)

        public void ScrollToBottom(bool animate = true)
            => RequestScroll(_minOffset, animate); // 底部即最小值 (负数)


        // --- Paging Mode API ---

        public void ScrollToPage(int pageIndex, bool animate = true) {
            if (pageIndex < 0) pageIndex = 0;
            // 计算目标 Offset
            double target = -(pageIndex * ActualPageSize);
            RequestScroll(target, animate);
        }

        public void ScrollToFirstPage(bool animate = true) => ScrollToPage(0, animate);

        public void ScrollToLastPage(bool animate = true) {
            // 计算总页数
            double contentLen = Orientation == Orientation.Horizontal ? _mover.ActualWidth : _mover.ActualHeight;
            // 避免除以0
            int maxPage = (int)Math.Max(0, Math.Ceiling(contentLen / ActualPageSize) - 1);
            ScrollToPage(maxPage, animate);
        }


        // ... [OnRendering, UpdateVisual, Dispose 保持不变] ...
        private void OnRendering(object sender, EventArgs e) {
            if (_animator.Update()) {
                _logicalOffset = _animator.CurrentValue;
                UpdateVisual(_logicalOffset);
            } else {
                // 动画结束，且没有在拖拽中
                if (!_isDragging && InteractionState == InteractionState.Flinging) {
                    InteractionState = InteractionState.Idle; // [State Update] 动画完成，回归静止
                }
            }
        }

        private void UpdateVisual(double offset) {
            if (_transform == null) return;

            // 1. 应用变换
            if (Orientation == Orientation.Horizontal) _transform.X = offset;
            else _transform.Y = offset;

            // 2. [New Feature] 触发 ScrollChanged 事件
            // 注意：这里是高频调用，订阅者不应做耗时操作
            ScrollChanged?.Invoke(this, new ScrollChangedEventArgs(offset));

            // 3. [New Feature] 计算并触发 PageChanged
            if (ScrollMode == ScrollMode.Paging) {
                CalculateCurrentPageIndex(offset);
            }
        }

        private void CalculateCurrentPageIndex(double offset) {
            double pageSize = ActualPageSize;
            if (pageSize <= 0) return;

            // 使用银行家舍入法或普通四舍五入确保 Index 稳定性
            int newIndex = (int)Math.Round(-offset / pageSize);

            if (newIndex != _currentPageIndex) {
                var args = new PageChangedEventArgs(newIndex, _currentPageIndex);
                _currentPageIndex = newIndex;
                PageChanged?.Invoke(this, args);
            }
        }

        public void Dispose() {
            CompositionTarget.Rendering -= OnRendering;
            if (_mover != null) {
                _mover.SizeChanged -= OnMoverSizeChanged;
            }
        }
    }
}