using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Helper.Lyrics.LyricsContentParser;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Lyrics;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.GSMTCService
{
    public partial class GSMTCService : IGSMTCService
    {
        private LatestOnlyTaskRunner _refreshLyricsRunner = new();

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsData? CurrentLyricsData { get; private set; }

        [ObservableProperty] public partial LyricsCacheItem? CurrentLyricsSearchResult { get; private set; }

        private async Task RefreshLyricsAsync(CancellationToken token)
        {
            CurrentLyricsData = LyricsData.GetLoadingPlaceholder();

            if (CurrentSongInfo != SongInfoExtensions.Placeholder)
            {
                CurrentLyricsSearchResult = await Task.Run(async () => await _lyrcsSearchService.SearchSmartlyAsync(
                    CurrentSongInfo, CurrentMediaSourceProviderInfo?.LyricsSearchType, token), token);

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
            _ = _refreshLyricsRunner.RunAsync(RefreshLyricsAsync);
        }

    }
}
