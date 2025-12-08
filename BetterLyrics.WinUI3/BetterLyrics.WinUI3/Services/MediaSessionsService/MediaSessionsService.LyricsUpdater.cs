using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Parsers.LyricsParser;
using CommunityToolkit.Mvvm.ComponentModel;
using Lyricify.Lyrics.Helpers.General;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
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

        private int _langIndex = 0;
        private List<LyricsData> _lyricsDataArr = [];

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsData? CurrentLyricsData { get; private set; }

        public event EventHandler<LyricsChangedEventArgs>? LyricsChanged;

        [ObservableProperty] public partial LyricsSearchResult? CurrentLyricsSearchResult { get; private set; }

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial TranslationSearchProvider? TranslationSearchProvider { get; private set; }

        [ObservableProperty] public partial bool IsTranslating { get; set; } = false;

        private void SetCurrentLyricsData()
        {
            App.Current.Resources.DispatcherQueue.TryEnqueue(() =>
            {
                CurrentLyricsData = _lyricsDataArr.ElementAtOrDefault(_langIndex);
            });
        }

        private async Task RefreshTranslationAsync(TranslationSettings settings, CancellationToken token)
        {
            TranslationSearchProvider = null;
            _lyricsDataArr.ElementAtOrDefault(0)?.ClearTranslatedText();

            IsTranslating = true;

            SetPhoneticText();

            await SetTranslatedTextAsync(settings, token);
            if (token.IsCancellationRequested) return;

            SetCurrentLyricsData();

            IsTranslating = false;
        }

        private async Task SetTranslatedTextAsync(TranslationSettings settings, CancellationToken token)
        {
            if (!settings.IsTranslationEnabled) return;

            _logger.LogInformation("SetTranslatedTextAsync");
            string targetLangCode = settings.SelectedTargetLanguageCode;
            _logger.LogInformation("Target language code: {TargetLangCode}", targetLangCode);
            string? originalText = _lyricsDataArr.FirstOrDefault()?.WrappedOriginalText;
            if (originalText == null) return;

            string? originalLangCode = LanguageHelper.DetectLanguageCode(originalText);
            _logger.LogInformation("Original language code: {OriginalLangCode}", originalLangCode);

            if (originalLangCode == targetLangCode)
            {
                _logger.LogInformation("Original lyrics already in target language: {TargetLangCode}", targetLangCode);

                _lyricsDataArr.FirstOrDefault()?.ClearTranslatedText();
            }
            else
            {
                // Try get translation from itself first
                int found = _translateService.SearchTranslatedLyricsItself(_lyricsDataArr, targetLangCode);
                if (found >= 0)
                {
                    _logger.LogInformation("Found translated text in lyrics data at index {FoundIndex}", found);

                    _lyricsDataArr.FirstOrDefault()?.SetTranslatedText(_lyricsDataArr[found], 50);
                    TranslationSearchProvider = CurrentLyricsSearchResult?.Provider.ToTranslationSearchProvider();
                }
                else if (settings.IsLibreTranslateEnabled)
                {
                    _logger.LogInformation("LibreTranslate is enabled, trying to translate lyrics...");
                    string translated = string.Empty;
                    try
                    {
                        translated = await _translateService.TranslateTextAsync(originalText, targetLangCode, token);
                        if (token.IsCancellationRequested) return;
                        if (translated == string.Empty) return;

                        _lyricsDataArr.FirstOrDefault()?.SetTranslation(translated);

                        TranslationSearchProvider = Enums.TranslationSearchProvider.LibreTranslate;
                    }
                    catch (Exception)
                    {
                        ToastHelper.ShowToast("LibreTranslateFailed", null, InfoBarSeverity.Error);
                    }
                }
            }
        }

        private void SetPhoneticText()
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
                targetPhoneticCode = PhoneticHelper.RomanCode;
            }

            if (targetPhoneticCode == "")
            {
                _lyricsDataArr.FirstOrDefault()?.ClearPhoneticText();
            }

            // Try get phonetic text from itself
            int found = _translateService.SearchTranslatedLyricsItself(_lyricsDataArr, targetPhoneticCode);
            if (found >= 0)
            {
                _logger.LogInformation("Found phonetic text in lyrics data at index {FoundIndex}", found);
                _lyricsDataArr.FirstOrDefault()?.SetPhoneticText(_lyricsDataArr[found], 50);
            }

        }

        private async Task RefreshLyricsAsync(TranslationSettings settings, CancellationToken token)
        {
            _logger.LogInformation("RefreshLyricsAsync");

            CurrentLyricsSearchResult = null;
            _lyricsDataArr = [LyricsData.GetLoadingPlaceholder()];

            SetCurrentLyricsData();

            if (CurrentSongInfo != null)
            {
                CurrentLyricsSearchResult = await Task.Run(async () => await _lyrcsSearchService.SearchSmartlyAsync(
                    CurrentSongInfo,
                    !_settingsService.AppSettings.GeneralSettings.IgnoreCacheWhenSearching,
                    CurrentMediaSourceProviderInfo?.LyricsSearchType,
                    token),
                token);
                if (token.IsCancellationRequested) return;

                var lyricsParser = new LyricsParser();
                lyricsParser.Parse(CurrentSongInfo, CurrentLyricsSearchResult);
                _lyricsDataArr = lyricsParser.LyricsDataArr;
                ApplyChinesePreference();
            }

            _logger.LogInformation("Parsed lyrics: {MultiLangLyricsCount} languages", _lyricsDataArr.Count);

            // Show original first while loading phonetic and translated
            ApplyChinesePreference();

            SetCurrentLyricsData();

            await RefreshTranslationAsync(settings, token);
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

        public async void UpdateLyrics()
        {
            await _refreshLyricsRunner.RunAsync(async (token) =>
            {
                await RefreshLyricsAsync(_settingsService.AppSettings.TranslationSettings, token);
            });
        }

    }
}
