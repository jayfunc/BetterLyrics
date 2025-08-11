using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.LastFMService;
using BetterLyrics.WinUI3.Services.LibWatcherService;
using BetterLyrics.WinUI3.Services.LyricsSearchService;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Services.TranslateService;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace BetterLyrics.WinUI3.ViewModels.LyricsRendererViewModel
{
    public partial class LyricsRendererViewModel
    {
        public LyricsRendererViewModel(
            ISettingsService settingsService,
            IMediaSessionsService mediaSessionsService,
            ILyricsSearchService musicSearchService,
            ILibWatcherService libWatcherService,
            ITranslateService libreTranslateService,
            ILastFMService lastFMService
            )
        {
            _settingsService = settingsService;
            _lyrcsSearchService = musicSearchService;
            _mediaSessionsService = mediaSessionsService;
            _libWatcherService = libWatcherService;
            _translateService = libreTranslateService;

            _lastFMService = lastFMService;

            _logger = Ioc.Default.GetRequiredService<ILogger<LyricsRendererViewModel>>();

            _settingsService.AppSettings.MediaSourceProvidersInfo.ItemPropertyChanged += MediaSourceProvidersInfo_ItemPropertyChanged;
            _settingsService.AppSettings.LocalMediaFolders.CollectionChanged += LocalMediaFolders_CollectionChanged;
            _settingsService.AppSettings.LocalMediaFolders.ItemPropertyChanged += LocalMediaFolders_ItemPropertyChanged;

            _lyricsStyleSettings = _settingsService.AppSettings.StandardLyricsStyleSettings;
            _lyricsEffectSettings = _settingsService.AppSettings.StandardLyricsEffectSettings;

            _titleTextFormat.HorizontalAlignment = _artistTextFormat.HorizontalAlignment = _settingsService.AppSettings.AlbumArtLayoutSettings.SongInfoAlignmentType.ToCanvasHorizontalAlignment();

            _timelineSyncThreshold = 0;

            _displayType = _displayTypeReceived = _settingsService.AppSettings.GeneralSettings.DisplayType;

            _libWatcherService.MusicLibraryFilesChanged += LibWatcherService_MusicLibraryFilesChanged;

            _mediaSessionsService.IsPlayingChanged += PlaybackService_IsPlayingChanged;
            _mediaSessionsService.SongInfoChanged += PlaybackService_SongInfoChanged;
            _mediaSessionsService.AlbumArtChangedChanged += PlaybackService_AlbumArtChangedChanged;
            _mediaSessionsService.TimelineChanged += PlaybackService_TimelineChanged;

            _isPlaying = _mediaSessionsService.IsPlaying;

            UpdateColorConfig();
        }

        private void LocalMediaFolders_ItemPropertyChanged(object? sender, Extensions.ItemPropertyChangedEventArgs e)
        {
            // Music lib changed, re-fetch lyrics
            _logger.LogInformation("Local lyrics folders changed, refreshing lyrics.");
            _ = _refreshLyricsRunner.RunAsync(async tokne =>
            {
                await RefreshLyricsAsync(tokne);
            });
        }

        private void LocalMediaFolders_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Music lib changed, re-fetch lyrics
            _logger.LogInformation("Local lyrics folders changed, refreshing lyrics.");
            _ = _refreshLyricsRunner.RunAsync(async tokne =>
            {
                await RefreshLyricsAsync(tokne);
            });
        }

        private void MediaSourceProvidersInfo_ItemPropertyChanged(object? sender, Extensions.ItemPropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MediaSourceProviderInfo.LyricsSearchProvidersInfo):
                    _logger.LogInformation("MediaSourceProviderInfo.LyricsSearchProvidersInfo changed, refreshing lyrics.");
                    _ = _refreshLyricsRunner.RunAsync(async token =>
                    {
                        await RefreshLyricsAsync(token);
                    });
                    break;
                default:
                    break;
            }
        }
    }
}
