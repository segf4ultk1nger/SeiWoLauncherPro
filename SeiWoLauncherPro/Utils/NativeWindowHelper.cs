using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SeiWoLauncherPro.Utils {
    public class NativeWindowHelper {
        /// <summary>
        /// 获取真正的壁纸窗口句柄 (兼容 WorkerW 机制)
        /// </summary>
        public static IntPtr GetWallpaperWindow() {
            IntPtr progman = Win32Methods.FindWindow("Progman", null);
            // 发送消息触发系统生成 WorkerW
            Win32Methods.SendMessageTimeout(progman, 0x052C, new IntPtr(0), IntPtr.Zero, 0x0, 1000, out _);

            IntPtr workerw = IntPtr.Zero;
            Win32Methods.EnumWindows((IntPtr, lParam) => {
                IntPtr p = Win32Methods.FindWindowEx(IntPtr, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (p != IntPtr.Zero) {
                    workerw = Win32Methods.FindWindowEx(IntPtr.Zero, IntPtr, "WorkerW", null);
                }

                return true;
            }, new ArrayList());

            return workerw != IntPtr.Zero ? workerw : progman;
        }

        public record WindowInfo(nint IntPtr, string ClassName, string Title);

        /// <summary>
        /// 检查文件是否被占用（锁定）
        /// </summary>
        public static bool IsFileLocked(string filePath) {
            if (!File.Exists(filePath)) return false;

            try {
                // 尝试以独占方式打开文件
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                return false;
            } catch (IOException) {
                return true;
            }
        }

        /// <summary>
        /// 等待文件解除占用
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="timeoutMs">超时时间（毫秒），默认 10秒。设为 -1 则无限等待</param>
        /// <exception cref="TimeoutException">如果超过指定时间文件仍被占用</exception>
        public static void WaitForFile(string path, int timeoutMs = 10000) {
            var stopwatch = Stopwatch.StartNew();

            while (IsFileLocked(path)) {
                if (timeoutMs != -1 && stopwatch.ElapsedMilliseconds > timeoutMs) {
                    throw new TimeoutException($"等待文件释放超时: {path}");
                }

                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// 将 int 格式的 ARGB 转换为 Color 对象
        /// </summary>
        public static Color GetColor(int argb) {
            // 优化：使用位运算直接提取，去除多余的类型转换
            return Color.FromArgb(
                (byte)(argb >> 24),
                (byte)(argb >> 16),
                (byte)(argb >> 8),
                (byte)argb
            );
        }

        /// <summary>
        /// 通过类名查找窗口句柄
        /// </summary>
        public static nint FindWindowByClass(string className) {
            // 性能优化：如果只需要找一个特定窗口，遍历所有窗口开销较大。
            // 这里优先尝试直接用 FindWindow API，找不到再遍历（作为兜底）。
            var directFind = Win32Methods.FindWindow(className, null);
            if (directFind != IntPtr.Zero) return directFind;

            // 如果需要模糊匹配或遍历查找：
            var windows = GetAllWindows();
            var target = windows.FirstOrDefault(x => x.ClassName == className);
            return target?.IntPtr ?? IntPtr.Zero;
        }

        /// <summary>
        /// 获取所有窗口的信息 (使用 BFS 广度优先遍历)
        /// </summary>
        public static List<WindowInfo> GetAllWindows() {
            var windows = new List<WindowInfo>();
            var queue = new Queue<IntPtr>();

            queue.Enqueue(IntPtr.Zero);

            while (queue.Count > 0) {
                var parent = queue.Dequeue();

                // 获取父窗口下的第一个子窗口
                var currentWin = Win32Methods.FindWindowEx(parent, IntPtr.Zero, null, null);

                while (currentWin != IntPtr.Zero) {
                    // 1. 获取当前窗口信息
                    try {
                        var className = GetWindowClassName(currentWin);
                        var title = GetWindowTitle(currentWin);
                        windows.Add(new WindowInfo(currentWin, className, title));
                    } catch (Exception ex) {
                        // 替换了原本的 Logger，改用 Debug 输出，防止崩溃
                        Debug.WriteLine($"[NativeWindowHelper] 无法获取窗口信息 {currentWin}: {ex.Message}");
                    }

                    // 2. 检查当前窗口是否有子窗口 (如果有，加入队列待会儿遍历其子节点)
                    var childCheck = Win32Methods.FindWindowEx(currentWin, IntPtr.Zero, null, null);
                    if (childCheck != IntPtr.Zero) {
                        queue.Enqueue(currentWin);
                    }

                    // 3. 继续查找当前层级的下一个兄弟窗口
                    currentWin = Win32Methods.FindWindowEx(parent, currentWin, null, null);
                }
            }

            return windows;
        }

        private static string GetWindowClassName(IntPtr hWnd) {
            const int maxChars = 256;
            // 分配初始容量
            StringBuilder sb = new StringBuilder(maxChars);
            int length = Win32Methods.GetClassName(hWnd, sb, maxChars);

            // StringBuilder 会自动根据返回的实际长度截断
            return length > 0 ? sb.ToString() : string.Empty;
        }

        private static string GetWindowTitle(IntPtr hWnd) {
            const int maxChars = 256;
            StringBuilder sb = new StringBuilder(maxChars);
            int length = Win32Methods.GetWindowText(hWnd, sb, maxChars);

            return length > 0 ? sb.ToString() : string.Empty;
        }
    }
}