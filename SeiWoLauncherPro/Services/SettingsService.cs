using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Common.Services;

/// <summary>
/// 通用配置管理服务
/// </summary>
/// <typeparam name="TSettings">配置类类型，必须支持无参构造并实现 INotifyPropertyChanged</typeparam>
public class SettingsService<TSettings> : INotifyPropertyChanged
    where TSettings : class, INotifyPropertyChanged, new() {
    private readonly ILogger<SettingsService<TSettings>> _logger;
    private readonly string _configPath;
    private TSettings _settings = new();

    // --- 性能优化：IO 防抖与并发控制 ---
    private readonly System.Threading.Timer _saveDebounceTimer;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private const int SaveDelayMilliseconds = 500; // 防抖延迟：500ms
    private bool _isDirty = false; // 标记是否有未保存的更改

    // --- Overlay 系统 ---
    // 存储结构：PropertyKey -> (OverlayGuid -> Value)
    private readonly Dictionary<string, Dictionary<string, object?>> _overlayStorage = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public TSettings Settings
    {
        get => _settings;
        private set => SetField(ref _settings, value);
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="appRootPath">应用根目录（用于存放 Settings.json）</param>
    /// <param name="configFileName">配置文件名，默认为 Settings.json</param>
    public SettingsService(ILogger<SettingsService<TSettings>> logger, string appRootPath, string configFileName = "Settings.json")
    {
        _logger = logger;
        _configPath = Path.Combine(appRootPath, configFileName);

        // 初始化防抖计时器，初始状态为停止
        _saveDebounceTimer = new System.Threading.Timer(async _ => await SaveSettingsInternalAsync(), null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>
    /// 加载配置
    /// </summary>
    public async Task LoadSettingsAsync()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                _logger.LogInformation("正在加载配置文件: {Path}", _configPath);

                using var stream = File.OpenRead(_configPath);
                var loadedSettings = await JsonSerializer.DeserializeAsync<TSettings>(stream, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                });

                if (loadedSettings != null)
                {
                    Settings = loadedSettings;
                }
            }
            else
            {
                _logger.LogInformation("配置文件不存在，将使用默认设置。");
                Settings = new TSettings();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "配置文件加载失败，回退到默认设置。");
            Settings = new TSettings();
        }
        finally
        {
            // 重新绑定变更事件
            HookPropertyChanged();
        }
    }

    private void HookPropertyChanged()
    {
        // 先移除旧的事件，防止内存泄漏或多次触发
        Settings.PropertyChanged -= OnSettingsPropertyChanged;
        Settings.PropertyChanged += OnSettingsPropertyChanged;
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName)) return;

        // 忽略带有 JsonIgnore 的属性，避免不必要的保存
        var propInfo = typeof(TSettings).GetProperty(e.PropertyName);
        if (propInfo != null && propInfo.GetCustomAttribute<JsonIgnoreAttribute>() != null)
        {
            return;
        }

        // 触发防抖保存
        RequestSave();
    }

    /// <summary>
    /// 请求保存配置（带防抖）
    /// </summary>
    public void RequestSave()
    {
        _isDirty = true;
        // 重置计时器，如果在 SaveDelayMilliseconds 内再次调用，计时器会重新开始
        _saveDebounceTimer.Change(SaveDelayMilliseconds, Timeout.Infinite);
    }

    /// <summary>
    /// 立即执行异步保存（内部使用）
    /// </summary>
    private async Task SaveSettingsInternalAsync()
    {
        if (!_isDirty) return;

        // 获取锁，确保同一时间只有一个写入操作
        await _fileLock.WaitAsync();
        try
        {
            _logger.LogDebug("开始写入配置文件...");

            // 确保目录存在
            var dir = Path.GetDirectoryName(_configPath);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // 使用 FileStream 和 SerializeAsync 进行真正的异步写入
            await using var stream = new FileStream(_configPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
            await JsonSerializer.SerializeAsync(stream, Settings, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 防止中文被转义
            });

            _isDirty = false;
            _logger.LogDebug("配置文件写入完成。");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "写入配置文件时发生错误。");
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// 手动强制保存（通常在程序退出时调用）
    /// </summary>
    public async Task SaveAsync()
    {
        // 停止防抖计时器
        _saveDebounceTimer.Change(Timeout.Infinite, Timeout.Infinite);
        await SaveSettingsInternalAsync();
    }

    #region Overlay System (配置叠层系统)

    /// <summary>
    /// 添加设置叠层（临时修改设置）
    /// </summary>
    /// <param name="guid">叠层唯一标识</param>
    /// <param name="propertyName">属性名称</param>
    /// <param name="value">新值</param>
    public void AddSettingsOverlay(string guid, string propertyName, object? value)
    {
        var property = typeof(TSettings).GetProperty(propertyName);
        if (property == null)
        {
            _logger.LogWarning("尝试为不存在的属性 {PropertyName} 添加叠层", propertyName);
            return;
        }

        // 初始化该属性的叠层字典
        if (!_overlayStorage.ContainsKey(propertyName))
        {
            _overlayStorage[propertyName] = new Dictionary<string, object?>();
            // 保存原始值，Key 为 "@"
            var originalValue = property.GetValue(Settings);
            _overlayStorage[propertyName]["@"] = originalValue;
        }

        var layers = _overlayStorage[propertyName];

        // 如果当前值和要设置的值相同，且不是第一次覆盖，则跳过（优化）
        var currentValue = property.GetValue(Settings);
        if (Equals(currentValue, value) && layers.ContainsKey(guid)) return;

        // 更新叠层值
        layers[guid] = value;

        // 应用新值 (这将触发 PropertyChanged -> RequestSave)
        // 注意：因为这里修改了Settings，会触发保存。
        // 如果 Overlay 是极其临时的（如每帧变化），建议在 TSettings 属性上加 [JsonIgnore]，
        // 或者修改 RequestSave 逻辑来忽略特定来源的变更。
        property.SetValue(Settings, value);

        _logger.LogTrace("已应用叠层: {Guid} -> {Property} = {Value}", guid, propertyName, value);
    }

    /// <summary>
    /// 移除设置叠层（恢复之前的值）
    /// </summary>
    public void RemoveSettingsOverlay(string guid, string propertyName)
    {
        var property = typeof(TSettings).GetProperty(propertyName);
        if (property == null || !_overlayStorage.TryGetValue(propertyName, out var layers)) return;

        if (layers.Remove(guid))
        {
            // 寻找下一个生效的值：取字典中最后一个添加的值（假设字典顺序通常是插入顺序，
            // 但为了严谨，如果你需要优先级，可能需要改为 OrderedDictionary 或 List）
            // 这里简单取最后一个作为“最顶层”
            var nextValue = layers.Values.LastOrDefault();

            // 如果取出的值是 JsonElement (反序列化残留)，尝试转换
            if (nextValue is JsonElement jsonElement)
            {
                nextValue = jsonElement.Deserialize(property.PropertyType);
            }

            property.SetValue(Settings, nextValue);
            _logger.LogTrace("已移除叠层: {Guid} -> {Property}, 恢复为 {Value}", guid, propertyName, nextValue);

            // 如果只剩下原始值 "@"，则清理字典
            if (layers.Count == 1 && layers.ContainsKey("@"))
            {
                _overlayStorage.Remove(propertyName);
            }
        }
    }

    #endregion

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<TField>(ref TField field, TField value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<TField>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}