using BetterLyrics.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader; // 必须引用
using Windows.Storage;

namespace BetterLyrics.WinUI3.Services.PluginService
{
    public class PluginService : IPluginService
    {
        // 1. 核心插件列表
        private List<IPlugin> _plugins = new();
        public IReadOnlyList<IPlugin> Plugins => _plugins;

        // 2. 新增：上下文管理字典 (Key: 插件ID, Value: 加载上下文)
        // 我们需要存着它，以便将来执行 Unload
        private Dictionary<string, PluginLoadContext> _pluginContexts = new();

        // 3. 新增：已加载的文件路径缓存 (防止同一个 DLL 被扫两遍)
        private HashSet<string> _loadedDllPaths = new();

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
                    // 🔥 防御 1：基于路径的检查
                    // 如果这个文件已经在内存里了，绝对不要再 Load 一次
                    if (_loadedDllPaths.Contains(dllPath)) continue;

                    TryLoadPlugin(dllPath);
                }
            }
        }

        private void TryLoadPlugin(string dllPath)
        {
            try
            {
                // 创建上下文
                var loadContext = new PluginLoadContext(dllPath);

                // 加载程序集
                var assembly = loadContext.LoadFromAssemblyPath(dllPath);

                bool isPluginFound = false;

                foreach (var type in assembly.GetExportedTypes())
                {
                    if (typeof(IPlugin).IsAssignableFrom(type) && !type.IsAbstract)
                    {
                        // 实例化
                        var plugin = (IPlugin?)Activator.CreateInstance(type);
                        if (plugin == null) continue;

                        // 🔥 防御 2：基于 ID 的检查
                        // 防止 "不同 DLL 或者是新版本" 导致 ID 冲突
                        if (_plugins.Any(p => p.Id == plugin.Id))
                        {
                            // 遇到重复 ID，我们选择跳过新的，保留旧的
                            // (或者你也可以设计成卸载旧的加载新的，这取决于策略)
                            // 由于我们已经加载了 assembly，现在决定不用它，必须卸载 context
                            loadContext.Unload();
                            return;
                        }

                        // 初始化插件 (如果有 Initialize 方法)
                        try
                        {
                            plugin.Initialize();
                        }
                        catch (Exception initEx)
                        {
                            // 如果初始化失败（比如缺字典文件），就不应该把它加到列表里
                            // 记录日志...
                            loadContext.Unload();
                            return;
                        }

                        // ✅ 成功入库
                        _plugins.Add(plugin);
                        _pluginContexts.Add(plugin.Id, loadContext); // 记录上下文
                        isPluginFound = true;
                    }
                }

                // 如果这个 DLL 里找到了插件，标记路径为已加载
                if (isPluginFound)
                {
                    _loadedDllPaths.Add(dllPath);
                }
                else
                {
                    // 如果这个 DLL 里一个插件都没找到 (可能是依赖库)，
                    // 为了节省内存，我们可以把这个 Context 卸载掉
                    // (前提是其他插件不依赖它，这块比较复杂，简单起见可以先卸载)
                    loadContext.Unload();
                }
            }
            catch (Exception ex)
            {
                // 记录日志...
                // throw new Exception($"Failed to load plugin from {dllPath}: {ex.Message}", ex);
            }
        }

        public void UninstallPlugin(string pluginId)
        {
            var plugin = _plugins.FirstOrDefault(p => p.Id == pluginId);
            if (plugin == null) return;

            // 1. 获取相关信息
            var dllPath = plugin.GetType().Assembly.Location;
            var folderPath = Path.GetDirectoryName(dllPath);

            // 2. 从列表中移除插件对象
            _plugins.Remove(plugin);
            _loadedDllPaths.Remove(dllPath); // 允许下次重新加载这个路径

            // 3. 💥 核心：卸载上下文 (释放文件锁的关键)
            if (_pluginContexts.TryGetValue(pluginId, out var context))
            {
                context.Unload();
                _pluginContexts.Remove(pluginId);
            }

            // 4. 强制 GC (垃圾回收)
            // 上下文卸载是“软卸载”，必须等 GC 跑过之后，文件锁才会真正释放
            // 这几行代码对于“热删除”非常重要
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // 5. 物理删除文件
            if (Directory.Exists(folderPath))
            {
                try
                {
                    Directory.Delete(folderPath, true);
                }
                catch (IOException)
                {
                    // 如果 GC 还没来得及释放锁，可能会报错
                    // 实际生产中，通常是标记为“待删除”，下次重启时删
                    // 或者提示用户重启
                }
            }
        }

        public void InstallPlugin(string zipPath)
        {
            string pluginsRoot = Path.Combine(ApplicationData.Current.LocalFolder.Path, "plugins");
            string folderName = Path.GetFileNameWithoutExtension(zipPath);
            string installDir = Path.Combine(pluginsRoot, folderName);

            // 如果已经存在，说明是更新或者重装
            // 我们需要先根据文件夹找到旧插件的 ID，然后执行标准的卸载流程
            // (这里简化处理：直接尝试删文件夹，如果删不掉说明被占用)
            if (Directory.Exists(installDir))
            {
                // TODO: 最好是先 Find plugin by path -> UninstallPlugin(id)
                // 否则文件被锁住无法 Delete
                try
                {
                    Directory.Delete(installDir, true);
                }
                catch { /* 忽略或报错 */ }
            }
            Directory.CreateDirectory(installDir);

            ZipFile.ExtractToDirectory(zipPath, installDir);

            // 安装完后，如果不重启软件，你想立即生效的话：
            // LoadPlugins(); // 因为加了路径去重，这里重新调一次是安全的
        }
    }
}