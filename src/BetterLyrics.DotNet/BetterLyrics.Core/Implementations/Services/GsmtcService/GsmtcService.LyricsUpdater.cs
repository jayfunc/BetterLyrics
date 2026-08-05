using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Entities;
using BetterLyrics.Core.Models.Lyrics;
using CommunityToolkit.Mvvm.ComponentModel;
using LyricsContentParser = BetterLyrics.Core.Helpers.Lyrics.ContentParser.LyricsContentParser;

namespace BetterLyrics.Core.Implementations.Services.GsmtcService;

public partial class GsmtcService : IGsmtcService
{
    private readonly Debouncer _lyricsDebouncer = new();

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial LyricsData? CurrentLyricsData { get; private set; }

    [ObservableProperty] public partial LyricsCacheItem? CurrentLyricsSearchResult { get; private set; }

    public void UpdateLyrics()
    {
        _ = _lyricsDebouncer.RunAsync(async token => await RefreshLyricsAsync(token));
    }

    private async Task RefreshLyricsAsync(CancellationToken token)
    {
        if (CurrentSongInfo != SongInfoExtensions.Placeholder)
        {
            var maxRetries = _settingsService.AppSettings.GeneralSettings.MaxAutoRetryCount;
            CurrentLyricsSearchResult = null;

            for (var attempt = 1; attempt <= maxRetries; attempt++)
            {
                CurrentLyricsData = LyricsData.GetLoadingPlaceholder(attempt, maxRetries);

                try
                {
                    CurrentLyricsSearchResult = await Task.Run(async () => await _lyrcsSearchService.SearchSmartlyAsync(
                        CurrentSongInfo, CurrentMediaSourceProviderInfo?.LyricsSearchType, token), token);

                    break;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception)
                {
                    if (attempt == maxRetries) break;

                    await Task.Delay((attempt * 2 - 1) * 1000, token);
                }
            }

            if (CurrentLyricsSearchResult != null)
            {
                var lyricsParser = new LyricsContentParser();

                CurrentLyricsData = await Task.Run(async () => await lyricsParser.ParseAsync(CurrentLyricsSearchResult, token), token);
            }
        }

        if (CurrentLyricsSearchResult == null) CurrentLyricsData = LyricsDataExtensions.NotFoundPlaceholder;
    }
}