using BetterLyrics.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Windows.Storage;

namespace BetterLyrics.WinUI3.Services.PluginService
{
    public class PluginService : IPluginService
    {
        private List<IPlugin> _plugins = new();
        public IReadOnlyList<IPlugin> Plugins => _plugins;
        private Dictionary<string, PluginLoadContext> _pluginContexts = new();
        private HashSet<string> _loadedDllPaths = new();

        private readonly ILogger<PluginService> _logger;

        public PluginService(ILogger<PluginService> logger)
        {
            _logger = logger;
        }

        public void LoadPlugins()
        {
            string pluginsRoot = Path.Combine(ApplicationData.Current.LocalFolder.Path, "plugins");
            if (!Directory.Exists(pluginsRoot)) Directory.CreateDirectory(pluginsRoot);

            var pluginFolders = Directory.GetDirectories(pluginsRoot);

            foreach (var folder in pluginFolders)
            {
                var dllFiles = Directory.GetFiles(folder, "*.dll");

                foreach (var dllPath in dllFiles)
                {
                    if (_loadedDllPaths.Contains(dllPath)) continue;

                    TryLoadPlugin(dllPath);
                }
            }
        }

        private void TryLoadPlugin(string dllPath)
        {
            try
            {
                var loadContext = new PluginLoadContext(dllPath);
                var assembly = loadContext.LoadFromAssemblyPath(dllPath);
                bool isPluginFound = false;

                foreach (var type in assembly.GetExportedTypes())
                {
                    if (typeof(IPlugin).IsAssignableFrom(type) && !type.IsAbstract)
                    {
                        var plugin = (IPlugin?)Activator.CreateInstance(type);
                        if (plugin == null) continue;

                        if (_plugins.Any(p => p.Id == plugin.Id))
                        {
                            // 遇到重复 ID，我们选择跳过新的，保留旧的
                            // (或者你也可以设计成卸载旧的加载新的，这取决于策略)
                            // 由于我们已经加载了 assembly，现在决定不用它，必须卸载 context
                            loadContext.Unload();
                            return;
                        }

                        try
                        {
                            plugin.Initialize();
                        }
                        catch (Exception initEx)
                        {
                            _logger.LogError(initEx, "Failed to initialize plugin {id} from {path}", plugin.Id, dllPath);
                            loadContext.Unload();
                            return;
                        }

                        _plugins.Add(plugin);
                        _pluginContexts.Add(plugin.Id, loadContext);
                        isPluginFound = true;
                    }
                }

                if (isPluginFound)
                {
                    _loadedDllPaths.Add(dllPath);
                }
                else
                {
                    _logger.LogWarning("No valid plugin types found in assembly {path}", dllPath);
                    loadContext.Unload();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin from {path}", dllPath);
            }
        }

        public void UninstallPlugin(string pluginId)
        {
            var plugin = _plugins.FirstOrDefault(p => p.Id == pluginId);
            if (plugin == null) return;

            var dllPath = plugin.GetType().Assembly.Location;
            var folderPath = Path.GetDirectoryName(dllPath);

            _plugins.Remove(plugin);
            _loadedDllPaths.Remove(dllPath);

            if (_pluginContexts.TryGetValue(pluginId, out var context))
            {
                context.Unload();
                _pluginContexts.Remove(pluginId);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            if (Directory.Exists(folderPath))
            {
                try
                {
                    Directory.Delete(folderPath, true);
                }
                catch (IOException ex)
                {
                    _logger.LogError(ex, "Failed to delete plugin folder {}", folderPath);
                }
            }
        }

        public void InstallPlugin(string zipPath)
        {
            string pluginsRoot = Path.Combine(ApplicationData.Current.LocalFolder.Path, "plugins");
            string folderName = Path.GetFileNameWithoutExtension(zipPath);
            string installDir = Path.Combine(pluginsRoot, folderName);

            if (Directory.Exists(installDir))
            {
                // TODO: 最好是先 Find plugin by path -> UninstallPlugin(id)
                // 否则文件被锁住无法 Delete
                try
                {
                    Directory.Delete(installDir, true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete existing plugin folder {}", installDir);
                    throw;
                }
            }

            Directory.CreateDirectory(installDir);
            ZipFile.ExtractToDirectory(zipPath, installDir);
        }
    }
}