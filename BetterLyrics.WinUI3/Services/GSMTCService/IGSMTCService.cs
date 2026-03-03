// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Lyrics;
using BetterLyrics.WinUI3.Models.Settings;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI;

namespace BetterLyrics.WinUI3.Services.GSMTCService
{
    /// <summary>
    /// Interface for GlobalSystemMediaTransportControlsSession Service 
    /// </summary>
    public interface IGSMTCService : INotifyPropertyChanged
    {
        Task PlayAsync();
        Task PauseAsync();
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
        IRandomAccessStream? AlbumArtBitmapStream { get; }

        Task<NowPlayingPalette> CalculateAlbumArtThemeColorsAsync(LyricsWindowStatus lyricsWindowStatus, Color backdropAccentColor, CancellationToken token = default);
        Task<List<Color>> GetAlbumArtAccentColorsAsync(PaletteGeneratorType paletteGeneratorType, bool isDark, CancellationToken token = default);

        LyricsCacheItem? CurrentLyricsSearchResult { get; }
    }
}
