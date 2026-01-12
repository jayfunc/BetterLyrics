using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.Core.Interfaces
{
    public interface IPluginContext
    {
        string PluginDirectory { get; }

        IAIServicePlugin? AIService { get; }
    }
}
