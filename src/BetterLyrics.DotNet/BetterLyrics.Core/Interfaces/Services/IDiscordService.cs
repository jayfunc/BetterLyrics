using BetterLyrics.Core.Models;

namespace BetterLyrics.Core.Interfaces.Services;

public interface IDiscordService
{
    void Enable();
    void Disable();
    Task UpdateRichPresenceAsync(SongInfo songInfo, bool isPlaying = true, TimeSpan? currentPosition = null);
}