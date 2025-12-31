using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SeiWoLauncherPro.Controls
{
    public class SmoothPageIndicator : ContentControl
    {
        private Canvas _rootCanvas;
        private Rectangle _activeWorm; // 那个会动的“毛毛虫”
        private readonly List<Ellipse> _inactiveDots = new List<Ellipse>();

        private KineticScrollView _attachedScrollView;

        /// <summary>
        /// 【硬连线模式】直接接管 ScrollView 的心脏跳动，绕过 Binding 引擎
        /// </summary>
        public void AttachTo(KineticScrollView scrollView)
        {
            // 1. 防御性编程：如果之前连过别的，先断开（防止内存泄漏）
            if (_attachedScrollView != null)
            {
                _attachedScrollView.ScrollChanged -= OnScrollSync;
                _attachedScrollView.SizeChanged -= OnViewportSizeSync;
                // 如果你的 KineticScrollView 有 ContentSizeChanged 事件最好，没有的话可能需要手动更新 PageCount
            }

            _attachedScrollView = scrollView;

            if (_attachedScrollView == null) return;

            // 2. 建立神经连接
            // 监听高频滚动事件 (核心)
            _attachedScrollView.ScrollChanged += OnScrollSync;

            // 监听视口大小变化 (用于响应式调整 PageSize)
            _attachedScrollView.SizeChanged += OnViewportSizeSync;

            // 3. 初始状态同步 (Bootstrapping)
            SyncDimensions();
        }

// 专门处理滚动的高频回调，不做任何多余的逻辑
        private void OnScrollSync(object sender, ScrollChangedEventArgs e)
        {
            // 直接设置属性，触发 OnOffsetChanged -> UpdateWormPosition
            // 这里的开销仅仅是 DependencyProperty 的 SetValue，比 Binding 快得多
            this.CurrentOffset = e.Offset;
        }

// 处理窗口 Resize 或布局变化
        private void OnViewportSizeSync(object sender, SizeChangedEventArgs e)
        {
            SyncDimensions();
        }

// 统一的尺寸同步逻辑
        // 完善后的 SyncDimensions 自动计算页数逻辑
        public void SyncDimensions()
        {
            if (_attachedScrollView == null) return;

            // 获取视口大小
            double viewportLen = _attachedScrollView.Orientation == Orientation.Horizontal
                ? _attachedScrollView.ViewportWidth
                : _attachedScrollView.ViewportHeight;

            // 获取内容总长
            double extentLen = _attachedScrollView.Orientation == Orientation.Horizontal
                ? _attachedScrollView.ExtentWidth
                : _attachedScrollView.ExtentHeight;

            if (viewportLen <= 0) return;

            this.PageSize = viewportLen;

            // 自动计算页数：总长 / 页宽
            // 比如 1920宽，内容3840，就是 2页
            int count = (int)Math.Ceiling(extentLen / viewportLen);
            this.PageCount = Math.Max(1, count); // 至少得有1页吧
        }
        // === Dependency Properties ===

        // 1. 核心联动参数
        public int PageCount
        {
            get { return (int)GetValue(PageCountProperty); }
            set { SetValue(PageCountProperty, value); }
        }
        public static readonly DependencyProperty PageCountProperty =
            DependencyProperty.Register("PageCount", typeof(int), typeof(SmoothPageIndicator),
                new PropertyMetadata(0, OnLayoutPropertyChanged));

        public double CurrentOffset
        {
            get { return (double)GetValue(CurrentOffsetProperty); }
            set { SetValue(CurrentOffsetProperty, value); }
        }
        public static readonly DependencyProperty CurrentOffsetProperty =
            DependencyProperty.Register("CurrentOffset", typeof(double), typeof(SmoothPageIndicator),
                new PropertyMetadata(0.0, OnOffsetChanged));

        public double PageSize
        {
            get { return (double)GetValue(PageSizeProperty); }
            set { SetValue(PageSizeProperty, value); }
        }
        public static readonly DependencyProperty PageSizeProperty =
            DependencyProperty.Register("PageSize", typeof(double), typeof(SmoothPageIndicator),
                new PropertyMetadata(1.0, OnOffsetChanged)); // PageSize 变了也要重算位置

        // 2. 样式参数
        public double DotSize
        {
            get { return (double)GetValue(DotSizeProperty); }
            set { SetValue(DotSizeProperty, value); }
        }
        public static readonly DependencyProperty DotSizeProperty =
            DependencyProperty.Register("DotSize", typeof(double), typeof(SmoothPageIndicator),
                new PropertyMetadata(8.0, OnLayoutPropertyChanged));

        public double Spacing
        {
            get { return (double)GetValue(SpacingProperty); }
            set { SetValue(SpacingProperty, value); }
        }
        public static readonly DependencyProperty SpacingProperty =
            DependencyProperty.Register("Spacing", typeof(double), typeof(SmoothPageIndicator),
                new PropertyMetadata(12.0, OnLayoutPropertyChanged));

        public Brush ActiveBrush
        {
            get { return (Brush)GetValue(ActiveBrushProperty); }
            set { SetValue(ActiveBrushProperty, value); }
        }
        public static readonly DependencyProperty ActiveBrushProperty =
            DependencyProperty.Register("ActiveBrush", typeof(Brush), typeof(SmoothPageIndicator),
                new PropertyMetadata(Brushes.White, (d, e) => ((SmoothPageIndicator)d).UpdateColors()));

        public Brush InactiveBrush
        {
            get { return (Brush)GetValue(InactiveBrushProperty); }
            set { SetValue(InactiveBrushProperty, value); }
        }
        public static readonly DependencyProperty InactiveBrushProperty =
            DependencyProperty.Register("InactiveBrush", typeof(Brush), typeof(SmoothPageIndicator),
                new PropertyMetadata(new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)), (d, e) => ((SmoothPageIndicator)d).UpdateColors()));


        public SmoothPageIndicator()
        {
            // 这种轻量控件不需要复杂的 Template，直接通过代码构建 Visual Tree
            // 但为了规范，还是建议走 Template 流程，这里为了省事直接构造
            this.HorizontalAlignment = HorizontalAlignment.Center;
            this.VerticalAlignment = VerticalAlignment.Bottom;
            this.IsHitTestVisible = false; // 指示器通常不响应点击，让位于下层的 ScrollView
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            // 如果你在 XAML 里写了 Template 可以在这里找，
            // 但作为 Hardcore 模式，我们直接创建一个 Canvas 作为 Content
            _rootCanvas = new Canvas { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            this.Content = _rootCanvas;
            RebuildVisuals();
        }

        // 当布局类属性 (Count, Size, Spacing) 变化时，重建整个 UI
        private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SmoothPageIndicator)d).RebuildVisuals();
        }

        // 当滚动发生时，只更新 ActiveWorm 的位置 (High Performance)
        private static void OnOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SmoothPageIndicator)d).UpdateWormPosition();
        }

        private void UpdateColors()
        {
            if (_activeWorm != null) _activeWorm.Fill = ActiveBrush;
            foreach (var dot in _inactiveDots) dot.Fill = InactiveBrush;
        }

        /// <summary>
        /// 重建静态背景点和动态游标
        /// </summary>
        private void RebuildVisuals()
        {
            if (_rootCanvas == null || PageCount <= 0) return;

            _rootCanvas.Children.Clear();
            _inactiveDots.Clear();

            double totalWidth = (PageCount * DotSize) + ((PageCount - 1) * Spacing);
            double startX = 0; // Canvas 内部坐标

            // 1. 设置 Canvas 自身大小，确保在父容器中居中
            _rootCanvas.Width = totalWidth;
            _rootCanvas.Height = DotSize;

            // 2. 生成静态点
            for (int i = 0; i < PageCount; i++)
            {
                var dot = new Ellipse
                {
                    Width = DotSize,
                    Height = DotSize,
                    Fill = InactiveBrush
                };
                Canvas.SetLeft(dot, startX + i * (DotSize + Spacing));
                Canvas.SetTop(dot, 0);

                _rootCanvas.Children.Add(dot);
                _inactiveDots.Add(dot);
            }

            // 3. 生成动态游标 (Worm)
            _activeWorm = new Rectangle
            {
                Height = DotSize,
                Width = DotSize, // 初始宽度
                RadiusX = DotSize / 2, // 完全圆角
                RadiusY = DotSize / 2,
                Fill = ActiveBrush
            };

            // 放在最上层
            _rootCanvas.Children.Add(_activeWorm);

            // 立即计算一次位置
            UpdateWormPosition();
        }

        /// <summary>
        /// 核心物理逻辑：计算游标的位置和形变
        /// </summary>
        private void UpdateWormPosition()
        {
            if (_activeWorm == null || PageCount <= 0 || PageSize <= 0) return;

            // 1. 计算当前的浮点页码 (例如 1.5)
            // Offset 通常是负数 (因为内容向左滚)，取反
            double rawProgress = -CurrentOffset / PageSize;

            // 钳制范围，防止过度拉伸
            double progress = Math.Clamp(rawProgress, 0, PageCount - 1);

            int currentIndex = (int)Math.Floor(progress);
            double percent = progress - currentIndex; // 当前页的进度 (0.0 ~ 1.0)

            // 基础步长
            double step = DotSize + Spacing;

            // 2. 计算 Left (位置)
            // 简单的线性移动： CurrentIndex * Step + Percent * Step
            double left = progress * step;

            // 3. 计算 Width (Worm Effect)
            // 这是一个钟形曲线，在 percent = 0.5 时达到最大，0 和 1 时为 0
            // 使用 Sin(0~PI)
            double stretchFactor = Math.Sin(percent * Math.PI);

            // 最大拉伸长度：比如拉伸到 2 倍 DotSize，或者填满间隙 Spacing
            // 这里设定为：填满半个间隙带来的视觉连接感
            double maxStretch = Spacing * 0.8;

            double currentWidth = DotSize + (maxStretch * stretchFactor);

            // 修正 Left：因为宽度增加了，如果 Left 只按线性算，游标中心会偏。
            // 实际上 Worm 效果是：头先走，尾巴后走。
            // 我们可以简单地修正一下 Left，让它在伸长时向左微调一点点，或者不做微调直接线性。
            // 为了“头先动”的效果，我们可以搞个非线性函数，但这里先用居中扩展：
            // 如果要完美模拟 iOS，需要分别计算 HeadPosition 和 TailPosition。

            // 简单优化版：保持线性 Left，只改 Width，看起来就是向右伸长（因为 Left 是左边缘）。
            // 如果想要中心对称伸缩：
            // double center = progress * step + DotSize / 2;
            // left = center - currentWidth / 2;

            // 希沃大屏版（更稳重）：
            // 只要 Left 线性移动，Width 变大，效果就是“变长了然后收缩”。

            _activeWorm.Width = currentWidth;
            Canvas.SetLeft(_activeWorm, left);
        }
    }
}