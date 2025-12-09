using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Parsers.LyricsParser;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.MediaSessionsService
{
    public partial class MediaSessionsService : IMediaSessionsService
    {
        private LatestOnlyTaskRunner _refreshLyricsRunner = new();

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsData? CurrentLyricsData { get; private set; }

        [ObservableProperty] public partial LyricsSearchResult? CurrentLyricsSearchResult { get; private set; }

        private async Task RefreshLyricsAsync(CancellationToken token)
        {
            _logger.LogInformation("RefreshLyricsAsync");

            CurrentLyricsSearchResult = null;
            CurrentLyricsData = LyricsData.GetLoadingPlaceholder();

            if (CurrentSongInfo != null)
            {
                CurrentLyricsSearchResult = await Task.Run(async () => await _lyrcsSearchService.SearchSmartlyAsync(
                    CurrentSongInfo,
                    true,
                    CurrentMediaSourceProviderInfo?.LyricsSearchType,
                    token),
                token);
                if (token.IsCancellationRequested) return;

                if (CurrentLyricsSearchResult != null)
                {
                    var lyricsParser = new LyricsParser();

                    (CurrentLyricsData, CurrentLyricsSearchResult.TransliterationProvider, CurrentLyricsSearchResult.TranslationProvider) =
                        await Task.Run(async () => await lyricsParser.Parse(
                            _translateService,
                            _settingsService.AppSettings.TranslationSettings,
                            CurrentLyricsSearchResult,
                            token),
                    token);
                }
            }
        }

        public async void UpdateLyrics()
        {
            await _refreshLyricsRunner.RunAsync(async (token) =>
            {
                await RefreshLyricsAsync(token);
            });
        }

    }
}
