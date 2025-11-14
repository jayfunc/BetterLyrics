using BetterLyrics.WinUI3.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Services.DiscordService
{
    public interface IDiscordService
    {
        void UpdateRichPresence(SongInfo songInfo);
        void UpdateRichPresence(TimeSpan current, TimeSpan duration);
    }
}
