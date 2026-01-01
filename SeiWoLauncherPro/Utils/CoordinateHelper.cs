using SeiWoLauncherPro;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace SeiWoLauncherPro.Utils
{
    public static class CoordinateHelper
    {
        /// <summary>
        /// 获取控件 (Visual) 相对于其祖先窗口 (Window) 的坐标。
        /// 这里的输出是 WPF 逻辑单位。
        /// </summary>
        public static Point GetRelativeToWindow(UIElement element) // 这里从 Visual 改为 UIElement
        {
            var window = Window.GetWindow(element);
            if (window == null) return new Point(0, 0);

            // 现在 IsVisible 正常工作了
            if (!element.IsVisible) return new Point(0, 0);

            try
            {
                // 如果你担心某些奇葩情况，可以加一个 IsLoaded 检查
                if (!element.IsMeasureValid) return new Point(0, 0);

                return element.TransformToAncestor(window).Transform(new Point(0, 0));
            }
            catch (Exception)
            {
                return new Point(0, 0);
            }
        }

        /// <summary>
        /// 获取窗口相对于屏幕左上角的物理像素坐标。
        /// 这里处理了 DPI 缩放，直接对标 BitBlt 使用的物理坐标。
        /// </summary>
        public static Rect GetWindowScreenRect(Window window)
        {
            var dpi = VisualTreeHelper.GetDpi(window);

            // PointToScreen 返回的是物理像素坐标
            Point screenPos = window.PointToScreen(new Point(0, 0));

            return new Rect(
                screenPos.X,
                screenPos.Y,
                window.ActualWidth * dpi.DpiScaleX,
                window.ActualHeight * dpi.DpiScaleY
            );
        }

        public static Rect GetRelativeRectToWallpaper(Window window)
        {
            IntPtr windowHandle = new WindowInteropHelper(window).Handle;
            IntPtr wallpaperHandle = NativeWindowHelper.GetWallpaperWindow();

            Win32Methods.RECT windowRect;
            Win32Methods.GetWindowRect(windowHandle, out windowRect);

            Win32Methods.RECT wallpaperRect;
            Win32Methods.GetWindowRect(wallpaperHandle, out wallpaperRect);

            // 计算相对偏移：窗口坐标 - 壁纸窗口坐标
            return new Rect(
                windowRect.Left - wallpaperRect.Left,
                windowRect.Top - wallpaperRect.Top,
                windowRect.Right - windowRect.Left,
                windowRect.Bottom - windowRect.Top
            );
        }
    }
}