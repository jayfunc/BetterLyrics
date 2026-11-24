namespace BetterLyrics.Core.Events;

public class ListenBrainzAuthenticationChangedEventArgs : EventArgs
{
    public ListenBrainzAuthenticationChangedEventArgs(bool isAuthenticated, string? userName)
    {
        IsAuthenticated = isAuthenticated;
        UserName = userName;
    }

    public bool IsAuthenticated { get; }

    public string? UserName { get; }
}
