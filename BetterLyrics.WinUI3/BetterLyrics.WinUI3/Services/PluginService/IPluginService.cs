using BetterLyrics.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Services.PluginService
{
    public interface IPluginService
    {
        T? GetPlugin<T>() where T : class;

        /// <summary>
        /// Invoke this method only when the app starts
        /// </summary>
        void LoadPlugins();
        void InstallPlugin(string zipPath);
        void UninstallPlugin(string pluginId);
        void PerformFileSynchronization();
    }
}
