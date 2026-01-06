using BetterLyrics.WinUI3.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.SongSearchMapService
{
    public interface ISongSearchMapService
    {
        Task SaveMappingAsync(MappedSongSearchQuery mapping);
        Task<MappedSongSearchQuery?> GetMappingAsync(string title, string artist, string album);
        Task DeleteMappingAsync(MappedSongSearchQuery mapping);
    }
}
