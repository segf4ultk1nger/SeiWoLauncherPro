using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;

namespace SeiWoLauncherPro.Utils {
    public static class Win32Methods {
        public delegate bool EnumedWindow(IntPtr handleWindow, ArrayList handles);

        public const int GWL_EX_STYLE = -20;
        public const int WS_EX_APPWINDOW = 0x00040000;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_NOACTIVATE = 0x08000000;

        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOACTIVATE = 0x0010;

        public const int WM_WINDOWPOSCHANGING = 0x0046;

        public const int SPI_SETDESKWALLPAPER = 0x0014;
        public const int WM_SETTINGCHANGE = 0x001A;

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 2;

        public const uint PW_RENDERFULLCONTENT = 0x00000002;

        public const uint HKEY_CURRENT_USER = 0x80000001;
        public const int REG_SZ = 1;
        public const uint KEY_WRITE = 0x20006;
        public const uint KEY_READ = 0x20019;
        public const uint CREATE_NEW_KEY = 0x00000001;
        public const int ERROR_SUCCESS = 0;

        public const int REG_NOTIFY_CHANGE_NAME = 0x1;
        public const int REG_NOTIFY_CHANGE_ATTRIBUTES = 0x2;
        public const int REG_NOTIFY_CHANGE_LAST_SET = 0x4;
        public const int REG_NOTIFY_CHANGE_SECURITY = 0x8;

        public const int SWP_NOZORDER = 0x0004;

        public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        public static readonly nint HWND_TOPMOST = new(-1);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int RegCreateKeyEx(uint hKey, string lpSubKey, int Reserved, string? lpClass, uint dwOptions, uint samDesired, IntPtr lpSecurityAttributes, out IntPtr phkResult, out uint lpdwDisposition);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int RegOpenKeyEx(uint hKey, string lpSubKey, uint ulOptions, uint samDesired, out IntPtr phkResult);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int RegSetValueEx(IntPtr hKey, string? lpValueName, int Reserved, int dwType, byte[] lpData, int cbData);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int RegDeleteTree(uint hKey, string lpSubKey);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int RegCloseKey(IntPtr hKey);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int RegNotifyChangeKeyValue(IntPtr hKey, bool bWatchSubtree, int dwNotifyFilter, IntPtr hEvent, bool fAsynchronus);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr hP, IntPtr hC, string sC, string sW);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumedWindow lpEnumFunc, ArrayList lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPOS
        {
            public nint hwnd;
            public nint hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public uint flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }
    }
}