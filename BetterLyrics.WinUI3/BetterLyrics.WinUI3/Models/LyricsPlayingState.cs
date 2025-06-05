using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models
{
    public enum LyricsPlayingState
    {
        /// <summary>
        /// Not played yet, will be playing in the future
        /// </summary>
        NotPlayed,

        /// <summary>
        /// Playing
        /// </summary>
        Playing,

        /// <summary>
        /// Has already played
        /// </summary>
        Played,
    }
}
