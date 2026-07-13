using BetterLyrics.Core.Events;
using BetterLyrics.Core.Models;
using LiteFM.Abstractions;

namespace BetterLyrics.Core.Interfaces.Services;

public interface ILastFmService
{
    LastFMUser? User { get; }
    bool IsAuthenticated { get; }

    event EventHandler<LastFMUserChangedEventArgs>? UserChanged;
    event EventHandler<LastFMIsAuthenticatedChangedEventArgs>? IsAuthenticatedChanged;

    Task AuthAsync();
    Task ConfirmAuthAsync(string param);
    Task UnAuthAsync();
    Task ConfirmUnAuthAsync();
    Task TrackAsync(SongInfo songInfo);
    Task RefreshAsync();
}