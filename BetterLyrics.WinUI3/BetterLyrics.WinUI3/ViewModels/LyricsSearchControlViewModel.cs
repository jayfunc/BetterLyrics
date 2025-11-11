using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.LyricsSearchService;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.Services.SettingsService;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsSearchControlViewModel : BaseViewModel
    {
        private readonly ILyricsSearchService _lyricsSearchService;
        private readonly IMediaSessionsService _mediaSessionsService;
        private readonly ISettingsService _settingsService;

        private LatestOnlyTaskRunner _lyricsSearchRunner = new();

        [ObservableProperty]
        public partial AppSettings AppSettings { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<LyricsSearchResult> LyricsSearchResults { get; set; } = [];

        [ObservableProperty]
        public partial LyricsSearchResult? SelectedLyricsSearchResult { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<LyricsData>? LyricsDataArr { get; set; }

        [ObservableProperty]
        public partial LyricsLine? SelectedLyricsLine { get; set; }

        [ObservableProperty]
        public partial MappedSongSearchQuery? MappedSongSearchQuery { get; set; }

        [ObservableProperty]
        public partial bool IsSearching { get; set; } = false;

        public LyricsSearchControlViewModel(ILyricsSearchService lyricsSearchService, IMediaSessionsService mediaSessionsService, ISettingsService settingsService)
        {
            _lyricsSearchService = lyricsSearchService;
            _mediaSessionsService = mediaSessionsService;
            _settingsService = settingsService;

            AppSettings = _settingsService.AppSettings;

            _mediaSessionsService.SongInfoChanged += MediaSessionsService_SongInfoChanged;

            InitMappedSongSearchQuery();
        }

        private void MediaSessionsService_SongInfoChanged(object? sender, Events.SongInfoChangedEventArgs e)
        {
            InitMappedSongSearchQuery();
        }

        private void InitMappedSongSearchQuery()
        {
            LyricsSearchResults.Clear();
            LyricsDataArr = null;
            if (_mediaSessionsService.SongInfo != null)
            {
                var found = GetMappedSongSearchQueryFromSettings();
                if (found == null)
                {
                    MappedSongSearchQuery = new MappedSongSearchQuery
                    {
                        OriginalTitle = _mediaSessionsService.SongInfo.Title,
                        OriginalArtist = _mediaSessionsService.SongInfo.Artist,
                        OriginalAlbum = _mediaSessionsService.SongInfo.Album,
                        MappedTitle = _mediaSessionsService.SongInfo.Title,
                        MappedArtist = _mediaSessionsService.SongInfo.Artist,
                        MappedAlbum = _mediaSessionsService.SongInfo.Album,
                    };
                }
                else
                {
                    MappedSongSearchQuery = found.Clone();
                }
            }
        }

        private MappedSongSearchQuery? GetMappedSongSearchQueryFromSettings()
        {
            if (_mediaSessionsService.SongInfo == null)
            {
                return null;
            }

            var found = AppSettings.MappedSongSearchQueries
                .Where(x => x.OriginalTitle == _mediaSessionsService.SongInfo.Title && x.OriginalArtist == _mediaSessionsService.SongInfo.Artist && x.OriginalAlbum == _mediaSessionsService.SongInfo.Album);

            return found.FirstOrDefault();
        }

        [RelayCommand]
        private void Search()
        {
            if (MappedSongSearchQuery == null)
            {
                return;
            }

            IsSearching = true;
            LyricsSearchResults.Clear();
            MappedSongSearchQuery.LyricsSearchProvider = null;
            _ = _lyricsSearchRunner.RunAsync(async (token) =>
            {
                LyricsSearchResults = [..await Task.Run(async () =>
                {
                    return await _lyricsSearchService.SearchAllAsync(
                        MappedSongSearchQuery.MappedTitle,
                        MappedSongSearchQuery.MappedArtist,
                        MappedSongSearchQuery.MappedAlbum,
                        _mediaSessionsService.SongInfo?.DurationMs ?? 0, token);
                }, token)];
                IsSearching = false;
            });
        }

        [RelayCommand]
        private void Save()
        {
            if (MappedSongSearchQuery == null)
            {
                return;
            }

            var existing = GetMappedSongSearchQueryFromSettings();
            if (existing != null)
            {
                AppSettings.MappedSongSearchQueries.Remove(existing);
            }
            AppSettings.MappedSongSearchQueries.Add(MappedSongSearchQuery);
            MappedSongSearchQuery = MappedSongSearchQuery.Clone();
        }

        [RelayCommand]
        private void Reset()
        {
            var existing = GetMappedSongSearchQueryFromSettings();
            if (existing != null)
            {
                AppSettings.MappedSongSearchQueries.Remove(existing);
            }
            InitMappedSongSearchQuery();
            SelectedLyricsSearchResult = null;
        }

        [RelayCommand]
        private void ResetMappedTitle()
        {
            MappedSongSearchQuery?.MappedTitle = MappedSongSearchQuery?.OriginalTitle ?? string.Empty;
        }

        [RelayCommand]
        private void ResetMappedArtist()
        {
            MappedSongSearchQuery?.MappedArtist = MappedSongSearchQuery?.OriginalArtist ?? string.Empty;
        }

        [RelayCommand]
        private void ResetMappedAlbum()
        {
            MappedSongSearchQuery?.MappedAlbum = MappedSongSearchQuery?.OriginalAlbum ?? string.Empty;
        }

        partial void OnSelectedLyricsSearchResultChanged(LyricsSearchResult? value)
        {
            MappedSongSearchQuery?.LyricsSearchProvider = value?.Provider;
            if (value?.Raw != null)
            {
                var lyricsParser = new LyricsParser();
                lyricsParser.Parse(
                   [MappedSongSearchQuery ?? new()],
                    MappedSongSearchQuery?.OriginalTitle ?? "",
                    MappedSongSearchQuery?.OriginalArtist ?? "",
                    MappedSongSearchQuery?.OriginalAlbum ?? "",
                    value?.Raw, (int?)_mediaSessionsService.SongInfo?.DurationMs, value?.Provider);
                LyricsDataArr = [.. lyricsParser.LyricsDataArr];
            }
            else
            {
                LyricsDataArr = null;
            }
        }

        partial void OnSelectedLyricsLineChanged(LyricsLine? value)
        {
            if (value?.StartMs == null)
            {
                return;
            }
            _mediaSessionsService.ChangePosition(value.StartMs / 1000.0);
        }
    }
}
