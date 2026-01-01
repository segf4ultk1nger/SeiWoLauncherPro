using CommunityToolkit.Mvvm.ComponentModel;
using SeiWoLauncherPro.Enums;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace SeiWoLauncherPro.Models;

public partial class Settings : ObservableObject
{
    [ObservableProperty]
    private int _windowWidth = 400;

    [ObservableProperty]
    private double _backgroundOpacity = 0.9;

    [ObservableProperty]
    private int _wallpaperAutoUpdateIntervalSeconds = 30;

    [ObservableProperty]
    private bool _isWallpaperAutoUpdateEnabled = false;

    [ObservableProperty]
    private SettingsEnums.AccentColorSource _accentColorSource = SettingsEnums.AccentColorSource.SystemAccent;

    [ObservableProperty]
    private bool _isAccentColorRequestUsingFallbackMode = false;

    [ObservableProperty]
    private ObservableCollection<Color> _wallpaperColorPalette = new(Enumerable.Repeat(Colors.DodgerBlue, 5));

    [ObservableProperty]
    private int _selectedColorPaletteIndex = 0;
}