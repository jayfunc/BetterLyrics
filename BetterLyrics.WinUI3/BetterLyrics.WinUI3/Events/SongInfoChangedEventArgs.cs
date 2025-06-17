using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Models;

namespace BetterLyrics.WinUI3.Events
{
    public class SongInfoChangedEventArgs(SongInfo? songInfo) : EventArgs
    {
        public SongInfo? SongInfo { get; set; } = songInfo;
    }
}
