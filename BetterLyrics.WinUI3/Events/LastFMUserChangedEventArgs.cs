using Hqub.Lastfm.Entities;
using System;

namespace BetterLyrics.WinUI3.Events
{
    public class LastFMUserChangedEventArgs : EventArgs
    {
        public User? User { get; set; }
        public LastFMUserChangedEventArgs(User? user)
        {
            User = user;
        }
    }
}
