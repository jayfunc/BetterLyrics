using BetterLyrics.Core.Models;
using DiscordRPC;

namespace BetterLyrics.Core.Interfaces.Services;

public interface IDiscordService
{
    User? CurrentUser { get; }
    event EventHandler<User?>? UserChanged;
    void Enable();
    void Disable();
    Task UpdateRichPresenceAsync(SongInfo songInfo, bool isPlaying = true, TimeSpan? currentPosition = null, string? albumArtUrl = null);
}