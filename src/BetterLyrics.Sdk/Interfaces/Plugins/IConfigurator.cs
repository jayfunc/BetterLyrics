using BetterLyrics.Sdk.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.Sdk.Interfaces.Plugins
{
    public interface IConfigurator
    {
        object Get(string key, object defaultValue);
        void Set(string key, object value, ConfigChangedBy configChangedBy);

        event EventHandler<string, ConfigChangedBy>? OnConfigChanged;
    }

}
