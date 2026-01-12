using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.Core.Interfaces.Services
{
    public interface IAIService
    {
        Task<string> ChatAsync(string systemPrompt, string userPrompt);
    }
}
