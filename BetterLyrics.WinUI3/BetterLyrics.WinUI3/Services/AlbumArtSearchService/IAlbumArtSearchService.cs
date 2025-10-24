using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace BetterLyrics.WinUI3.Services.AlbumArtSearchService
{
    public interface IAlbumArtSearchService
    {
        Task<IBuffer?> SearchAsync(string mediaSessionId, string title, string artist, string album, IBuffer? bufferFromSMTC, CancellationToken token);
    }
}
