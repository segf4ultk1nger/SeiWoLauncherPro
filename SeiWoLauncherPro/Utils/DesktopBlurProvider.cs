using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices; // 必须加上这个
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SeiWoLauncherPro.Utils {
    public class DesktopBlurProvider
    {
        // 必须导入这个来修复内存泄漏
        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        private static readonly Lazy<DesktopBlurProvider> _instance = new(() => new DesktopBlurProvider());
        public static DesktopBlurProvider Instance => _instance.Value;

        public ImageBrush BlurredBrush { get; private set; }

        // 记录原始分辨率，用于 UI 层计算 Viewbox 缩放
        public double SourceWidth { get; private set; }
        public double SourceHeight { get; private set; }

        public void RefreshSnapshot()
        {
            IntPtr hwnd = NativeWindowHelper.GetWallpaperWindow();
            if (!Win32Methods.GetWindowRect(hwnd, out Win32Methods.RECT rect)) return;

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            if (width <= 0 || height <= 0) return;

            this.SourceWidth = width;
            this.SourceHeight = height;

            // 使用 using 确保 rawBitmap 及时回收
            using (Bitmap raw = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb))
            {
                using (Graphics g = Graphics.FromImage(raw))
                {
                    IntPtr hdc = g.GetHdc();
                    try {
                        Win32Methods.PrintWindow(hwnd, hdc, Win32Methods.PW_RENDERFULLCONTENT);
                    }
                    finally {
                        g.ReleaseHdc(hdc);
                    }
                }

                // 2. 下采样（推荐 4 倍，性能平衡点）
                int scale = 4;
                int sw = width / scale;
                int sh = height / scale;

                using (Bitmap small = new Bitmap(sw, sh, System.Drawing.Imaging.PixelFormat.Format32bppPArgb))
                {
                    using (Graphics gSmall = Graphics.FromImage(small))
                    {
                        gSmall.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
                        gSmall.DrawImage(raw, 0, 0, sw, sh);
                    }

                    // 3. 执行模糊 (内部应自理 LockBits)
                    UnsafeBlur.ApplyStackBlur(small, 5);

                    // 4. 转为 WPF 资源
                    IntPtr hBitmap = small.GetHbitmap();
                    try
                    {
                        var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                            hBitmap,
                            IntPtr.Zero,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());

                        bitmapSource.Freeze();

                        // 5. 创建共享画笔
                        var brush = new ImageBrush(bitmapSource)
                        {
                            // 这里关键：因为小图最终会被拉伸显示，
                            // 设置为 Absolute 意味着 Viewbox 的数值直接对应屏幕像素坐标
                            ViewportUnits = BrushMappingMode.Absolute,
                            ViewboxUnits = BrushMappingMode.Absolute,
                            TileMode = TileMode.None,
                            Stretch = Stretch.Fill
                        };
                        brush.Freeze();

                        this.BlurredBrush = brush;
                    }
                    finally
                    {
                        // 记得干掉这个非托管句柄！
                        DeleteObject(hBitmap);
                    }
                }
            } // raw 自动释放
        }
    }
}