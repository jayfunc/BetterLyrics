using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services
{
    public interface IAlbumArtSearchService
    {
        Task<byte[]?> SearchAsync(string title, string artist, string album, byte[]? bytesFromSMTC = null);
    }
}
