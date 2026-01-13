using BetterLyrics.WinUI3.Models;

namespace BetterLyrics.WinUI3.Services.DiscordService
{
    public interface IDiscordService
    {
        void Enable();
        void Disable();
        void UpdateRichPresence(SongInfo songInfo);
    }
}
