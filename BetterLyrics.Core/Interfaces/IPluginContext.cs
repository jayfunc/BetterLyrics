using BetterLyrics.Core.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.Core.Interfaces
{
    public interface IPluginContext
    {
        string PluginDirectory { get; }

        IAIService? AIService { get; }
    }
}
