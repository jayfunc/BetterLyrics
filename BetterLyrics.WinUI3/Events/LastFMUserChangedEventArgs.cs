using LiteFM.Abstractions;
using System;

namespace BetterLyrics.WinUI3.Events
{
    public class LastFMUserChangedEventArgs : EventArgs
    {
        public LastFMUser? User { get; set; }
        public LastFMUserChangedEventArgs(LastFMUser? user)
        {
            User = user;
        }
    }
}
