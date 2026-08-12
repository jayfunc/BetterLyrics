using Hqub.Lastfm.Entities;

namespace BetterLyrics.Core.Events;

public class LastFMUserChangedEventArgs : EventArgs
{
    public LastFMUserChangedEventArgs(User? user)
    {
        User = user;
    }

    public User? User { get; set; }
}