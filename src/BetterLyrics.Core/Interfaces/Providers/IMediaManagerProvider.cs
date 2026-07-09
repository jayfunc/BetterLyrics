namespace BetterLyrics.Core.Interfaces.Providers
{
    public interface IMediaManagerProvider
    {
        IMediaSessionProvider? FocusedSession { get; }
        IEnumerable<IMediaSessionProvider> CurrentMediaSessions { get; }

        delegate void SessionChangeDelegate(IMediaSessionProvider? mediaSession);

        event SessionChangeDelegate? OnAnySessionOpened;
        event SessionChangeDelegate? OnAnySessionClosed;
        event SessionChangeDelegate? OnFocusedSessionChanged;
        event SessionChangeDelegate? OnAnyMediaPropertyChanged;
        event SessionChangeDelegate? OnAnyPlaybackStateChanged;
        event SessionChangeDelegate? OnAnyTimelinePropertyChanged;

        void Init();
        bool IsMediaSessionExisting(string sessionId);
    }
}
