namespace BetterLyrics.Core.Events
{
    public class LastFMIsAuthenticatedChangedEventArgs : EventArgs
    {
        public bool IsAuthenticated { get; set; }
        public LastFMIsAuthenticatedChangedEventArgs(bool isAuthenticated)
        {
            IsAuthenticated = isAuthenticated;
        }
    }
}
