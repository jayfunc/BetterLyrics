using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Lyricify.Lyrics.Helpers.General;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.MediaSessionsService
{
    public partial class MediaSessionsService : IMediaSessionsService
    {
        private LatestOnlyTaskRunner _refreshLyricsRunner = new();
        private LatestOnlyTaskRunner _refreshTranslationRunner = new();

        private int _langIndex = 0;
        private List<LyricsData> _lyricsDataArr = [];

        public LyricsData? CurrentLyricsData => _lyricsDataArr.ElementAtOrDefault(_langIndex);

        public event EventHandler<LyricsChangedEventArgs>? LyricsChanged;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsSearchProvider? LyricsSearchProvider { get; private set; }

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial TranslationSearchProvider? TranslationSearchProvider { get; private set; }

        [ObservableProperty] public partial bool IsTranslating { get; set; } = false;

        private async Task RefreshTranslationAsync(CancellationToken token)
        {
            TranslationSearchProvider = null;
            _lyricsDataArr.ElementAtOrDefault(0)?.ClearTranslatedText();

            _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                LyricsChanged?.Invoke(this, new LyricsChangedEventArgs(CurrentLyricsData));
            });

            IsTranslating = true;

            await SetPhoneticTextAsync(token);
            await SetTranslatedTextAsync(token);
            if (token.IsCancellationRequested) return;

            IsTranslating = false;

            _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                LyricsChanged?.Invoke(this, new LyricsChangedEventArgs(CurrentLyricsData));
            });
        }

        private async Task SetTranslatedTextAsync(CancellationToken token)
        {
            if (!_settingsService.AppSettings.TranslationSettings.IsTranslationEnabled) return;

            _logger.LogInformation("Showing translation for lyrics...");
            string targetLangCode = _settingsService.AppSettings.TranslationSettings.SelectedTargetLanguageCode;
            _logger.LogInformation("Target language code: {TargetLangCode}", targetLangCode);
            string? originalText = _lyricsDataArr.FirstOrDefault()?.WrappedOriginalText;
            if (originalText == null) return;

            string? originalLangCode = LanguageHelper.DetectLanguageCode(originalText);
            _logger.LogInformation("Original language code: {OriginalLangCode}", originalLangCode);

            if (originalLangCode == targetLangCode)
            {
                _logger.LogInformation("Original lyrics already in target language: {TargetLangCode}", targetLangCode);

                _lyricsDataArr[0].ClearTranslatedText();
            }
            else
            {
                // Try get translation from itself first
                int found = _translateService.SearchTranslatedLyricsItself(_lyricsDataArr, targetLangCode);
                if (found >= 0)
                {
                    _logger.LogInformation("Found translated text in lyrics data at index {FoundIndex}", found);

                    _lyricsDataArr[0].SetTranslatedText(_lyricsDataArr[found], _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.LyricsTranslationSeparator, 50);
                    TranslationSearchProvider = LyricsSearchProvider.ToTranslationSearchProvider();
                }
                else if (_settingsService.AppSettings.TranslationSettings.IsLibreTranslateEnabled)
                {
                    _logger.LogInformation("LibreTranslate is enabled, trying to translate lyrics...");
                    string translated = string.Empty;
                    try
                    {
                        translated = await _translateService.TranslateTextAsync(originalText, targetLangCode, token);
                        if (token.IsCancellationRequested) return;
                        if (translated == string.Empty) return;

                        _lyricsDataArr[0].SetTranslation(translated, _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.LyricsTranslationSeparator);

                        TranslationSearchProvider = Enums.TranslationSearchProvider.LibreTranslate;
                    }
                    catch (Exception)
                    {
                        DevWinUI.Growl.Error(_resourceService.GetLocalizedString("LibreTranslateFailed")!);
                    }
                }
            }
        }

        private async Task SetPhoneticTextAsync(CancellationToken token)
        {
            _logger.LogInformation("Showing phonetic text for lyrics...");
            string targetPhoneticCode = "";
            _logger.LogInformation("Target phonetic code: {TargetPhonetic}", targetPhoneticCode);
            string? originalText = _lyricsDataArr.FirstOrDefault()?.WrappedOriginalText;
            if (originalText == null) return;

            string? originalLangCode = LanguageHelper.DetectLanguageCode(originalText);
            _logger.LogInformation("Original phonetic code: {OriginalLangCode}", originalLangCode);

            if (originalLangCode == "zh" && _settingsService.AppSettings.TranslationSettings.IsChineseRomanizationEnabled)
            {
                targetPhoneticCode = _settingsService.AppSettings.TranslationSettings.ChineseRomanization.ToPhoneticCode();
            }
            else if (originalLangCode == "ja" && _settingsService.AppSettings.TranslationSettings.IsJapaneseRomanizationEnabled)
            {
                targetPhoneticCode = PhoneticHelper.RomajiCode;
            }

            if (targetPhoneticCode == "")
            {
                _lyricsDataArr[0].ClearPhoneticText();
            }

            // Try get phonetic text from itself
            int found = _translateService.SearchTranslatedLyricsItself(_lyricsDataArr, targetPhoneticCode);
            if (found >= 0)
            {
                _logger.LogInformation("Found phonetic text in lyrics data at index {FoundIndex}", found);
                _lyricsDataArr[0].SetPhoneticText(_lyricsDataArr[found], _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.LyricsTranslationSeparator, 50);
            }

        }

        private async Task RefreshLyricsAsync(CancellationToken token)
        {
            _logger.LogInformation("Refreshing lyrics...");

            LyricsSearchProvider = null;
            _lyricsDataArr = [LyricsData.GetLoadingPlaceholder()];

            _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                LyricsChanged?.Invoke(this, new LyricsChangedEventArgs(CurrentLyricsData));
            });

            if (CurrentSongInfo != null)
            {
                _logger.LogInformation("Searching lyrics for: Title={Title}, Artist={Artist}, Album={Album}, DurationMs={DurationMs}",
                    CurrentSongInfo.Title, CurrentSongInfo.Artist, CurrentSongInfo.Album, CurrentSongInfo.DurationMs);

                var lyricsSearchResult = await Task.Run(async () => await _lyrcsSearchService.SearchSmartlyAsync(
                    CurrentSongInfo.PlayerId ?? "",
                    CurrentSongInfo.Title,
                    CurrentSongInfo.Artist,
                    CurrentSongInfo.Album,
                    CurrentSongInfo.DurationMs,
                    CurrentSongInfo.SongId,
                    token
                ), token);
                if (token.IsCancellationRequested) return;
                LyricsSearchProvider = lyricsSearchResult?.Provider;

                _logger.LogInformation("Lyrics was found? {Found}, Provider: {LyricsSearchProvider}", lyricsSearchResult?.IsFound, LyricsSearchProvider);

                var lyricsParser = new LyricsParser();
                lyricsParser.Parse(
                    _settingsService.AppSettings.MappedSongSearchQueries.ToList(),
                    CurrentSongInfo.Title, CurrentSongInfo.Artist, CurrentSongInfo.Album, lyricsSearchResult?.Raw, (int?)CurrentSongInfo?.DurationMs, LyricsSearchProvider);
                _lyricsDataArr = lyricsParser.LyricsDataArr;
                ApplyChinesePreference();
            }
            else
            {
                _logger.LogWarning("SongInfo is null, cannot search lyrics.");
            }

            _logger.LogInformation("Parsed lyrics: {MultiLangLyricsCount} languages", _lyricsDataArr.Count);

            // Show original first while loading phonetic and translated
            ApplyChinesePreference();

            _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                LyricsChanged?.Invoke(this, new LyricsChangedEventArgs(CurrentLyricsData));
            });

            UpdateTranslations();
        }

        private void ApplyChinesePreference()
        {
            var traditionalChinesePreferred = _settingsService.AppSettings.TranslationSettings.IsTraditionalChineseEnabled;
            var found = _lyricsDataArr.FindIndex(x => x.LanguageCode == "zh");
            if (found >= 0)
            {
                foreach (var item in _lyricsDataArr[found].LyricsLines)
                {
                    item.OriginalText = traditionalChinesePreferred ? ChineseHelper.ToTC(item.OriginalText) : ChineseHelper.ToSC(item.OriginalText);
                }
            }
        }

        public void UpdateLyrics()
        {
            _refreshLyricsRunner.RunAsync(RefreshLyricsAsync);
        }

        public void UpdateTranslations()
        {
            _refreshTranslationRunner.RunAsync(RefreshTranslationAsync);
        }
    }
}
