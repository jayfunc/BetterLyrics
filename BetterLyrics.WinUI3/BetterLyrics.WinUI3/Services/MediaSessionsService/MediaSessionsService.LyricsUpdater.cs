using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Lyricify.Lyrics.Helpers.General;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;

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

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsSearchProvider? LyricsSearchProvider { get; set; }

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial TranslationSearchProvider? TranslationSearchProvider { get; set; }

        [ObservableProperty] public partial bool IsTranslating { get; set; } = false;

        private async Task RefreshTranslationAsync(CancellationToken token)
        {
            TranslationSearchProvider = null;
            _lyricsDataArr.ElementAtOrDefault(0)?.ClearTranslatedText();
            LyricsChanged?.Invoke(this, new LyricsChangedEventArgs(CurrentLyricsData));
            IsTranslating = true;

            await SetPhoneticTextAsync(token);
            await SetTranslatedTextAsync(token);
            if (token.IsCancellationRequested) return;

            IsTranslating = false;
            LyricsChanged?.Invoke(this, new LyricsChangedEventArgs(CurrentLyricsData));
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
            _logger.LogInformation("Original language code: {OriginalLangCode}", originalLangCode ?? "null");

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
                    _logger.LogInformation("Found translation in lyrics data at index {FoundIndex}", found);
                    if (_settingsService.AppSettings.TranslationSettings.ShowTranslationOnly)
                    {
                        _lyricsDataArr[found].ClearTranslatedText();
                        _langIndex = found;
                    }
                    else
                    {
                        _lyricsDataArr[0].SetTranslatedText(_lyricsDataArr[found], _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.LyricsTranslationSeparator, 50);
                        _langIndex = 0;
                        TranslationSearchProvider = LyricsSearchProvider.ToTranslationSearchProvider();
                    }
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

                        if (_settingsService.AppSettings.TranslationSettings.ShowTranslationOnly)
                        {
                            _lyricsDataArr[^1] = _lyricsDataArr[0].CreateLyricsDataFrom(translated);
                            _lyricsDataArr[^1].ClearTranslatedText();
                            _langIndex = _lyricsDataArr.Count - 1;
                        }
                        else
                        {
                            _lyricsDataArr[0].SetTranslation(translated, _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.LyricsTranslationSeparator);
                            _langIndex = 0;
                        }
                        TranslationSearchProvider = Enums.TranslationSearchProvider.LibreTranslate;
                    }
                    catch (Exception)
                    {
                        App.Current.LyricsWindowNotificationPanel?.Notify(App.ResourceLoader?.GetString("LibreTranslateFailed")!, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
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
            _logger.LogInformation("Original language code: {OriginalLangCode}", originalLangCode ?? "null");

            if (originalLangCode == "zh" && _settingsService.AppSettings.TranslationSettings.IsChineseRomanizationEnabled)
            {
                switch (_settingsService.AppSettings.TranslationSettings.ChineseRomanization)
                {
                    case ChineseRomanization.Pinyin:
                        targetPhoneticCode = "pinyin";
                        break;
                    case ChineseRomanization.Jyutping:
                        targetPhoneticCode = "jyutping";
                        break;
                    default:
                        break;
                }
            }
            else if (originalLangCode == "ja" && _settingsService.AppSettings.TranslationSettings.IsJapaneseRomanizationEnabled)
            {
                targetPhoneticCode = "romaji";
            }

            if (targetPhoneticCode == "")
            {
                _lyricsDataArr[0].ClearPhoneticText();
            }

            // Try get phonetic text from itself first
            int found = _translateService.SearchTranslatedLyricsItself(_lyricsDataArr, targetPhoneticCode);
            if (found >= 0)
            {
                _logger.LogInformation("Found translation in lyrics data at index {FoundIndex}", found);
                _lyricsDataArr[0].SetPhoneticText(_lyricsDataArr[found], _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.LyricsTranslationSeparator, 50);
                _langIndex = 0;
            }

        }

        private async Task RefreshLyricsAsync(CancellationToken token)
        {
            _logger.LogInformation("Refreshing lyrics...");

            LyricsSearchProvider = null;
            _lyricsDataArr = [LyricsData.GetLoadingPlaceholder()];

            LyricsChanged?.Invoke(this, new LyricsChangedEventArgs(CurrentLyricsData));

            if (SongInfo != null)
            {
                _logger.LogInformation("Searching lyrics for: Title={Title}, Artist={Artist}, Album={Album}, DurationMs={DurationMs}",
                    SongInfo.Title, SongInfo.Artist, SongInfo.Album, SongInfo.DurationMs);

                var lyricsSearchResult = await Task.Run(async () => await _lyrcsSearchService.SearchSmartlyAsync(
                    SongInfo.PlayerId ?? "",
                    SongInfo.Title,
                    SongInfo.Artist,
                    SongInfo.Album ?? "",
                    SongInfo.DurationMs ?? 0,
                    SongInfo.SongId,
                    token
                ), token);
                if (token.IsCancellationRequested) return;
                LyricsSearchProvider = lyricsSearchResult?.Provider;

                _logger.LogInformation("Lyrics was found? {Found}, Provider: {LyricsSearchProvider}", lyricsSearchResult?.IsFound, LyricsSearchProvider?.ToString() ?? "null");

                _lyricsDataArr = new LyricsParser().Parse(lyricsSearchResult?.Raw, (int?)SongInfo?.DurationMs);
                ApplyChinesePreference();
                FillTranslationFromCache(LyricsSearchProvider);
            }
            else
            {
                _logger.LogWarning("SongInfo is null, cannot search lyrics.");
            }

            _logger.LogInformation("Parsed lyrics: {MultiLangLyricsCount} languages", _lyricsDataArr.Count);

            // This ensures that translation is always reset while waiting for translations
            _lyricsDataArr[0].ClearTranslatedText();
            LyricsChanged?.Invoke(this, new LyricsChangedEventArgs(CurrentLyricsData));
            ApplyChinesePreference();
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
                    item.OriginalText = traditionalChinesePreferred ? ChineseHelper.S2T(item.OriginalText) : ChineseHelper.T2S(item.OriginalText);
                }
            }
        }

        private void FillTranslationFromCache(LyricsSearchProvider? provider)
        {
            string? translationRaw = null;
            switch (provider)
            {
                case Enums.LyricsSearchProvider.QQ:
                    translationRaw = FileHelper.ReadLyricsCache(SongInfo!.Title, SongInfo.Artist, LyricsFormat.Lrc, PathHelper.QQTranslationCacheDirectory);
                    break;
                case Enums.LyricsSearchProvider.Kugou:
                    translationRaw = FileHelper.ReadLyricsCache(SongInfo!.Title, SongInfo.Artist, LyricsFormat.Lrc, PathHelper.KugouTranslationCacheDirectory);
                    break;
                case Enums.LyricsSearchProvider.Netease:
                    translationRaw = FileHelper.ReadLyricsCache(SongInfo!.Title, SongInfo.Artist, LyricsFormat.Lrc, PathHelper.NeteaseTranslationCacheDirectory);
                    break;
                case Enums.LyricsSearchProvider.LrcLib:
                    break;
                case Enums.LyricsSearchProvider.AmllTtmlDb:
                    break;
                case Enums.LyricsSearchProvider.LocalMusicFile:
                    break;
                case Enums.LyricsSearchProvider.LocalLrcFile:
                    break;
                case Enums.LyricsSearchProvider.LocalEslrcFile:
                    break;
                case Enums.LyricsSearchProvider.LocalTtmlFile:
                    break;
                default:
                    break;
            }
            if (translationRaw != null)
            {
                var translationData = new LyricsParser().Parse(translationRaw, (int?)SongInfo?.DurationMs);
                if (provider == Enums.LyricsSearchProvider.QQ)
                {
                    foreach (var data in translationData)
                    {
                        foreach (var item in data.LyricsLines)
                        {
                            if (item.OriginalText == "//")
                            {
                                item.OriginalText = "";
                            }
                        }
                    }
                }

                _lyricsDataArr = _lyricsDataArr.Concat(translationData).ToList();
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
