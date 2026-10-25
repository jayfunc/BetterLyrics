using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Messages.Playback;
using BetterLyrics.Core.Models.Entities;
using BetterLyrics.Core.Models.Lyrics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using LyricsContentParser = BetterLyrics.Core.Helpers.Lyrics.ContentParser.LyricsContentParser;

namespace BetterLyrics.Core.Implementations.Services.GsmtcService;

public partial class GsmtcService : IGsmtcService
{
    private readonly Debouncer _lyricsDebouncer = new();
    private readonly Debouncer _lyricsPresentationDebouncer = new();

    /// <summary>
    /// 标记本次 <see cref="CurrentLyricsData"/> 变更是否只是译文（翻译 / 音译）的显示变化
    /// </summary>
    private bool _isPresentationUpdate;

    [ObservableProperty]
    public partial LyricsData? CurrentLyricsData { get; private set; }

    partial void OnCurrentLyricsDataChanged(LyricsData? value) =>
        WeakReferenceMessenger.Default.Send(new CurrentLyricsDataChangedMessage(value, _isPresentationUpdate));

    [ObservableProperty] public partial LyricsCacheItem? CurrentLyricsSearchResult { get; private set; }

    public void UpdateLyrics()
    {
        _ = _lyricsDebouncer.RunAsync(async token => await RefreshLyricsAsync(token));
    }

    public void UpdateLyricsPresentation()
    {
        var searchResult = CurrentLyricsSearchResult;
        if (searchResult == null)
        {
            // 没有缓存结果（例如尚未成功获取歌词）时退回到完整刷新
            UpdateLyrics();
            return;
        }

        _ = _lyricsPresentationDebouncer.RunAsync(async token =>
        {
            var lyricsParser = new LyricsContentParser();
            var lyricsData = await Task.Run(async () => await lyricsParser.ParseAsync(searchResult, token), token);

            token.ThrowIfCancellationRequested();

            // 解析期间歌曲或歌词源已经变化，本次结果作废
            if (!ReferenceEquals(CurrentLyricsSearchResult, searchResult)) return;

            _isPresentationUpdate = true;
            try
            {
                CurrentLyricsData = lyricsData;
            }
            finally
            {
                _isPresentationUpdate = false;
            }
        });
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
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Lyrics search attempt {Attempt}/{MaxRetries} failed for {Title} - {Artist}.",
                        attempt, maxRetries, CurrentSongInfo.Title, CurrentSongInfo.Artist);

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
        else
        {
            CurrentLyricsSearchResult = null;
        }

        if (CurrentLyricsSearchResult == null) CurrentLyricsData = LyricsDataExtensions.NotFoundPlaceholder;
    }
}