using BetterLyrics.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
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

        public T? GetPlugin<T>() where T : class, IPlugin
        {
            return _plugins.OfType<T>().FirstOrDefault();
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

            InitializePlugins();
        }

        private void InitializePlugins()
        {
            foreach (var plugin in _plugins)
            {
                try
                {
                    string dllPath = plugin.GetType().Assembly.Location;
                    string? pluginDir = Path.GetDirectoryName(dllPath);
                    if (pluginDir == null) continue;

                    var context = new PluginContext(this, pluginDir);
                    plugin.OnLoad(context);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize plugin {Name}", plugin.Name);
                }
            }
        }

        private void TryLoadPlugin(string dllPath)
        {
            // 1. Create Context
            var loadContext = new PluginLoadContext(dllPath);

            try
            {
                var assembly = loadContext.LoadFromAssemblyPath(dllPath);
                int loadedCount = 0; // Track successfully loaded plugins

                // 2. [Safety Check] Safely retrieve types
                IEnumerable<Type> types;
                try
                {
                    types = assembly.GetExportedTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // If some types fail to load, only keep the usable ones!
                    types = ex.Types.Where(t => t != null)!;
                    foreach (var loaderEx in ex.LoaderExceptions)
                    {
                        _logger.LogWarning("Partial type loading failure in DLL {path}: {msg}", dllPath, loaderEx?.Message);
                    }
                }

                foreach (var type in types)
                {
                    // 3. Check if it is a valid plugin class
                    if (typeof(IPlugin).IsAssignableFrom(type) && !type.IsAbstract)
                    {
                        IPlugin? plugin = null;
                        try
                        {
                            // 4. [Instantiation Guard] Prevent plugin constructor errors from crashing the main app
                            plugin = (IPlugin?)Activator.CreateInstance(type);
                            if (plugin == null) continue;

                            // 5. Check for duplicate IDs
                            if (_plugins.Any(p => p.Id == plugin.Id))
                            {
                                _logger.LogWarning("Skipping duplicate plugin: {id} ({path})", plugin.Id, dllPath);
                                // Explicitly break reference to aid Unload
                                plugin = null;
                                continue;
                            }

                            // 7. Add to collection
                            _plugins.Add(plugin);
                            _pluginContexts.Add(plugin.Id, loadContext);
                            loadedCount++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to initialize/instantiate plugin {type}", type.FullName);

                            // If plugin initialization fails, explicitly dispose if possible
                            if (plugin is IDisposable disposable)
                            {
                                try { disposable.Dispose(); } catch { }
                            }
                            plugin = null; // Break reference
                        }
                    }
                }

                // 8. Finalize: If no usable plugins were found in this DLL, unload Context
                if (loadedCount > 0)
                {
                    _loadedDllPaths.Add(dllPath);
                    _logger.LogInformation("Successfully loaded {count} plugin(s) from {path}", loadedCount, dllPath);
                }
                else
                {
                    _logger.LogWarning("No valid plugins found in {path}. Unloading context.", dllPath);
                    // No plugin instances remain alive at this point, safe to unload
                    loadContext.Unload();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load assembly: {path}", dllPath);
                // Only unload here if the loading process completely crashed
                try { loadContext.Unload(); } catch { }
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