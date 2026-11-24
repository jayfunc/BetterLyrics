using BetterLyrics.Core.Events;
using BetterLyrics.Core.Models;
using Hqub.Lastfm.Entities;

namespace BetterLyrics.Core.Interfaces.Services;

public interface ILastFmService
{
    User? User { get; }
    bool IsAuthenticated { get; }

    /// <summary>
    /// Whether the user has provided their own Last.fm API key pair.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// The Last.fm API key provided by the user, if any.
    /// </summary>
    string? ApiKey { get; }

    /// <summary>
    /// The Last.fm shared secret provided by the user, if any.
    /// </summary>
    string? SharedSecret { get; }

    event EventHandler<LastFMUserChangedEventArgs>? UserChanged;
    event EventHandler<LastFMIsAuthenticatedChangedEventArgs>? IsAuthenticatedChanged;

    /// <summary>
    /// Stores the user provided API credentials. Passing empty values removes them.
    /// </summary>
    Task ConfigureAsync(string? apiKey, string? sharedSecret);

    Task AuthAsync();
    Task ConfirmAuthAsync();
    Task UnAuthAsync();
    Task ConfirmUnAuthAsync();
    Task TrackAsync(SongInfo songInfo);
    Task UpdateNowPlayingAsync(SongInfo songInfo);
    Task RefreshAsync();
    Task<string?> GetAlbumArtUrlAsync(SongInfo songInfo);
}
