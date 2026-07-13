using System.Collections.ObjectModel;
using System.Net;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Entities;
using BetterLyrics.Core.Models.Lyrics;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LyricsContentParser = BetterLyrics.Core.Helpers.Lyrics.ContentParser.LyricsContentParser;

namespace BetterLyrics.Core.ViewModels;

public partial class LyricsSearchControlViewModel : BaseViewModel,
    IRecipient<PropertyChangedMessage<SongInfo>>
{
    private readonly IAppUIThreadProvider _appUIThreadProvider;
    private readonly IGlobalToastProvider _globalToastProvider;
    private readonly ILyricsSearchService _lyricsSearchService;
    private readonly ISettingsService _settingsService;
    private readonly ISongSearchMapService _songSearchMapService;

    public LyricsSearchControlViewModel(
        ILyricsSearchService lyricsSearchService,
        IGsmtcService gsmtcService,
        ISettingsService settingsService,
        ISongSearchMapService songSearchMapService, IAppUIThreadProvider appUiThreadProvider,
        IGlobalToastProvider globalToastProvider)
    {
        _lyricsSearchService = lyricsSearchService;
        _settingsService = settingsService;
        _songSearchMapService = songSearchMapService;
        _appUIThreadProvider = appUiThreadProvider;
        _globalToastProvider = globalToastProvider;

        GsmtcService = gsmtcService;
        AppSettings = _settingsService.AppSettings;

        _ = InitMappedSongSearchQueryAsync();
    }

    public IGsmtcService GsmtcService { get; set; }

    [ObservableProperty] public partial AppSettings AppSettings { get; set; }

    [ObservableProperty] public partial ObservableCollection<LyricsCacheItem> LyricsSearchResults { get; set; } = [];

    [ObservableProperty] public partial LyricsCacheItem? SelectedLyricsSearchResult { get; set; }

    [ObservableProperty] public partial ObservableCollection<LyricsData>? LyricsDataArr { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial MappedSongSearchQuery? MappedSongSearchQuery { get; set; }

    [ObservableProperty] public partial bool IsSearching { get; set; } = false;

    public void Receive(PropertyChangedMessage<SongInfo> message)
    {
        if (message.Sender is IGsmtcService)
            if (message.PropertyName == nameof(IGsmtcService.CurrentSongInfo))
                _ = InitMappedSongSearchQueryAsync();
    }

    private async Task InitMappedSongSearchQueryAsync()
    {
        LyricsSearchResults.Clear();
        LyricsDataArr = null;
        if (GsmtcService.CurrentSongInfo != null)
        {
            var found = await _songSearchMapService.TryGetMappingAsync(GsmtcService.CurrentSongInfo);

            if (found == null)
                MappedSongSearchQuery = new MappedSongSearchQuery
                {
                    OriginalTitle = GsmtcService.CurrentSongInfo.Title,
                    OriginalArtist = GsmtcService.CurrentSongInfo.Artist,
                    OriginalAlbum = GsmtcService.CurrentSongInfo.Album,
                    MappedTitle = GsmtcService.CurrentSongInfo.Title,
                    MappedArtist = GsmtcService.CurrentSongInfo.Artist,
                    MappedAlbum = GsmtcService.CurrentSongInfo.Album
                };
            else
                MappedSongSearchQuery = (MappedSongSearchQuery)found.Clone();
        }
    }

    public void PlayLyricsLine(LyricsLine? value)
    {
        if (value?.StartMs == null) return;

        _ = GsmtcService.ChangePositionAsync(value.StartMs / 1000.0);
    }

    [RelayCommand]
    private void Search()
    {
        if (MappedSongSearchQuery == null) return;

        IsSearching = true;
        LyricsSearchResults.Clear();
        MappedSongSearchQuery.LyricsSearchProvider = null;

        var activeProviders = _lyricsSearchService.GetActiveProviders();
        foreach (var provider in activeProviders)
            LyricsSearchResults.Add(new LyricsCacheItem
            {
                Provider = provider,
                IsSearching = true
            });

        _ = Task.Run(async () =>
        {
            try
            {
                var songInfo = ((SongInfo)GsmtcService.CurrentSongInfo.Clone())
                    .WithTitle(MappedSongSearchQuery.MappedTitle)
                    .WithArtist(MappedSongSearchQuery.MappedArtist)
                    .WithAlbum(MappedSongSearchQuery.MappedAlbum);

                var checkCache = !_settingsService.AppSettings.GeneralSettings.IgnoreCacheWhenSearching;

                await foreach (var item in _lyricsSearchService.SearchAllAsync(songInfo, checkCache))
                    _appUIThreadProvider.Execute(() =>
                    {
                        var index = -1;
                        for (var i = 0; i < LyricsSearchResults.Count; i++)
                            if (LyricsSearchResults[i].Provider == item.Provider)
                            {
                                index = i;
                                break;
                            }

                        if (index != -1)
                        {
                            item.IsSearching = false;
                            LyricsSearchResults[index] = item;
                        }
                    });
            }
            finally
            {
                _appUIThreadProvider.Execute(() =>
                {
                    for (var i = LyricsSearchResults.Count - 1; i >= 0; i--)
                        if (LyricsSearchResults[i].IsSearching)
                            LyricsSearchResults[i].IsSearching = false;

                    IsSearching = false;
                });
            }
        });
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (MappedSongSearchQuery == null) return;

        await _songSearchMapService.SaveMappingAsync(MappedSongSearchQuery);
        MappedSongSearchQuery = (MappedSongSearchQuery)MappedSongSearchQuery.Clone();
        GsmtcService.UpdateLyrics();
    }

    [RelayCommand]
    private async Task ResetAsync()
    {
        if (MappedSongSearchQuery == null) return;

        await _songSearchMapService.DeleteMappingAsync(MappedSongSearchQuery);
        await InitMappedSongSearchQueryAsync();
        SelectedLyricsSearchResult = null;
        GsmtcService.UpdateLyrics();
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
    private async Task CopySearchLinkAsync()
    {
        var uriString = $"betterlyrics://lyrics/search/" +
                        $"title={WebUtility.UrlEncode(MappedSongSearchQuery?.MappedTitle)}&" +
                        $"artist={WebUtility.UrlEncode(MappedSongSearchQuery?.MappedArtist)}&" +
                        $"album={WebUtility.UrlEncode(MappedSongSearchQuery?.MappedAlbum)}";
        try
        {
            await TextCopy.ClipboardService.SetTextAsync(uriString);

            _globalToastProvider.Show("ActionCompleted", null, MessageSeverity.Success);
        }
        catch (Exception ex)
        {
            _globalToastProvider.Show("Error", ex.Message, MessageSeverity.Error);
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
}