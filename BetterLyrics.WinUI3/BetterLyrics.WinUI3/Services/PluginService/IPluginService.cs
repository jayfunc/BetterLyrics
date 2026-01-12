using BetterLyrics.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Services.PluginService
{
    public interface IPluginService
    {
        IReadOnlyList<IPlugin> Plugins { get; }

        T? GetPlugin<T>() where T : class, IPlugin;
        void LoadPlugins();
        void InstallPlugin(string zipPath);
        void UninstallPlugin(string pluginId);
    }
}
