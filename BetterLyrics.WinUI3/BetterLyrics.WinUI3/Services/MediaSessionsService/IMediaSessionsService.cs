// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Models;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.MediaSessionsService
{
    public interface IMediaSessionsService : INotifyPropertyChanged
    {
        event EventHandler<LyricsChangedEventArgs>? LyricsChanged;

        Task PlayAsync();
        Task PauseAsync();
        Task PreviousAsync();
        Task NextAsync();
        Task ChangePosition(double seconds);

        void UpdateLyrics();
        void UpdateTranslations();

        void InitPlaybackShortcuts();

        MediaSourceProviderInfo? CurrentMediaSourceProviderInfo { get; }

        bool CurrentIsPlaying { get; }
        SongInfo? CurrentSongInfo { get; }
        TimeSpan CurrentPosition { get; }
        LyricsData? CurrentLyricsData { get; }

        BitmapImage? AlbumArtBitmapImage { get; }
        AlbumArtThemeColors AlbumArtThemeColors { get; }

        TranslationSearchProvider? TranslationSearchProvider { get; }
        LyricsSearchResult? CurrentLyricsSearchResult { get; }
    }
}
