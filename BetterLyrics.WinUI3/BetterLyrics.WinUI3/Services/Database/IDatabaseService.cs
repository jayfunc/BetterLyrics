using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Models;
using Windows.Media.Control;

namespace BetterLyrics.WinUI3.Services.Database
{
    public interface IDatabaseService
    {
        Task RebuildDatabaseAsync(IList<string> paths);

        Task<SongInfo> FindSongInfoAsync(
            GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProps
        );

        SongInfo FindSongInfo(SongInfo initSongInfo, string searchTitle, string searchArtist);
    }
}
