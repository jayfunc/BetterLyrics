using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Events
{
    public class IsPlayingChangedEventArgs(bool isPlaying) : EventArgs
    {
        public bool IsPlaying { get; set; } = isPlaying;
    }
}
