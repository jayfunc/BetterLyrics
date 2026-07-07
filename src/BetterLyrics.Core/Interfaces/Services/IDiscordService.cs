using BetterLyrics.Core.Models;

namespace BetterLyrics.Core.Interfaces.Services;

public interface IDiscordService
{
    void Enable();
    void Disable();
    void UpdateRichPresence(SongInfo songInfo);
}