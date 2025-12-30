using System;
using System.Drawing; // NuGet: System.Drawing.Common
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
// 明确引用，防止冲突
using WinPixelFormat = System.Drawing.Imaging.PixelFormat;

// TODO：清理代码，优化代码结构，清理一下AI写的口水话注释和神笔代码。

namespace SeiWoLauncherPro.Utils
{
    public class DesktopBlurProvider : IDisposable
    {
        private readonly Window _hostWindow;
        private readonly DispatcherTimer _updateTimer;
        private ImageBrush _blurredBrush;
        private TranslateTransform _brushTransform;

        private FrameworkElement _targetElement;

        private double _dpiScaleX = 1.0;
        private double _dpiScaleY = 1.0;

        private bool _disposedValue;

        public ImageBrush BlurredBackgroundBrush => _blurredBrush;

        public DesktopBlurProvider(Window hostWindow)
        {
            _hostWindow = hostWindow;
            _targetElement = hostWindow; // 默认目标

            _brushTransform = new TranslateTransform();
            _blurredBrush = new ImageBrush
            {
                Stretch = Stretch.Fill,
                TileMode = TileMode.None,
                ViewportUnits = BrushMappingMode.Absolute,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top,
                Transform = _brushTransform,
                Opacity = 1.0
            };

            // 既然我们在 CPU 端已经把模糊算好了，这里用 HighQuality 仅仅是为了
            // 将缩小的图拉伸时保持平滑，不再依赖 GPU 的 BlurEffect
            RenderOptions.SetBitmapScalingMode(_blurredBrush, BitmapScalingMode.HighQuality);

            _hostWindow.Loaded += OnWindowLoaded;
            _hostWindow.LocationChanged += OnWindowLocationChanged;
            _hostWindow.Closed += OnWindowClosed;

            // 3秒刷新一次背景
            _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _updateTimer.Tick += (s, e) => CaptureAndBlur();
        }

        public void SetTargetElement(FrameworkElement element)
        {
            _targetElement = element ?? _hostWindow;
            // 既然改用 CPU 模糊，这里就不再自动添加 BlurEffect 了
            // 保持纯净，内容和背景分离
            UpdateBrushAlignment();
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            var source = PresentationSource.FromVisual(_hostWindow);
            if (source != null && source.CompositionTarget != null)
            {
                _dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                _dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
            }

            CaptureAndBlur();
            _updateTimer.Start();
            UpdateBrushAlignment();
        }

        private void OnWindowLocationChanged(object sender, EventArgs e)
        {
            UpdateBrushAlignment();
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            Dispose();
        }

        private void UpdateBrushAlignment()
        {
            if (_hostWindow == null || _targetElement == null) return;
            if (!_targetElement.IsLoaded) return;

            try
            {
                // --- 终极定位修复逻辑 ---

                // 1. 获取 Border 左上角相对于【屏幕物理原点】的坐标
                // PointToScreen 会自动累加：Screen位置 + Window位置 + Window边框 + Border Margin + Border Padding
                // 这是最准确的物理坐标。
                Point screenPointPx = _targetElement.PointToScreen(new Point(0, 0));

                // 2. 物理坐标 -> 逻辑坐标
                // ImageBrush 的 Transform 是基于 WPF 逻辑单位的。
                // 如果 DPI 是 150% (1.5)，物理像素 150 对应逻辑像素 100。
                double offsetXLog = screenPointPx.X / _dpiScaleX;
                double offsetYLog = screenPointPx.Y / _dpiScaleY;

                // 3. 反向平移
                // 我们的背景图是铺满整个屏幕的(0,0)，现在 Border 跑到了 (100, 100)。
                // 为了让背景图看起来没动，我们需要把背景图往回拉 100。
                _brushTransform.X = -offsetXLog;
                _brushTransform.Y = -offsetYLog;
            }
            catch (InvalidOperationException)
            {
                // 忽略 Visual 未连接的情况
            }
        }

        private void CaptureAndBlur()
        {
            if (_disposedValue) return;

            try
            {
                // 获取主屏逻辑尺寸 (用于 Viewport)
                double screenWidthLog = SystemParameters.PrimaryScreenWidth;
                double screenHeightLog = SystemParameters.PrimaryScreenHeight;

                // 获取主屏物理尺寸 (用于截图和 Bitmap)
                int screenWidthPx = (int)(screenWidthLog * _dpiScaleX);
                int screenHeightPx = (int)(screenHeightLog * _dpiScaleY);

                if (screenWidthPx <= 0 || screenHeightPx <= 0) return;

                IntPtr hWallpaper = GetWallpaperWindow();

                using (var bitmap = new Bitmap(screenWidthPx, screenHeightPx, WinPixelFormat.Format32bppArgb))
                {
                    bool success = false;
                    if (hWallpaper != IntPtr.Zero)
                    {
                        using (Graphics g = Graphics.FromImage(bitmap))
                        {
                            IntPtr hdc = g.GetHdc();
                            try { success = PrintWindow(hWallpaper, hdc, 0); }
                            finally { g.ReleaseHdc(hdc); }
                        }
                    }

                    if (!success)
                    {
                        using (Graphics g = Graphics.FromImage(bitmap))
                        {
                            g.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(screenWidthPx, screenHeightPx));
                        }
                    }

                    // --- 1. Downscaling (CPU 降采样) ---
                    // 缩小比例：1/12。缩小后图片很小，处理速度极快。
                    int miniWidth = Math.Max(1, screenWidthPx / 12);
                    int miniHeight = Math.Max(1, screenHeightPx / 12);

                    using (var miniBmp = new Bitmap(miniWidth, miniHeight, WinPixelFormat.Format32bppArgb))
                    {
                        using (var g = Graphics.FromImage(miniBmp))
                        {
                            // 使用高质量双线性插值
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                            g.DrawImage(bitmap, 0, 0, miniWidth, miniHeight);
                        }

                        // --- 2. Unsafe CPU Blur (核心修改) ---
                        // 应用 StackBlur 算法。半径 5 在小图上相当于原图的 Radius 60，效果很强。
                        UnsafeStackBlur.Apply(miniBmp, 5);

                        var imageSource = ToBitmapSource(miniBmp);
                        imageSource.Freeze();

                        _blurredBrush.ImageSource = imageSource;

                        // --- 3. Viewport 对齐 ---
                        // 强制使用逻辑屏幕尺寸，确保 ImageBrush 1:1 覆盖桌面
                        _blurredBrush.Viewport = new Rect(0, 0, screenWidthLog, screenHeightLog);
                    }
                }
                UpdateBrushAlignment();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Capture Error: {ex.Message}");
            }
        }

        // --- Win32 API ---
        // TODO：把这里的代码给迁移到 Win32NativeMethods.cs 去。
        private IntPtr GetWallpaperWindow()
        {
            IntPtr progman = FindWindow("Progman", null);
            SendMessageTimeout(progman, 0x052C, new IntPtr(0), IntPtr.Zero, 0x0, 1000, out var result);
            IntPtr workerw = IntPtr.Zero;
            EnumWindows((hwnd, lParam) =>
            {
                IntPtr p = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (p != IntPtr.Zero) workerw = FindWindowEx(IntPtr.Zero, hwnd, "WorkerW", null);
                return true;
            }, IntPtr.Zero);
            return workerw != IntPtr.Zero ? workerw : progman;
        }

        [DllImport("user32.dll")] static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", SetLastError = true)] static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);
        [DllImport("user32.dll", SetLastError = true)] static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);
        [DllImport("user32.dll", CharSet = CharSet.Auto)] static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);
        [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);
        [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr hObject);

        private BitmapSource ToBitmapSource(Bitmap bitmap)
        {
            IntPtr hBitmap = bitmap.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(hBitmap); }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _updateTimer?.Stop();
                    if (_hostWindow != null)
                    {
                        _hostWindow.Loaded -= OnWindowLoaded;
                        _hostWindow.LocationChanged -= OnWindowLocationChanged;
                        _hostWindow.Closed -= OnWindowClosed;
                    }
                    _blurredBrush = null;
                    _targetElement = null;
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    // =========================================================
    // Unsafe 高性能模糊算法 (StackBlur 近似高斯)
    // 直接操作内存指针，速度极快，无视 C# 安全检查
    // =========================================================
    public static unsafe class UnsafeStackBlur
    {
        public static void Apply(Bitmap bmp, int radius)
        {
            if (radius < 1) return;

            int w = bmp.Width;
            int h = bmp.Height;

            // 锁定内存，获取指针
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, w, h),
                ImageLockMode.ReadWrite, WinPixelFormat.Format32bppArgb);

            try
            {
                // 获取首地址指针
                byte* ptr = (byte*)data.Scan0;
                int stride = data.Stride;

                // 为了代码简洁，这里实现一个 3-Pass Box Blur (近似高斯)
                // 真正的 StackBlur 代码量太大，Box Blur 多跑几次效果几乎一样，且代码更短

                // 分配临时缓冲区
                int byteCount = stride * h;
                byte[] tempBuffer = new byte[byteCount];

                fixed (byte* tempPtr = tempBuffer)
                {
                    // 迭代 3 次，模拟高斯平滑
                    for (int i = 0; i < 3; i++)
                    {
                        BoxBlurH(ptr, tempPtr, w, h, stride, radius);
                        BoxBlurV(tempPtr, ptr, w, h, stride, radius);
                    }
                }
            }
            finally
            {
                bmp.UnlockBits(data);
            }
        }

        // 水平方向模糊 (Unsafe Pointer Version)
        private static void BoxBlurH(byte* src, byte* dest, int w, int h, int stride, int r)
        {
            // 这里的 alpha 通道(A)通常在 byte+3 的位置
            // B G R A

            // 并行优化 (Parallel For)
            System.Threading.Tasks.Parallel.For(0, h, y =>
            {
                byte* rowSrc = src + y * stride;
                byte* rowDest = dest + y * stride;

                for (int x = 0; x < w; x++)
                {
                    long bSum = 0, gSum = 0, rSum = 0;
                    int count = 0;

                    // 简单滑动窗口
                    int left = x - r;
                    if (left < 0) left = 0;
                    int right = x + r;
                    if (right >= w) right = w - 1;

                    for (int k = left; k <= right; k++)
                    {
                        // 32bpp: 4 bytes per pixel
                        byte* px = rowSrc + k * 4;
                        bSum += px[0];
                        gSum += px[1];
                        rSum += px[2];
                        count++;
                    }

                    byte* dPx = rowDest + x * 4;
                    dPx[0] = (byte)(bSum / count);
                    dPx[1] = (byte)(gSum / count);
                    dPx[2] = (byte)(rSum / count);
                    dPx[3] = 255; // Alpha 填满，防止透明
                }
            });
        }

        // 垂直方向模糊 (Unsafe Pointer Version)
        private static void BoxBlurV(byte* src, byte* dest, int w, int h, int stride, int r)
        {
            System.Threading.Tasks.Parallel.For(0, w, x =>
            {
                for (int y = 0; y < h; y++)
                {
                    long bSum = 0, gSum = 0, rSum = 0;
                    int count = 0;

                    int top = y - r;
                    if (top < 0) top = 0;
                    int bottom = y + r;
                    if (bottom >= h) bottom = h - 1;

                    for (int k = top; k <= bottom; k++)
                    {
                        byte* px = src + k * stride + x * 4;
                        bSum += px[0];
                        gSum += px[1];
                        rSum += px[2];
                        count++;
                    }

                    byte* dPx = dest + y * stride + x * 4;
                    dPx[0] = (byte)(bSum / count);
                    dPx[1] = (byte)(gSum / count);
                    dPx[2] = (byte)(rSum / count);
                    dPx[3] = 255;
                }
            });
        }
    }
}