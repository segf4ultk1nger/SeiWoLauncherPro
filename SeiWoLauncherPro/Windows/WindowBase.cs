using System.Collections;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;

namespace SeiWoLauncherPro {
    public abstract class WindowBase : Window {
        // 配置项
        public bool IsIgnoreShowDesktop { get; set; } = true;
        public bool IsHideFromAltTab { get; set; } = true;
        public bool IsNoActivate { get; set; } = true;
        public bool IsUseWindowChromeTransparency { get; set; } = true;
        public bool IsBottomMost { get; set; } = true;
        public bool UseClearTouch { get; set; } = true;

        protected WindowBase() {
            // 初始化 Build 钩子
            Content = Build();

            if (IsUseWindowChromeTransparency) {
                ApplyWindowChromeTransparent();
            }

            if (UseClearTouch)  {
                ApplyClearTouch();
            }
        }

        protected abstract FrameworkElement Build();

        private void ApplyWindowChromeTransparent() {
            this.WindowStyle = WindowStyle.None;
            this.ResizeMode = ResizeMode.NoResize;
            this.Background = Brushes.Transparent;
            WindowChrome.SetWindowChrome(this, new WindowChrome {
                // 让客户区覆盖整个窗口，配合 -1 实现真正的透明效果
                GlassFrameThickness = new Thickness(-1),
                CaptionHeight = 0,
                UseAeroCaptionButtons = false,
                ResizeBorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(0),
                NonClientFrameEdges = NonClientFrameEdges.None,
            });
        }

        private void ApplyClearTouch() {
            Stylus.SetIsFlicksEnabled(this, false);
            Stylus.SetIsPressAndHoldEnabled(this, false);
            Stylus.SetIsTapFeedbackEnabled(this, false);
            Stylus.SetIsTouchFeedbackEnabled(this, false);
        }

        protected override void OnSourceInitialized(EventArgs e) {
            base.OnSourceInitialized(e);
            var handle = new WindowInteropHelper(this).Handle;

            // 1. 原有的 ExStyle 处理 (Alt+Tab, NoActivate)
            int exStyle = Win32Methods.GetWindowLong(handle, Win32Methods.GWL_EX_STYLE);
            if (IsHideFromAltTab) exStyle = (exStyle | Win32Methods.WS_EX_TOOLWINDOW) & ~Win32Methods.WS_EX_APPWINDOW;
            if (IsNoActivate) exStyle |= Win32Methods.WS_EX_NOACTIVATE;
            Win32Methods.SetWindowLong(handle, Win32Methods.GWL_EX_STYLE, exStyle);

            // 2. 原有的 Win+D 处理
            if (IsIgnoreShowDesktop) SetAsDesktopChild(handle);

            // 3. 注入 Z-Order 守护钩子
            if (IsBottomMost) {
                HwndSource? source = HwndSource.FromHwnd(handle);
                if (source != null) {
                    source.AddHook(WndProc);
                }

                // 初始化时先置底一次
                SendToBottom(handle);
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
            // 捕获 WM_WINDOWPOSCHANGING 消息
            // 当系统或其他程序尝试修改窗口层级时，拦截并强制设为 HWND_BOTTOM
            if (msg == Win32Methods.WM_WINDOWPOSCHANGING) {
                SendToBottom(hwnd);
            }
            return IntPtr.Zero;
        }

        private void SendToBottom(IntPtr handle) {
            Win32Methods.SetWindowPos(handle, Win32Methods.HWND_BOTTOM, 0, 0, 0, 0,
                Win32Methods.SWP_NOSIZE | Win32Methods.SWP_NOMOVE | Win32Methods.SWP_NOACTIVATE);
        }

        private void SetAsDesktopChild(IntPtr handle) {
            ArrayList windowHandles = new();
            Win32Methods.EnumWindows((h, list) => {
                list.Add(h);
                return true;
            }, windowHandles);

            foreach (IntPtr h in windowHandles) {
                IntPtr hNextWin = Win32Methods.FindWindowEx(h, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (hNextWin != IntPtr.Zero) {
                    // 设置 Owner 为桌面的底层壳，这样 Win+D 就切不走它
                    new WindowInteropHelper(this).Owner = hNextWin;
                    break;
                }
            }
        }
    }
}