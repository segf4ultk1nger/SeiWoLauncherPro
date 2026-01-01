using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace SeiWoLauncherPro.Utils; // 请根据项目修改命名空间

[SupportedOSPlatform("windows")]
public sealed class RegistryNotifier : IDisposable
{

    private IntPtr _registryKeyHandle = IntPtr.Zero;
    private CancellationTokenSource? _cts;
    private bool _disposed;

    public event EventHandler? RegistryKeyUpdated;

    /// <summary>
    /// 初始化注册表监听器
    /// </summary>
    /// <param name="root">根键 (如 RegistryNotifier.HKEY_CURRENT_USER)</param>
    /// <param name="path">子键路径</param>
    public RegistryNotifier(uint root, string path)
    {
        int result = Win32Methods.RegOpenKeyEx(root, path, 0, Win32Methods.KEY_READ, out _registryKeyHandle);
        if (result != 0)
        {
            throw new System.ComponentModel.Win32Exception(result, $"无法打开注册表键: {path}");
        }
    }

    /// <summary>
    /// 开始监听
    /// </summary>
    public void Start()
    {
        if (_cts != null) return;

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        Task.Run(() => MonitorLoop(token), token);
    }

    /// <summary>
    /// 停止监听
    /// </summary>
    public void Stop()
    {
        _cts?.Cancel();
        _cts = null;
    }

    private void MonitorLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _registryKeyHandle != IntPtr.Zero)
        {
            // 注意：RegNotifyChangeKeyValue 在 fAsynchronus 为 false 时会阻塞当前线程
            // 直到指定的注册表项发生变化
            int result = Win32Methods.RegNotifyChangeKeyValue(
                _registryKeyHandle,
                true,
                Win32Methods.REG_NOTIFY_CHANGE_NAME | Win32Methods.REG_NOTIFY_CHANGE_ATTRIBUTES | Win32Methods.REG_NOTIFY_CHANGE_LAST_SET | Win32Methods.REG_NOTIFY_CHANGE_SECURITY,
                IntPtr.Zero,
                false);

            if (result == 0 && !token.IsCancellationRequested)
            {
                RegistryKeyUpdated?.Invoke(this, EventArgs.Empty);
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss}] Registry Key Changed.");
            }
            else
            {
                // 如果出错或被取消，跳出循环
                break;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        Stop();
        if (_registryKeyHandle != IntPtr.Zero)
        {
            Win32Methods.RegCloseKey(_registryKeyHandle);
            _registryKeyHandle = IntPtr.Zero;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~RegistryNotifier() => Dispose();
}