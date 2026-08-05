using System.IO.Compression;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.ViewModels;
using BetterLyrics.Sdk.Enums;
using BetterLyrics.Sdk.Interfaces.Plugins;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Extensions.Logging;

namespace BetterLyrics.Core.Implementations.Services.PluginService;

public class PluginService : BaseViewModel, IPluginService, IRecipient<PropertyChangedMessage<bool>>
{
    private readonly Dictionary<string, IConfigurator?> _configurator = [];
    private readonly Dictionary<string, int> _hashedId = [];
    private readonly ILogger<PluginService> _logger;
    private readonly ISettingsService _settingsService;

    public PluginService(ISettingsService settingsService, ILogger<PluginService> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    public T? GetPlugin<T>() where T : class
    {
        var info = _settingsService.AppSettings.PluginsInfo
            .FirstOrDefault(p =>
                p.IsEnabled &&
                p.IsInitialized &&
                p.Plugin is T
            );

        return info?.Plugin as T;
    }

    public async Task LoadPluginsAsync()
    {
        PerformFileSynchronization();

        var pluginsList = _settingsService.AppSettings.PluginsInfo;
        pluginsList.ToList().RemoveAll(p => !Directory.Exists(Path.Combine(PathHelper.PluginsDirectory, p.Id)));

        if (!Directory.Exists(PathHelper.PluginsDirectory)) return;

        var pluginFolders = Directory.GetDirectories(PathHelper.PluginsDirectory);

        foreach (var folder in pluginFolders)
        {
            var pluginId = Path.GetFileName(folder);
            var dllPath = Path.Combine(folder, $"{pluginId}.dll");

            if (!File.Exists(dllPath)) continue;

            try
            {
                var plugin = LoadPluginAssembly(dllPath);
                if (plugin == null) continue;

                var existingInfo = pluginsList.FirstOrDefault(p => p.Id == pluginId);
                if (existingInfo != null)
                    existingInfo.Plugin = plugin;
                else
                    pluginsList.Add(new PluginInfo(plugin));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin from {Path}", dllPath);
            }
        }

        foreach (var info in pluginsList)
            if (info.IsEnabled && !info.IsInitialized)
                await InitializePluginsAsync(info);
    }

    public void InstallPlugin(string zipPath)
    {
        var tempId = Guid.NewGuid().ToString();
        var tempPath = Path.Combine(Path.GetTempPath(), tempId);

        try
        {
            ZipFile.ExtractToDirectory(zipPath, tempPath);

            var pluginId = PluginMetadataHelper.IdentifyPluginId(tempPath);

            if (string.IsNullOrEmpty(pluginId))
                throw new Exception("Invalid plugin package: Could not identify Plugin ID.");

            var pendingDir = Path.Combine(PathHelper.PendingPluginsDirectory, pluginId);
            ForceDeleteDirectory(pendingDir);
            Directory.Move(tempPath, pendingDir);

            _logger.LogInformation("Plugin {Id} prepared for installation in {Path}", pluginId, pendingDir);
        }
        catch (Exception)
        {
            if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true);
            throw;
        }
    }

    public void UninstallPlugin(string pluginId)
    {
        var targetDir = Path.Combine(PathHelper.PluginsDirectory, pluginId);

        if (Directory.Exists(targetDir))
        {
            var markerFile = Path.Combine(targetDir, ".delete");
            File.WriteAllText(markerFile, "delete me");

            _logger.LogInformation("Plugin {Id} marked for deletion.", pluginId);
        }

        var info = _settingsService.AppSettings.PluginsInfo.FirstOrDefault(p => p.Id == pluginId);
        if (info != null)
        {
            if (info.Plugin != null && info.IsInitialized)
            {
                try
                {
                    _configurator.Remove(pluginId);
                    _hashedId.Remove(pluginId);
                    info.Plugin.DisposeAsync().AsTask().Wait();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing plugin {Id} during uninstallation", pluginId);
                }
            }
            _settingsService.AppSettings.PluginsInfo.Remove(info);
        }
    }

    public async Task TogglePluginAsync(string pluginId)
    {
        var info = _settingsService.AppSettings.PluginsInfo.FirstOrDefault(p => p.Id == pluginId);
        if (info == null) return;

        if (info.IsEnabled)
        {
            if (!info.IsInitialized) await InitializePluginsAsync(info);
        }
        else
        {
            if (info.Plugin != null && info.IsInitialized)
            {
                try
                {
                    _configurator.Remove(pluginId);
                    _hashedId.Remove(pluginId);
                    await info.Plugin.DisposeAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing plugin {Id}", pluginId);
                }

                info.IsInitialized = false;
            }
        }
    }

    public void SetSettingItem(string pluginId, string key, object value)
    {
        _configurator.GetValueOrDefault(pluginId)?.Set(key, value, ConfigChangedBy.Host);
    }

    public object GetSettingItem(string pluginId, string key, object defaultValue)
    {
        return _configurator.GetValueOrDefault(pluginId)?.Get(key, defaultValue) ?? defaultValue;
    }

    public int GetPluginHashedId(string pluginId)
    {
        return _hashedId.GetValueOrDefault(pluginId, -1);
    }

    public string GetPluginId(int hashedId)
    {
        return _hashedId
            .FirstOrDefault(x => x.Value == hashedId, new KeyValuePair<string, int>("N/A", -1)).Key;
    }

    public void Receive(PropertyChangedMessage<bool> message)
    {
        if (message.Sender is PluginInfo pluginInfo)
            if (message.PropertyName == nameof(PluginInfo.IsEnabled))
                _ = TogglePluginAsync(pluginInfo.Id);
    }

    private async Task InitializePluginsAsync(PluginInfo pluginInfo)
    {
        if (pluginInfo.IsInitialized) return;

        try
        {
            if (pluginInfo.Plugin == null) return;

            var dllPath = pluginInfo.Plugin.GetType().Assembly.Location;
            var pluginDir = Path.GetDirectoryName(dllPath);

            if (pluginDir == null) return;

            var localizer = new PluginLocalizer(pluginDir);
            var configurator = new PluginConfigurator(pluginDir);
            var context = new PluginContext(this, pluginDir, localizer, configurator);

            _configurator[pluginInfo.Id] = configurator;
            _hashedId[pluginInfo.Id] = HashHelper.GetSafeHash(pluginInfo.Id, 1000);

            await pluginInfo.Plugin.InitializeAsync(context);
            pluginInfo.IsInitialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize plugin {Id}", pluginInfo.Plugin?.Id);
        }
    }

    /// <summary>
    ///     文件同步逻辑：处理 Pending 的移动和标记为 .delete 的删除
    /// </summary>
    private void PerformFileSynchronization()
    {
        if (Directory.Exists(PathHelper.PluginsDirectory))
            foreach (var dir in Directory.GetDirectories(PathHelper.PluginsDirectory))
                if (File.Exists(Path.Combine(dir, ".delete")))
                    try
                    {
                        ForceDeleteDirectory(dir);
                        _logger.LogInformation("Cleaned up uninstalled plugin: {Dir}", dir);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to delete plugin directory {Dir}", dir);
                    }

        if (Directory.Exists(PathHelper.PendingPluginsDirectory))
            foreach (var pendingDir in Directory.GetDirectories(PathHelper.PendingPluginsDirectory))
            {
                var folderName = Path.GetFileName(pendingDir);
                var targetDir = Path.Combine(PathHelper.PluginsDirectory, folderName);

                try
                {
                    ForceDeleteDirectory(targetDir);

                    Directory.Move(pendingDir, targetDir);
                    _logger.LogInformation("Applied plugin update/install: {Dir}", targetDir);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to move plugin from {Src} to {Dst}", pendingDir, targetDir);
                }
            }
        // (可选) 清理空的 Pending 根目录，保持整洁
        // try { Directory.Delete(PathHelper.PendingPluginsDirectory); } catch { }
    }

    private IPlugin? LoadPluginAssembly(string dllPath)
    {
        var context = new PluginLoadContext(dllPath);

        try
        {
            var assembly = context.LoadFromAssemblyPath(dllPath);

            var type = assembly.GetExportedTypes()
                .FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract);

            if (type == null) return null;

            return (IPlugin?)Activator.CreateInstance(type);
        }
        catch (Exception ex)
        {
            try
            {
                context.Unload();
            }
            catch
            {
            }

            throw;
        }
    }

    private void ForceDeleteDirectory(string path)
    {
        if (!Directory.Exists(path)) return;

        var dir = new DirectoryInfo(path);
        foreach (var info in dir.GetFileSystemInfos("*", SearchOption.AllDirectories))
        {
            info.Attributes = FileAttributes.Normal;
        }

        for (int i = 0; i < 3; i++)
        {
            try
            {
                dir.Delete(true);
                return;
            }
            catch (DirectoryNotFoundException)
            {
                return;
            }
            catch (Exception ex)
            {
                if (i == 2)
                {
                    _logger.LogError(ex, "Failed to force delete directory {Path} after retries.", path);
                    throw;
                }
                System.Threading.Thread.Sleep(100);
            }
        }
    }
}