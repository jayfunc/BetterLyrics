using BetterLyrics.WinUI3.Models;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.SongSearchMapService
{
    public interface ISongSearchMapService
    {
        Task SaveMappingAsync(MappedSongSearchQuery mapping);
        Task<MappedSongSearchQuery?> TryGetMappingAsync(SongInfo songInfo);
        Task<(string Title, string Artist, string Album)> GetMappingAsync(SongInfo songInfo);
        Task DeleteMappingAsync(MappedSongSearchQuery mapping);
    }
}
