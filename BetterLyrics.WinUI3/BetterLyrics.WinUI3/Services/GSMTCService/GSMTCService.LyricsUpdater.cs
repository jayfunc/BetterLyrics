using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Parsers.LyricsParser;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.GSMTCService
{
    public partial class GSMTCService : IGSMTCService
    {
        private readonly DispatcherQueueTimer _refreshLyricsTimer;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsData? CurrentLyricsData { get; private set; }

        [ObservableProperty] public partial LyricsSearchResult? CurrentLyricsSearchResult { get; private set; }

        private async Task RefreshLyricsAsync()
        {
            _logger.LogInformation("RefreshLyricsAsync");

            CurrentLyricsSearchResult = null;
            CurrentLyricsData = LyricsData.GetLoadingPlaceholder();

            if (CurrentSongInfo != SongInfoExtensions.Placeholder)
            {
                CurrentLyricsSearchResult = await Task.Run(async () => await _lyrcsSearchService.SearchSmartlyAsync(
                    CurrentSongInfo, true, CurrentMediaSourceProviderInfo?.LyricsSearchType, CancellationToken.None));

                if (CurrentLyricsSearchResult != null)
                {
                    var lyricsParser = new LyricsParser();

                    (CurrentLyricsData, CurrentLyricsSearchResult.TransliterationProvider, CurrentLyricsSearchResult.TranslationProvider) =
                        await Task.Run(async () => await lyricsParser.Parse(
                            _translationService, _transliterationService, _settingsService.AppSettings.TranslationSettings, CurrentLyricsSearchResult,
                            CancellationToken.None));
                }
            }

            if (CurrentLyricsSearchResult == null)
            {
                CurrentLyricsData = LyricsData.GetNotfoundPlaceholder();
            }
        }

        public async void UpdateLyrics()
        {
            _refreshLyricsTimer.Debounce(async () =>
            {
                await RefreshLyricsAsync();
            }, Constants.Time.DebounceTimeout);
        }

    }
}
