using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Helper.Lyrics.LyricsContentParser;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Lyrics;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.GSMTCService;
using BetterLyrics.WinUI3.Services.LyricsSearchService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Services.SongSearchMapService;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsSearchControlViewModel : BaseViewModel,
        IRecipient<PropertyChangedMessage<SongInfo>>
    {
        private readonly ILyricsSearchService _lyricsSearchService;
        private readonly ISettingsService _settingsService;
        private readonly ISongSearchMapService _songSearchMapService;

        public IGSMTCService GSMTCService { get; set; }

        [ObservableProperty] public partial AppSettings AppSettings { get; set; }

        [ObservableProperty] public partial ObservableCollection<LyricsCacheItem> LyricsSearchResults { get; set; } = [];

        [ObservableProperty] public partial LyricsCacheItem? SelectedLyricsSearchResult { get; set; }

        [ObservableProperty] public partial ObservableCollection<LyricsData>? LyricsDataArr { get; set; }

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial MappedSongSearchQuery? MappedSongSearchQuery { get; set; }

        [ObservableProperty] public partial bool IsSearching { get; set; } = false;

        public LyricsSearchControlViewModel(
            ILyricsSearchService lyricsSearchService,
            IGSMTCService gsmtcService,
            ISettingsService settingsService,
            ISongSearchMapService songSearchMapService
        )
        {
            _lyricsSearchService = lyricsSearchService;
            _settingsService = settingsService;
            _songSearchMapService = songSearchMapService;

            GSMTCService = gsmtcService;
            AppSettings = _settingsService.AppSettings;

            _ = InitMappedSongSearchQueryAsync();
        }

        private async Task InitMappedSongSearchQueryAsync()
        {
            LyricsSearchResults.Clear();
            LyricsDataArr = null;
            if (GSMTCService.CurrentSongInfo != null)
            {
                var found = await _songSearchMapService.TryGetMappingAsync(GSMTCService.CurrentSongInfo);

                if (found == null)
                {
                    MappedSongSearchQuery = new MappedSongSearchQuery
                    {
                        OriginalTitle = GSMTCService.CurrentSongInfo.Title,
                        OriginalArtist = GSMTCService.CurrentSongInfo.Artist,
                        OriginalAlbum = GSMTCService.CurrentSongInfo.Album,
                        MappedTitle = GSMTCService.CurrentSongInfo.Title,
                        MappedArtist = GSMTCService.CurrentSongInfo.Artist,
                        MappedAlbum = GSMTCService.CurrentSongInfo.Album,
                    };
                }
                else
                {
                    MappedSongSearchQuery = (MappedSongSearchQuery)found.Clone();
                }
            }
        }

        public void PlayLyricsLine(LyricsLine? value)
        {
            if (value?.StartMs == null)
            {
                return;
            }
            _ = GSMTCService.ChangePositionAsync(value.StartMs / 1000.0);
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

            var activeProviders = _lyricsSearchService.GetActiveProviders();
            foreach (var provider in activeProviders)
            {
                LyricsSearchResults.Add(new LyricsCacheItem
                {
                    Provider = provider,
                    IsSearching = true
                });
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    var songInfo = ((SongInfo)GSMTCService.CurrentSongInfo.Clone())
                        .WithTitle(MappedSongSearchQuery.MappedTitle)
                        .WithArtist(MappedSongSearchQuery.MappedArtist)
                        .WithAlbum(MappedSongSearchQuery.MappedAlbum);

                    var checkCache = !_settingsService.AppSettings.GeneralSettings.IgnoreCacheWhenSearching;

                    await foreach (var item in _lyricsSearchService.SearchAllAsync(songInfo, checkCache))
                    {
                        _dispatcherQueue.TryEnqueue(() =>
                        {
                            var index = -1;
                            for (int i = 0; i < LyricsSearchResults.Count; i++)
                            {
                                if (LyricsSearchResults[i].Provider == item.Provider)
                                {
                                    index = i;
                                    break;
                                }
                            }

                            if (index != -1)
                            {
                                item.IsSearching = false;
                                LyricsSearchResults[index] = item;
                            }
                        });
                    }
                }
                finally
                {
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        for (int i = LyricsSearchResults.Count - 1; i >= 0; i--)
                        {
                            if (LyricsSearchResults[i].IsSearching)
                            {
                                LyricsSearchResults[i].IsSearching = false;
                            }
                        }

                        IsSearching = false;
                    });
                }
            });
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (MappedSongSearchQuery == null)
            {
                return;
            }

            await _songSearchMapService.SaveMappingAsync(MappedSongSearchQuery);
            MappedSongSearchQuery = (MappedSongSearchQuery)MappedSongSearchQuery.Clone();
            GSMTCService.UpdateLyrics();
        }

        [RelayCommand]
        private async Task ResetAsync()
        {
            if (MappedSongSearchQuery == null) return;

            await _songSearchMapService.DeleteMappingAsync(MappedSongSearchQuery);
            await InitMappedSongSearchQueryAsync();
            SelectedLyricsSearchResult = null;
            GSMTCService.UpdateLyrics();
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

        [RelayCommand]
        private void CopySearchLink()
        {
            var uriString = $"betterlyrics://lyrics/search/" +
                $"title={MappedSongSearchQuery?.MappedTitle}&" +
                $"artist={MappedSongSearchQuery?.MappedArtist}&" +
                $"album={MappedSongSearchQuery?.MappedAlbum}";
            try
            {
                DataPackage dataPackage = new();
                dataPackage.SetText(Uri.EscapeDataString(uriString));
                Clipboard.SetContent(dataPackage);

                GlobalToastManager.Show("ActionCompleted", null, InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                GlobalToastManager.Show("Error", ex.Message, InfoBarSeverity.Error);
                return;
            }
        }

        partial void OnSelectedLyricsSearchResultChanged(LyricsCacheItem? value)
        {
            MappedSongSearchQuery?.LyricsSearchProvider = value?.Provider;
            if (value?.Raw != null)
            {
                var lyricsParser = new LyricsContentParser();
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
                    _ = InitMappedSongSearchQueryAsync();
                }
            }
        }
    }
}
