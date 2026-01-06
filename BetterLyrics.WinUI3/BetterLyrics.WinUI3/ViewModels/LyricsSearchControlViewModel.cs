using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Parsers.LyricsParser;
using BetterLyrics.WinUI3.Services.LyricsSearchService;
using BetterLyrics.WinUI3.Services.GSMTCService;
using BetterLyrics.WinUI3.Services.SettingsService;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsSearchControlViewModel : BaseViewModel,
        IRecipient<PropertyChangedMessage<SongInfo>>
    {
        private readonly ILyricsSearchService _lyricsSearchService;
        private readonly IGSMTCService _gsmtcService;
        private readonly ISettingsService _settingsService;

        private LatestOnlyTaskRunner _lyricsSearchRunner = new();

        [ObservableProperty]
        public partial AppSettings AppSettings { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<LyricsCacheItem> LyricsSearchResults { get; set; } = [];

        [ObservableProperty]
        public partial LyricsCacheItem? SelectedLyricsSearchResult { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<LyricsData>? LyricsDataArr { get; set; }

        [ObservableProperty]
        public partial MappedSongSearchQuery? MappedSongSearchQuery { get; set; }

        [ObservableProperty]
        public partial bool IsSearching { get; set; } = false;

        public LyricsSearchControlViewModel(ILyricsSearchService lyricsSearchService, IGSMTCService gsmtcService, ISettingsService settingsService)
        {
            _lyricsSearchService = lyricsSearchService;
            _gsmtcService = gsmtcService;
            _settingsService = settingsService;

            AppSettings = _settingsService.AppSettings;

            InitMappedSongSearchQuery();
        }

        private void InitMappedSongSearchQuery()
        {
            LyricsSearchResults.Clear();
            LyricsDataArr = null;
            if (_gsmtcService.CurrentSongInfo != null)
            {
                var found = GetMappedSongSearchQueryFromSettings();
                if (found == null)
                {
                    MappedSongSearchQuery = new MappedSongSearchQuery
                    {
                        OriginalTitle = _gsmtcService.CurrentSongInfo.Title,
                        OriginalArtist = _gsmtcService.CurrentSongInfo.Artist,
                        OriginalAlbum = _gsmtcService.CurrentSongInfo.Album,
                        MappedTitle = _gsmtcService.CurrentSongInfo.Title,
                        MappedArtist = _gsmtcService.CurrentSongInfo.Artist,
                        MappedAlbum = _gsmtcService.CurrentSongInfo.Album,
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
            if (_gsmtcService.CurrentSongInfo == null)
            {
                return null;
            }

            var found = AppSettings.MappedSongSearchQueries
                .FirstOrDefault(x =>
                    x.OriginalTitle == _gsmtcService.CurrentSongInfo.Title &&
                    x.OriginalArtist == _gsmtcService.CurrentSongInfo.Artist &&
                    x.OriginalAlbum == _gsmtcService.CurrentSongInfo.Album);

            return found;
        }

        public void PlayLyricsLine(LyricsLine? value)
        {
            if (value?.StartMs == null)
            {
                return;
            }
            _gsmtcService.ChangePosition(value.StartMs / 1000.0);
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
                    var result = await _lyricsSearchService.SearchAllAsync(
                        ((SongInfo)_gsmtcService.CurrentSongInfo.Clone())
                            .WithTitle(MappedSongSearchQuery.MappedTitle)
                            .WithArtist(MappedSongSearchQuery.MappedArtist)
                            .WithAlbum(MappedSongSearchQuery.MappedAlbum),
                        !_settingsService.AppSettings.GeneralSettings.IgnoreCacheWhenSearching,
                        token);
                    return result;
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

        partial void OnSelectedLyricsSearchResultChanged(LyricsCacheItem? value)
        {
            MappedSongSearchQuery?.LyricsSearchProvider = value?.Provider;
            if (value?.Raw != null)
            {
                var lyricsParser = new LyricsParser();
                LyricsDataArr = [.. lyricsParser.Parse(value)];
            }
            else
            {
                LyricsDataArr = null;
            }
        }

        public void Receive(PropertyChangedMessage<SongInfo> message)
        {
            if (message.Sender is IGSMTCService)
            {
                if (message.PropertyName == nameof(IGSMTCService.CurrentSongInfo))
                {
                    InitMappedSongSearchQuery();
                }
            }
        }
    }
}
