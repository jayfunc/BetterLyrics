// 2025/6/23 by Zhe Fang

using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Domain;
using BetterLyrics.Core.Models.Entities;
using BetterLyrics.Core.Models.Lyrics;
using BetterLyrics.Core.Models.Settings;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.GSMTCService
{
    /// <summary>
    /// Interface for GlobalSystemMediaTransportControlsSession Service 
    /// </summary>
    public interface IGSMTCService : INotifyPropertyChanged
    {
        Task PlayAsync();
        Task PauseAsync();
        Task StopAsync();
        Task PreviousAsync();
        Task NextAsync();
        Task ChangePositionAsync(double seconds);
        Task ChangeLyricsLineAsync(int index);

        void UpdateLyrics();

        MediaSourceProviderInfo? CurrentMediaSourceProviderInfo { get; }

        bool IsScrobbled { get; }
        TimeSpan ScrobbledDuration { get; }
        TimeSpan TargetScrobbledDuration { get; }

        bool CurrentIsPlaying { get; }
        SongInfo CurrentSongInfo { get; }
        TimeSpan CurrentPosition { get; }
        LyricsData? CurrentLyricsData { get; }

        BitmapImage? AlbumArtBitmapImage { get; }
        byte[]? AlbumArtBytes { get; }

        Task<NowPlayingPalette> CalculateAlbumArtThemeColorsAsync(LyricsWindowStatus lyricsWindowStatus,
            AppColor backdropAccentColor, CancellationToken token = default);

        Task<List<AppColor>> GetAlbumArtAccentColorsAsync(PaletteGeneratorType paletteGeneratorType, bool isDark,
            CancellationToken token = default);

        LyricsCacheItem? CurrentLyricsSearchResult { get; }
    }
}