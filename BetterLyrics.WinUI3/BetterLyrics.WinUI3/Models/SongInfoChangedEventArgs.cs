using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models
{
    public class SongInfoChangedEventArgs(SongInfo? songInfo) : EventArgs
    {
        public SongInfo? SongInfo { get; set; } = songInfo;
    }
}
