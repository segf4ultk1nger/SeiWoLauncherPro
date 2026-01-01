using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace SeiWoLauncherPro.Utils;

[SupportedOSPlatform("windows")]
public static class UriProtocolRegisterHelper
{
    /// <summary>
    /// 注册协议。
    /// </summary>
    /// <param name="uriScheme">协议名称，例如 "myapp"</param>
    public static void Register(string uriScheme)
    {
        var exePath = Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrEmpty(exePath)) return;

        string rootPath = $@"Software\Classes\{uriScheme}";
        string commandPath = $@"{rootPath}\shell\open\command";
        string commandValue = $"\"{exePath}\" --uri \"%1\"";

        // 1. 创建并设置 Root Key (URL Protocol)
        if (Win32Methods.RegCreateKeyEx(Win32Methods.HKEY_CURRENT_USER, rootPath, 0, null, 0, Win32Methods.KEY_WRITE, IntPtr.Zero, out IntPtr hRootKey, out _) == Win32Methods.ERROR_SUCCESS)
        {
            SetStringValue(hRootKey, "URL Protocol", "");
            Win32Methods.RegCloseKey(hRootKey);
        }

        // 2. 创建并设置 Command Key
        if (Win32Methods.RegCreateKeyEx(Win32Methods.HKEY_CURRENT_USER, commandPath, 0, null, 0, Win32Methods.KEY_WRITE, IntPtr.Zero, out IntPtr hCommandKey, out _) == Win32Methods.ERROR_SUCCESS)
        {
            SetStringValue(hCommandKey, null, commandValue); // null 或 "" 代表 (Default)
            Win32Methods.RegCloseKey(hCommandKey);
        }
    }

    /// <summary>
    /// 卸载协议。
    /// </summary>
    public static void UnRegister(string uriScheme)
    {
        Win32Methods.RegDeleteTree(Win32Methods.HKEY_CURRENT_USER, $@"Software\Classes\{uriScheme}");
    }

    /// <summary>
    /// 检查协议是否已注册。
    /// </summary>
    public static bool IsRegistered(string uriScheme)
    {
        int result = Win32Methods.RegOpenKeyEx(Win32Methods.HKEY_CURRENT_USER, $@"Software\Classes\{uriScheme}", 0, Win32Methods.KEY_READ, out IntPtr hKey);
        if (result == Win32Methods.ERROR_SUCCESS)
        {
            Win32Methods.RegCloseKey(hKey);
            return true;
        }
        return false;
    }

    // --- 私有辅助方法 ---

    private static void SetStringValue(IntPtr hKey, string? name, string value)
    {
        byte[] data = Encoding.Unicode.GetBytes(value + "\0");
        Win32Methods.RegSetValueEx(hKey, name, 0, Win32Methods.REG_SZ, data, data.Length);
    }
}