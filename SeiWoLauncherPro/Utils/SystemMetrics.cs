using System.Runtime.InteropServices;

public static class SystemMetrics
{
    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    public const int SM_CXSCREEN = 0; // 主屏幕宽
    public const int SM_CYSCREEN = 1; // 主屏幕高

    public static (int Width, int Height) GetPrimaryScreenSize()
    {
        return (GetSystemMetrics(SM_CXSCREEN), GetSystemMetrics(SM_CYSCREEN));
    }
}