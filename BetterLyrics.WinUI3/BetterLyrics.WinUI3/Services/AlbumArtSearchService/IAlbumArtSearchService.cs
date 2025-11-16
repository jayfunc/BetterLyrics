using BetterLyrics.WinUI3.Models;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace BetterLyrics.WinUI3.Services.AlbumArtSearchService
{
    public interface IAlbumArtSearchService
    {
        Task<IBuffer?> SearchAsync(SongInfo songInfo, IBuffer? bufferFromSMTC, CancellationToken token);
    }
}
