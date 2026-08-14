using System.Threading.Tasks;
using BetterLyrics.Core.Models;

namespace BetterLyrics.Core.Interfaces.Providers;

public interface INowPlayingToastProvider
{
    void Initialize();
    Task ShowAsync(SongInfo song, byte[]? albumArtBytes);
}
