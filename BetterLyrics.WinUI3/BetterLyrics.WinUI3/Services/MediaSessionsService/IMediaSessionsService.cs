// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Models;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.UI;

namespace BetterLyrics.WinUI3.Services.MediaSessionsService
{
    public interface IMediaSessionsService : INotifyPropertyChanged
    {
        Task PlayAsync();
        Task PauseAsync();
        Task PreviousAsync();
        Task NextAsync();
        Task ChangePosition(double seconds);
        Task ChangeLyricsLine(int index);

        void UpdateLyrics();

        MediaSourceProviderInfo? CurrentMediaSourceProviderInfo { get; }

        bool CurrentIsPlaying { get; }
        SongInfo? CurrentSongInfo { get; }
        TimeSpan CurrentPosition { get; }
        LyricsData? CurrentLyricsData { get; }

        BitmapImage? AlbumArtBitmapImage { get; }

        AlbumArtThemeColors CalculateAlbumArtThemeColors(LyricsWindowStatus lyricsWindowStatus, Color backdropAccentColor);

        LyricsSearchResult? CurrentLyricsSearchResult { get; }
    }
}
