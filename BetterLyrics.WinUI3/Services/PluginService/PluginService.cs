using BetterLyrics.Core.Interfaces;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.SettingsService;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Windows.Storage;

namespace BetterLyrics.WinUI3.Services.PluginService
{
    public class PluginService : IPluginService
    {
        private Dictionary<string, PluginLoadContext> _pluginContexts = new();
        private HashSet<string> _loadedDllPaths = new();

        private readonly ISettingsService _settingsService;
        private readonly ILogger<PluginService> _logger;

        public PluginService(ISettingsService settingsService, ILogger<PluginService> logger)
        {
            _settingsService = settingsService;
            _logger = logger;
        }

        public T? GetPlugin<T>() where T : class
        {
            var plugins = _settingsService.AppSettings.PluginsInfo;

            return plugins.OfType<T>().FirstOrDefault();
        }

        public void LoadPlugins()
        {
            var pluginFolders = Directory.GetDirectories(PathHelper.PluginsDirectory);

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

        public void UninstallPlugin(string pluginId)
        {
            var plugins = _settingsService.AppSettings.PluginsInfo;

            var activePlugin = plugins.FirstOrDefault(p => p.Id == pluginId);
            if (activePlugin != null)
            {
                plugins.Remove(activePlugin);
            }
        }

        public void InstallPlugin(string zipPath)
        {
            var plugins = _settingsService.AppSettings.PluginsInfo;

            string tempExtractPath = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, Guid.NewGuid().ToString());
            ZipFile.ExtractToDirectory(zipPath, tempExtractPath);

            string? pluginId = IdentifyPluginId(tempExtractPath);

            string pendingDir = Path.Combine(PathHelper.PendingPluginsDirectory, pluginId);

            if (Directory.Exists(pendingDir)) Directory.Delete(pendingDir, true);

            Directory.Move(tempExtractPath, pendingDir);

            plugins.Add(new PluginInfo(pluginId));
        }

        public void PerformFileSynchronization()
        {
            var plugins = _settingsService.AppSettings.PluginsInfo;

            if (Directory.Exists(PathHelper.PendingPluginsDirectory))
            {
                foreach (var pendingDir in Directory.GetDirectories(PathHelper.PendingPluginsDirectory))
                {
                    string folderName = Path.GetFileName(pendingDir);
                    string targetDir = Path.Combine(PathHelper.PluginsDirectory, folderName);

                    try
                    {
                        if (Directory.Exists(targetDir))
                        {
                            Directory.Delete(targetDir, true);
                        }

                        Directory.Move(pendingDir, targetDir);
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }

            if (Directory.Exists(PathHelper.PluginsDirectory))
            {
                foreach (var pluginDir in Directory.GetDirectories(PathHelper.PluginsDirectory))
                {
                    string pluginId = Path.GetFileName(pluginDir);

                    if (!plugins.Any(x => x.Id == pluginId))
                    {
                        try
                        {
                            Directory.Delete(pluginDir, true);
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }
            }
        }

        private void InitializePlugins()
        {
            var plugins = _settingsService.AppSettings.PluginsInfo;

            foreach (var plugin in plugins)
            {
                try
                {
                    string dllPath = plugin.GetType().Assembly.Location;
                    string? pluginDir = Path.GetDirectoryName(dllPath);
                    if (pluginDir == null) continue;

                    var context = new PluginContext(this, pluginDir);
                    plugin.Plugin.OnLoad(context);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize plugin {Name}", plugin.Plugin.Name);
                }
            }
        }

        /// <summary>
        /// Invoke this method only when the app starts
        /// </summary>
        /// <param name="dllPath"></param>
        private void TryLoadPlugin(string dllPath)
        {
            var plugins = _settingsService.AppSettings.PluginsInfo;
            var loadContext = new PluginLoadContext(dllPath);

            try
            {
                var assembly = loadContext.LoadFromAssemblyPath(dllPath);
                int loadedCount = 0;

                IEnumerable<Type> types;
                try
                {
                    types = assembly.GetExportedTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null)!;
                    foreach (var loaderEx in ex.LoaderExceptions)
                    {
                        _logger.LogWarning("Partial type loading failure in DLL {path}: {msg}", dllPath, loaderEx?.Message);
                    }
                }

                foreach (var type in types)
                {
                    if (typeof(IPlugin).IsAssignableFrom(type) && !type.IsAbstract)
                    {
                        IPlugin? plugin = null;
                        try
                        {
                            plugin = (IPlugin?)Activator.CreateInstance(type);
                            if (plugin == null) continue;

                            var pluginFound = plugins.FirstOrDefault(p => p.Id == plugin.Id);
                            if (pluginFound == null)
                            {
                                plugins.Add(new PluginInfo(plugin));
                            }
                            else if (pluginFound.Plugin == null)
                            {
                                pluginFound.Plugin = plugin;
                            }

                            if (_pluginContexts.ContainsKey(plugin.Id))
                            {
                                _pluginContexts.Add(plugin.Id, loadContext);
                                loadedCount++;
                            }
                            else
                            {
                                _logger.LogWarning("Skipping duplicate plugin: {id} ({path})", plugin.Id, dllPath);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to initialize/instantiate plugin {type}", type.FullName);

                            if (plugin is IDisposable disposable)
                            {
                                try { disposable.Dispose(); } catch { }
                            }
                            plugin = null;
                        }
                    }
                }

                if (loadedCount > 0)
                {
                    _loadedDllPaths.Add(dllPath);
                    _logger.LogInformation("Successfully loaded {count} plugin(s) from {path}", loadedCount, dllPath);
                }
                else
                {
                    _logger.LogWarning("No valid plugins found in {path}. Unloading context.", dllPath);
                    loadContext.Unload();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load assembly: {path}", dllPath);
                try { loadContext.Unload(); } catch { }
            }
        }

        private string? IdentifyPluginId(string folderPath)
        {
            var dllFiles = Directory.GetFiles(folderPath, "*.dll", SearchOption.AllDirectories);

            foreach (var dllPath in dllFiles)
            {
                try
                {
                    using var stream = File.OpenRead(dllPath);
                    using var peReader = new PEReader(stream);

                    if (!peReader.HasMetadata) continue;

                    var reader = peReader.GetMetadataReader();
                    if (!reader.IsAssembly) continue;

                    var assemblyDefinition = reader.GetAssemblyDefinition();
                    string assemblyName = reader.GetString(assemblyDefinition.Name);

                    if (assemblyName.Contains("BetterLyrics.Plugins") || IsReferencingCore(reader))
                    {
                        return assemblyName;
                    }
                }
                catch
                {
                }
            }
            return null;
        }

        private bool IsReferencingCore(MetadataReader reader)
        {
            foreach (var handle in reader.AssemblyReferences)
            {
                var reference = reader.GetAssemblyReference(handle);
                string refName = reader.GetString(reference.Name);
                if (refName == "BetterLyrics.Core") return true;
            }
            return false;
        }

    }
}