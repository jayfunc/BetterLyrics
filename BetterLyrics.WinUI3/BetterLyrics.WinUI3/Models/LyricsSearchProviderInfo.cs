using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace BetterLyrics.WinUI3.Models
{
    public partial class LyricsSearchProviderInfo : ObservableObject
    {
        [ObservableProperty]
        public partial LyricsSearchProvider Provider { get; set; }

        [ObservableProperty]
        public partial bool IsEnabled { get; set; }

        public LyricsSearchProviderInfo() { }

        public LyricsSearchProviderInfo(LyricsSearchProvider provider, bool isEnabled)
        {
            Provider = provider;
            IsEnabled = isEnabled;
        }
    }
}
