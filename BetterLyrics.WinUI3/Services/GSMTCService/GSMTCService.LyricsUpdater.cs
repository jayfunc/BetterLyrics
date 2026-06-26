using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Models.Entities;
using BetterLyrics.Core.Models.Lyrics;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading;
using System.Threading.Tasks;
using LyricsContentParser = BetterLyrics.Core.Helpers.Lyrics.ContentParser.LyricsContentParser;

namespace BetterLyrics.WinUI3.Services.GSMTCService
{
    public partial class GSMTCService : IGSMTCService
    {
        private Debouncer _lyricsDebouncer = new();

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LyricsData? CurrentLyricsData { get; private set; }

        [ObservableProperty]
        public partial LyricsCacheItem? CurrentLyricsSearchResult { get; private set; }

        private async Task RefreshLyricsAsync(CancellationToken token)
        {
            if (CurrentSongInfo != SongInfoExtensions.Placeholder)
            {
                int maxRetries = 3;
                CurrentLyricsSearchResult = null;

                for (int attempt = 1; attempt <= maxRetries; attempt++)
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
                        if (attempt == maxRetries)
                        {
                            break;
                        }

                        await Task.Delay((attempt * 2 - 1) * 1000, token);
                    }
                }

                if (CurrentLyricsSearchResult != null)
                {
                    var lyricsParser = new LyricsContentParser();

                    (CurrentLyricsData, CurrentLyricsSearchResult.TransliterationProvider, CurrentLyricsSearchResult.TranslationProvider) =
                        await Task.Run(async () => await lyricsParser.ParseAsync(
                            _translationService, _transliterationService, _settingsService.AppSettings.TranslationSettings, CurrentLyricsSearchResult, token), token);
                }
            }

            if (CurrentLyricsSearchResult == null)
            {
                CurrentLyricsData = LyricsData.GetNotfoundPlaceholder();
            }
        }

        public void UpdateLyrics()
        {
            _ = _lyricsDebouncer.RunAsync(async (token) => await RefreshLyricsAsync(token));
        }
    }
}