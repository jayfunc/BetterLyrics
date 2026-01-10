using BetterLyrics.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Services.PluginService
{
    public interface IPluginService
    {
        IReadOnlyList<ILyricsProvider> Providers { get; }

        void LoadPlugins();
    }
}
