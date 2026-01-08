using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using System;
using System.Collections.Generic;

namespace BetterLyrics.WinUI3.Events
{
    public class MediaSourceProvidersInfoEventArgs(List<MediaSourceProviderInfo> sessionIds) : EventArgs
    {
        public List<MediaSourceProviderInfo> MediaSourceProviersInfo { get; set; } = sessionIds;
    }
}
