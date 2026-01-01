using Common.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SeiWoLauncherPro.Enums;
using SeiWoLauncherPro.Models;
using SeiWoLauncherPro.Utils;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
// 需引用 System.Drawing.Common
// 需引用 System.Windows.Forms
using Application = System.Windows.Application;
using Color = System.Windows.Media.Color;

// 假设这些 Helper 类你已经有了，或者它们是原来项目里的通用工具类
// using ClassIsland.Core.Helpers.Native;
// using ClassIsland.Helpers;
// 这里的命名空间根据你的实际项目修改

namespace SeiWoLauncherPro.Services;

[SupportedOSPlatform("windows")]
public sealed class WallpaperPickingService : IHostedService, INotifyPropertyChanged, IDisposable {
    private readonly SettingsService<Settings> _settingsService;
    private readonly ILogger<WallpaperPickingService> _logger;
    private readonly IHostApplicationLifetime _appLifetime;

    private static readonly string DesktopWindowClassName = "Progman";
    private ObservableCollection<Color> _wallpaperColorPlatte = new();
    private BitmapImage _wallpaperImage = new();
    private bool _isWorking = false;

    // 假设这是你原来的 RegistryNotifier，保留原样
    public RegistryNotifier RegistryNotifier { get; }

    private DispatcherTimer UpdateTimer { get; } = new() {
        Interval = TimeSpan.FromMinutes(1)
    };

    // 辅助结构，用于优化排序性能
    private readonly struct ColorScore {
        public string Key { get; }
        public int Count { get; }
        public double Score { get; }
        public Color Color { get; }

        public ColorScore(string key, int count, double score, Color color) {
            Key = key;
            Count = count;
            Score = score;
            Color = color;
        }
    }

    public ObservableCollection<Color> WallpaperColorPlatte {
        get => _wallpaperColorPlatte;
        set => SetField(ref _wallpaperColorPlatte, value);
    }

    public BitmapImage WallpaperImage {
        get => _wallpaperImage;
        set => SetField(ref _wallpaperImage, value);
    }

    public bool IsWorking {
        get => _isWorking;
        set => SetField(ref _isWorking, value);
    }

    public WallpaperPickingService(
        SettingsService<Settings> settingsService,
        ILogger<WallpaperPickingService> logger,
        IHostApplicationLifetime appLifetime) {
        _logger = logger;
        _settingsService = settingsService;
        _appLifetime = appLifetime;

        SystemEvents.UserPreferenceChanged += SystemEventsOnUserPreferenceChanged;

        // 保留原有的 RegistryNotifier 逻辑
        RegistryNotifier = new RegistryNotifier(Win32Methods.HKEY_CURRENT_USER, "Control Panel\\Desktop");
        RegistryNotifier.RegistryKeyUpdated += RegistryNotifierOnRegistryKeyUpdated;

        // 使用标准的 Host 生命周期管理
        _appLifetime.ApplicationStopping.Register(() => {
            RegistryNotifier.Stop();
            SystemEvents.UserPreferenceChanged -= SystemEventsOnUserPreferenceChanged;
        });

        RegistryNotifier.Start();

        UpdateTimer.Tick += UpdateTimerOnTick;
        // 假设 TimeSpanHelper 是你原有的工具类
        UpdateTimer.Interval =
            TimeSpanHelper.FromSecondsSafe(_settingsService.Settings.WallpaperAutoUpdateIntervalSeconds);

        _settingsService.Settings.PropertyChanged += SettingsServiceOnPropertyChanged;
        UpdateUpdateTimerEnableState();
    }

    private void SettingsServiceOnPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        switch (e.PropertyName) {
            case nameof(Settings.WallpaperAutoUpdateIntervalSeconds):
                UpdateTimer.Interval =
                    TimeSpanHelper.FromSecondsSafe(_settingsService.Settings.WallpaperAutoUpdateIntervalSeconds);
                break;
            case nameof(Settings.IsWallpaperAutoUpdateEnabled):
            case nameof(Settings.AccentColorSource):
                UpdateUpdateTimerEnableState();
                break;
        }
    }

    private void UpdateUpdateTimerEnableState() {
        if ((_settingsService.Settings.AccentColorSource == SettingsEnums.AccentColorSource.WallpaperAccentColor &&
             _settingsService.Settings.IsWallpaperAutoUpdateEnabled) ||
            _settingsService.Settings.AccentColorSource == SettingsEnums.AccentColorSource.ScreenAccentColor) {
            if (!UpdateTimer.IsEnabled) UpdateTimer.Start();
        } else {
            UpdateTimer.Stop();
        }
    }

    private async void UpdateTimerOnTick(object? sender, EventArgs e) {
        _logger.LogInformation("自动提取主题色Timer触发。");
        await GetWallpaperAsync();
    }

    private async void RegistryNotifierOnRegistryKeyUpdated(object? sender, EventArgs e) {
        _logger.LogInformation("壁纸注册表项更新触发。");
        // 避免在非必要时调用 Application.Current.Dispatcher
        await GetWallpaperAsync();
    }

    private async void SystemEventsOnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e) {
        if (e.Category == UserPreferenceCategory.Desktop) {
            _logger.LogInformation("UserPreferenceChanged事件更新触发。");
            await GetWallpaperAsync();
        }
    }

    #region Native / Win32 Methods (保留原有逻辑)

    private Bitmap? GetFullScreenShot(MonitorRect monitor) {
        try {
            // 这里的 Width 和 Height 是物理像素，非常适合 CopyFromScreen
            var baseImage = new Bitmap(monitor.Width, monitor.Height);
            using (var g = Graphics.FromImage(baseImage)) {
                // 注意：起点坐标要设为 monitor.Left 和 monitor.Top，
                // 否则在多屏幕环境下（比如希沃一体机外接了屏幕）会抓错位置。
                g.CopyFromScreen(monitor.Left, monitor.Top, 0, 0, new Size(monitor.Width, monitor.Height));
            }

            return baseImage;
        } catch (Exception ex) {
            _logger.LogError(ex, "获取屏幕截图失败。");
            return null;
        }
    }

    public static Bitmap? GetScreenShot(string className) {
        // 假设 NativeWindowHelper 和 WindowCaptureHelper 是你项目中保留的类
        var win = NativeWindowHelper.FindWindowByClass(className);
        if (win == IntPtr.Zero) {
            return null;
        }

        return WindowCaptureHelper.CaptureWindowBitBlt(win);
    }

    public Bitmap? GetFallbackWallpaper() {
        _logger.LogInformation("正在以兼容模式获取壁纸。");

        try {
            using var k = Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop");
            var path = (string?)k?.GetValue("WallPaper");
            var b = (Rectangle?)MonitorHelper.PrimaryScreenBounds ?? new Rectangle(0, 0, 1920, 1080);

            if (path == null || !System.IO.File.Exists(path))
                return null;

            // 使用 Image.FromFile 会锁定文件，改用 Stream 方式更安全（可选优化），这里保持原逻辑但增加Dispose
            using var originalImage = Image.FromFile(path);

            var m = 1.0;
            if (originalImage.Width > originalImage.Height) {
                m = 1.0 * b.Height / originalImage.Height;
            } else {
                m = 1.0 * b.Width / originalImage.Width;
            }

            return new Bitmap(originalImage, (int)(originalImage.Width * m), (int)(originalImage.Height * m));
        } catch (Exception ex) {
            _logger.LogError(ex, "以兼容模式获取壁纸失败。");
            return null;
        }
    }

    #endregion

    public async Task GetWallpaperAsync() {
        if (IsWorking) return;

        IsWorking = true;
        _logger.LogInformation("正在提取壁纸主题色。");

        try {
            // 在后台线程运行，避免阻塞 UI
            await Task.Run(() => {
                Bitmap? bitmap = null;
                try {
                    // 获取 Bitmap
                    if (_settingsService.Settings.AccentColorSource ==
                        SettingsEnums.AccentColorSource.ScreenAccentColor) {
                        var screen = MonitorHelper.PrimaryScreenBounds;
                        bitmap = GetFullScreenShot(screen);
                    } else if (_settingsService.Settings.IsAccentColorRequestUsingFallbackMode) {
                        bitmap = GetFallbackWallpaper();
                    } else {
                        var className = DesktopWindowClassName;
                        bitmap = GetScreenShot(className);
                    }

                    if (bitmap is null) {
                        _logger.LogError("获取壁纸失败，Bitmap 为 null。");
                        return;
                    }

                    // 转换图片 (UI 线程操作)
                    Application.Current.Dispatcher.Invoke(() => {
                        // 假设 BitmapConveters 存在，并在转换后 Freeze 对象
                        var wpImage = BitmapConveters.ConvertToBitmapImage(bitmap, bitmap.Width);
                        if (wpImage.CanFreeze) wpImage.Freeze(); // 性能优化：冻结对象
                        WallpaperImage = wpImage;
                    });

                    NewColorPickingImpl(bitmap);
                } finally {
                    bitmap?.Dispose(); // 确保 Bitmap 及时释放
                }
            });

            // 更新 Settings 中的缓存
            // 切回 UI 线程或者使用线程安全的集合操作，这里假设 Settings 集合操作需要注意线程安全
            Application.Current.Dispatcher.Invoke(() => {
                var settings = _settingsService.Settings;
                if (settings.WallpaperColorPalette.Count < settings.SelectedColorPaletteIndex + 1 ||
                    WallpaperColorPlatte.Count < settings.SelectedColorPaletteIndex + 1 ||
                    settings.SelectedColorPaletteIndex < 0 ||
                    !AreColorsEqual(settings.WallpaperColorPalette, WallpaperColorPlatte,
                        settings.SelectedColorPaletteIndex)) {
                    settings.WallpaperColorPalette.Clear();
                    foreach (var i in WallpaperColorPlatte) {
                        settings.WallpaperColorPalette.Add(i);
                    }

                    settings.SelectedColorPaletteIndex = 0;
                }
            });
        } catch (Exception e) {
            _logger.LogError(e, "无法提取壁纸主题色");
        } finally {
            IsWorking = false;
            // 移除显式 GC.Collect()，让 CLR 自动管理
        }
    }

    // 辅助方法：比较颜色列表是否变化
    private bool AreColorsEqual(ObservableCollection<Color> source, ObservableCollection<Color> target, int index) {
        if (index >= source.Count || index >= target.Count) return false;
        return source[index] == target[index];
    }

    private void NewColorPickingImpl(Bitmap bitmap) {
        // 假设这些方法存在于你的项目中
        var bytes = BitmapConveters.BitmapToByteArray(bitmap);
        var back = AccentColorHelper.GetAccentColor(bytes, bitmap.Width, bitmap.Height);

        Application.Current.Dispatcher.Invoke(() => {
            WallpaperColorPlatte.Clear();
            WallpaperColorPlatte.Add(back ?? new Color());
        });

        _logger.LogTrace("实验性取色算法:提取到的主题色:{Color}", back?.ToString());
    }

    // 静态纯函数优化
    public static void ColorToHsv(Color color, out double hue, out double saturation, out double value) {
        int max = Math.Max(color.R, Math.Max(color.G, color.B));
        int min = Math.Min(color.R, Math.Min(color.G, color.B));

        hue = 0; // 此处简化，如果不需要精确 Hue 可忽略
        // 注意：原代码的 Hue 计算其实是缺失的 (hue=0)，这里保持原样以防改变逻辑，
        // 但如果需要 Hue，通常需要更复杂的 switch case。

        saturation = (max == 0) ? 0 : 1d - (1d * min / max);
        value = max / 255d;
    }

    public Task StartAsync(CancellationToken cancellationToken) {
        // 如果有初始化逻辑放这里
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        UpdateTimer.Stop();
        return Task.CompletedTask;
    }

    public void Dispose() {
        RegistryNotifier.Stop();
        SystemEvents.UserPreferenceChanged -= SystemEventsOnUserPreferenceChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null) {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}