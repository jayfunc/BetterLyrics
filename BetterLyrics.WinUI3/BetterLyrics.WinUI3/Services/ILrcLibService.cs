using System.Threading.Tasks;
using BetterLyrics.WinUI3.Enums;

namespace BetterLyrics.WinUI3.Services
{
    public interface ILrcLibService
    {
        Task<string?> SearchLyricsAsync(
            string title,
            string artist,
            string album,
            int duration,
            SearchMatchMode matchMode = SearchMatchMode.TitleAndArtist
        );
    }
}
