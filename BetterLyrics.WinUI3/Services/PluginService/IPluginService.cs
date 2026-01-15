using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.PluginService
{
    public interface IPluginService
    {
        /// <summary>
        /// 获取已加载的特定类型的插件实例
        /// </summary>
        T? GetPlugin<T>() where T : class;
        /// <summary>
        /// 程序启动时调用：同步文件 -> 清理配置 -> 加载插件
        /// </summary>
        Task LoadPluginsAsync();
        /// <summary>
        /// 安装插件：解压 -> 移动到 Pending 目录 -> 提示重启
        /// </summary>
        void InstallPlugin(string zipPath);
        /// <summary>
        /// 卸载插件：创建删除标记 -> 提示重启
        /// </summary>
        void UninstallPlugin(string pluginId);
        Task TogglePluginAsync(string pluginId, bool isEnabled);
    }
}
