using BetterLyrics.Core.Interfaces.Providers;
using System.Collections.Generic;

namespace BetterLyrics.Avalonia.Providers;

public class MediaManagerProvider : IMediaManagerProvider
{
    public IMediaSessionProvider? FocusedSession => throw new System.NotImplementedException();

    public IEnumerable<IMediaSessionProvider> CurrentMediaSessions => throw new System.NotImplementedException();

    public event IMediaManagerProvider.SessionChangeDelegate OnAnySessionOpened;
    public event IMediaManagerProvider.SessionChangeDelegate OnAnySessionClosed;
    public event IMediaManagerProvider.SessionChangeDelegate OnFocusedSessionChanged;
    public event IMediaManagerProvider.SessionChangeDelegate OnAnyMediaPropertyChanged;
    public event IMediaManagerProvider.SessionChangeDelegate OnAnyPlaybackStateChanged;
    public event IMediaManagerProvider.SessionChangeDelegate OnAnyTimelinePropertyChanged;

    public void Init()
    {
        throw new System.NotImplementedException();
    }

    public bool IsMediaSessionExisting(string sessionId)
    {
        throw new System.NotImplementedException();
    }
}
