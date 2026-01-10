using BetterLyrics.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Services.PluginService
{
    public class PluginService : IPluginService
    {
        private List<ILyricsProvider> _providers = new();

        public IReadOnlyList<ILyricsProvider> Providers => _providers;

        public void LoadPlugins()
        {
            // 在涉及加载程序集的地方：
            // var context = new PluginLoadContext(pluginPath); 
            // 它是本文件夹下的 internal 或者是 public 类，直接用即可。
        }
    }
}
