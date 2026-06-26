using LiteFM.Abstractions;

namespace BetterLyrics.Core.Events
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
