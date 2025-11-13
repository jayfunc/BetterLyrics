using BetterLyrics.WinUI3.Models;

namespace BetterLyrics.WinUI3.Services.LiveStatesService
{
    public interface ILiveStatesService
    {
        LiveStates LiveStates { get; set; }
        void InitLyricsWindowStatus();
    }
}
