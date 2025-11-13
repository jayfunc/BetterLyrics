using System;

namespace BetterLyrics.WinUI3.Events
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
