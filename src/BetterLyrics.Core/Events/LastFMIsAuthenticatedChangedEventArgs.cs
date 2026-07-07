namespace BetterLyrics.Core.Events;

public class LastFMIsAuthenticatedChangedEventArgs : EventArgs
{
    public LastFMIsAuthenticatedChangedEventArgs(bool isAuthenticated)
    {
        IsAuthenticated = isAuthenticated;
    }

    public bool IsAuthenticated { get; set; }
}