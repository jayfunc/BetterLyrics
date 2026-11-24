using BetterLyrics.Core.Events;
using BetterLyrics.Core.Models;

namespace BetterLyrics.Core.Interfaces.Services;

public interface IListenBrainzService
{
    /// <summary>
    /// The ListenBrainz user name associated with the stored token, if it has been validated.
    /// </summary>
    string? Username { get; }

    /// <summary>
    /// The stored ListenBrainz user token, if any.
    /// </summary>
    string? UserToken { get; }

    /// <summary>
    /// Whether a ListenBrainz user token is stored.
    /// </summary>
    bool IsAuthenticated { get; }

    event EventHandler<ListenBrainzAuthenticationChangedEventArgs>? AuthenticationChanged;

    /// <summary>
    /// Validates and stores a user token. Passing an empty value removes the stored token.
    /// </summary>
    /// <returns>True when the token was accepted (or removed), false when it is invalid or could not be validated.</returns>
    Task<bool> SaveTokenAsync(string? token);

    Task ClearTokenAsync();

    /// <summary>
    /// Re-validates the stored token and refreshes the user name.
    /// </summary>
    Task RefreshAsync();

    Task UpdateNowPlayingAsync(SongInfo songInfo);
    Task TrackAsync(SongInfo songInfo);
}
