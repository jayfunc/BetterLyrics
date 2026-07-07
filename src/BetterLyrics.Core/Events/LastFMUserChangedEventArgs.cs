using LiteFM.Abstractions;

namespace BetterLyrics.Core.Events;

public class LastFMUserChangedEventArgs : EventArgs
{
    public LastFMUserChangedEventArgs(LastFMUser? user)
    {
        User = user;
    }

    public LastFMUser? User { get; set; }
}