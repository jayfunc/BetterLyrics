using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.Core.Interfaces
{
    public interface IAIServicePlugin : IPlugin
    {
        Task<string> ChatAsync(string systemPrompt, string userPrompt);
    }
}
