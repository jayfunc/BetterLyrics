using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.Settings
{
    public interface ISettingsService
    {
        T? Get<T>(string key, T? defaultValue = default);
        void Set<T>(string key, T value);
    }
}
