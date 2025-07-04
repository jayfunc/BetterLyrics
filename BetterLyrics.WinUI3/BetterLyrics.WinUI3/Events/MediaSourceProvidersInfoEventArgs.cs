using BetterLyrics.WinUI3.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Events
{
    public class MediaSourceProvidersInfoEventArgs(List<MediaSourceProviderInfo> sessionIds):EventArgs
    {
        public List<MediaSourceProviderInfo> MediaSourceProviersInfo { get; set; } = sessionIds;
    }
}
