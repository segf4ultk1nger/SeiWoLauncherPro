using System.Drawing;
using System.Runtime.InteropServices;

namespace SeiWoLauncherPro.Utils
{
    public struct MonitorRect
    {
        public int Left, Top, Right, Bottom;
        public int Width => Right - Left;
        public int Height => Bottom - Top;

        // 重点：添加隐式转换
        // 这样当你传 MonitorRect 给需要 Rectangle 的方法时，C# 会自动帮你转
        public static implicit operator Rectangle(MonitorRect rect)
        {
            return new Rectangle(rect.Left, rect.Top, rect.Width, rect.Height);
        }
    }

    public static class MonitorHelper
    {
        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        private delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref MonitorRect lprcMonitor, IntPtr dwData);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MONITORINFO
        {
            public int cbSize;
            public MonitorRect rcMonitor;
            public MonitorRect rcWork;
            public uint dwFlags;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        public static List<MonitorRect> GetMonitors()
        {
            var monitors = new List<MonitorRect>();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref MonitorRect lprcMonitor, IntPtr dwData) =>
            {
                MONITORINFO mi = new MONITORINFO();
                mi.cbSize = Marshal.SizeOf(mi);
                if (GetMonitorInfo(hMonitor, ref mi))
                {
                    monitors.Add(mi.rcMonitor);
                }
                return true;
            }, IntPtr.Zero);
            return monitors;
        }

        public static MonitorRect PrimaryScreenBounds
        {
            get
            {
                // 0x00000001 是 MONITOR_DEFAULTTOPRIMARY
                IntPtr hMonitor = MonitorFromWindow(IntPtr.Zero, 1);
                MONITORINFO mi = new MONITORINFO();
                mi.cbSize = Marshal.SizeOf(mi);
                GetMonitorInfo(hMonitor, ref mi);
                return mi.rcMonitor;
            }
        }
    }
}