using BetterLyrics.Core.Interfaces.Providers;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.Avalonia.Providers
{
    public class PlatformProvider : IPlatformProvider
    {
        public string AppVersion => "";

        public void DeleteCredential(string resource, string key)
        {
            throw new NotImplementedException();
        }

        public string? GetCredential(string resource, string key)
        {
            throw new NotImplementedException();
        }

        public void SaveCredential(string resource, string key, string value)
        {
            throw new NotImplementedException();
        }
    }
}
