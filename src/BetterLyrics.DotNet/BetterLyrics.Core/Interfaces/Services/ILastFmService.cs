using BetterLyrics.Core.Events;
using BetterLyrics.Core.Models;
using Hqub.Lastfm.Entities;

namespace BetterLyrics.Core.Interfaces.Services;

public interface ILastFmService
{
    User? User { get; }
    bool IsAuthenticated { get; }

    event EventHandler<LastFMUserChangedEventArgs>? UserChanged;
    event EventHandler<LastFMIsAuthenticatedChangedEventArgs>? IsAuthenticatedChanged;

    Task AuthAsync();
    Task ConfirmAuthAsync();
    Task UnAuthAsync();
    Task ConfirmUnAuthAsync();
    Task TrackAsync(SongInfo songInfo);
    Task RefreshAsync();
}