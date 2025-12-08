// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.TranslateService;
using CommunityToolkit.Mvvm.DependencyInjection;
using Lyricify.Lyrics.Helpers.General;
using Lyricify.Lyrics.Parsers;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace BetterLyrics.WinUI3.Parsers.LyricsParser
{
    public partial class LyricsParser
    {
        private static readonly ILogger<LyricsParser> _logger = Ioc.Default.GetRequiredService<ILogger<LyricsParser>>();

        private List<LyricsData> _lyricsDataArr = [];

        public LyricsParser()
        {
        }

        public List<LyricsData> Parse(LyricsSearchResult? lyricsSearchResult)
        {
            _logger.LogInformation("LyricsParser.Parse");
            _lyricsDataArr = [];
            if (string.IsNullOrWhiteSpace(lyricsSearchResult?.Raw))
            {
                _lyricsDataArr.Add(LyricsData.GetNotfoundPlaceholder());
            }
            else
            {
                switch (lyricsSearchResult.Raw.DetectFormat())
                {
                    case LyricsFormat.Lrc:
                    case LyricsFormat.Eslrc:
                        ParseLrc(lyricsSearchResult.Raw, lyricsSearchResult.Provider.IsRemote());
                        break;
                    case LyricsFormat.Qrc:
                        ParseQrcKrc(QrcParser.Parse(lyricsSearchResult.Raw).Lines);
                        break;
                    case LyricsFormat.Krc:
                        ParseQrcKrc(KrcParser.Parse(lyricsSearchResult.Raw).Lines);
                        break;
                    case LyricsFormat.Ttml:
                        ParseTtml(lyricsSearchResult.Raw);
                        break;
                    default:
                        break;
                }

                if (_lyricsDataArr.Count == 0)
                {
                    _lyricsDataArr.Add(LyricsData.GetNotfoundPlaceholder());
                }
            }
            LoadTranslation(lyricsSearchResult);
            LoadTransliteration(lyricsSearchResult);
            GenerateTransliterationLyricsData();

            return _lyricsDataArr;
        }

        public async Task<LyricsData> Parse(ITranslateService translateService, TranslationSettings settings, LyricsSearchResult? lyricsSearchResult, CancellationToken token)
        {
            Parse(lyricsSearchResult);

            var main = _lyricsDataArr.First();

            // 应用音译
            if (settings.IsChineseRomanizationEnabled && main.LanguageCode == LanguageHelper.ChineseCode)
            {
                var found = settings.ChineseRomanization switch
                {
                    ChineseRomanization.Pinyin => _lyricsDataArr.FirstOrDefault(x => x.LanguageCode == PhoneticHelper.PinyinCode),
                    ChineseRomanization.Jyutping => _lyricsDataArr.FirstOrDefault(x => x.LanguageCode == PhoneticHelper.JyutpingCode),
                    _ => null,
                };
                if (found != null)
                {
                    main.SetPhoneticText(found);
                }
            }

            // 应用翻译
            if (settings.IsTranslationEnabled && main.LanguageCode != settings.SelectedTargetLanguageCode)
            {
                var found = _lyricsDataArr.FirstOrDefault(x => x.LanguageCode == settings.SelectedTargetLanguageCode);
                if (found != null)
                {
                    main.SetTranslatedText(found);
                }
                else if (settings.IsLibreTranslateEnabled)
                {
                    string translated = string.Empty;
                    try
                    {
                        translated = await translateService.TranslateTextAsync(main.WrappedOriginalText, settings.SelectedTargetLanguageCode, token);
                        _lyricsDataArr.FirstOrDefault()?.SetTranslation(translated);
                        //lyricsSearchResult = Enums.TranslationSearchProvider.LibreTranslate;
                    }
                    catch (TaskCanceledException) { }
                    catch (Exception)
                    {
                        ToastHelper.ShowToast("LibreTranslateFailed", null, InfoBarSeverity.Error);
                    }
                }
            }

            // 应用简体中文/繁体中文
            foreach (var item in main.LyricsLines)
            {
                item.PhoneticText = settings.IsTraditionalChineseEnabled ? ChineseHelper.ToTC(item.PhoneticText) : ChineseHelper.ToSC(item.PhoneticText);
                item.OriginalText = settings.IsTraditionalChineseEnabled ? ChineseHelper.ToTC(item.OriginalText) : ChineseHelper.ToSC(item.OriginalText);
                item.TranslatedText = settings.IsTraditionalChineseEnabled ? ChineseHelper.ToTC(item.TranslatedText) : ChineseHelper.ToSC(item.TranslatedText);
            }

            return main;
        }

        private void LoadTranslation(LyricsSearchResult? lyricsSearchResult)
        {
            if (!string.IsNullOrWhiteSpace(lyricsSearchResult?.Translation))
            {
                switch (lyricsSearchResult.Provider)
                {
                    case LyricsSearchProvider.QQ:
                    case LyricsSearchProvider.Kugou:
                    case LyricsSearchProvider.Netease:
                        ParseLrc(lyricsSearchResult.Translation, true);
                        break;
                    default:
                        break;
                }
            }
        }

        private void LoadTransliteration(LyricsSearchResult? lyricsSearchResult)
        {
            if (!string.IsNullOrWhiteSpace(lyricsSearchResult?.Transliteration))
            {
                switch (lyricsSearchResult.Provider)
                {
                    case LyricsSearchProvider.Netease:
                        ParseLrc(lyricsSearchResult.Transliteration, true);
                        _lyricsDataArr.LastOrDefault()?.LanguageCode = PhoneticHelper.RomanCode;
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// 在音译不存在的情况下生成音译歌词
        /// </summary>
        private void GenerateTransliterationLyricsData()
        {
            var main = _lyricsDataArr.FirstOrDefault();
            if (main != null)
            {
                string? languageCode = main.LanguageCode;
                if (languageCode == LanguageHelper.ChineseCode)
                {
                    if (!_lyricsDataArr.Any(x => x.LanguageCode == PhoneticHelper.PinyinCode))
                    {
                        _lyricsDataArr.Add(new LyricsData
                        {
                            LanguageCode = PhoneticHelper.PinyinCode,
                            AutoGenerated = true,
                            LyricsLines = main.LyricsLines.Select(line => new LyricsLine
                            {
                                StartMs = line.StartMs,
                                EndMs = line.EndMs,
                                OriginalText = PhoneticHelper.ToPinyin(line.OriginalText),
                                LyricsSyllables = line.LyricsSyllables.Select(c => new LyricsSyllable
                                {
                                    StartMs = c.StartMs,
                                    EndMs = c.EndMs,
                                    Text = PhoneticHelper.ToPinyin(c.Text),
                                    StartIndex = c.StartIndex
                                }).ToList()
                            }).ToList()
                        });
                    }
                    if (!_lyricsDataArr.Any(x => x.LanguageCode == PhoneticHelper.JyutpingCode))
                    {
                        _lyricsDataArr.Add(new LyricsData
                        {
                            LanguageCode = PhoneticHelper.JyutpingCode,
                            AutoGenerated = true,
                            LyricsLines = main.LyricsLines.Select(line => new LyricsLine
                            {
                                StartMs = line.StartMs,
                                EndMs = line.EndMs,
                                OriginalText = PhoneticHelper.ToJyutping(line.OriginalText),
                                LyricsSyllables = line.LyricsSyllables.Select(c => new LyricsSyllable
                                {
                                    StartMs = c.StartMs,
                                    EndMs = c.EndMs,
                                    Text = PhoneticHelper.ToJyutping(c.Text),
                                    StartIndex = c.StartIndex
                                }).ToList()
                            }).ToList()
                        });
                    }
                }
                else if (languageCode == LanguageHelper.JapaneseCode)
                {
                    if (!_lyricsDataArr.Any(x => x.LanguageCode == PhoneticHelper.RomanCode))
                    {
                        _lyricsDataArr.Add(new LyricsData
                        {
                            LanguageCode = PhoneticHelper.RomanCode,
                            AutoGenerated = true,
                            LyricsLines = main.LyricsLines.Select(line => new LyricsLine
                            {
                                StartMs = line.StartMs,
                                EndMs = line.EndMs,
                                OriginalText = PhoneticHelper.ToRomaji(line.OriginalText),
                                LyricsSyllables = line.LyricsSyllables.Select(c => new LyricsSyllable
                                {
                                    StartMs = c.StartMs,
                                    EndMs = c.EndMs,
                                    Text = PhoneticHelper.ToRomaji(c.Text),
                                    StartIndex = c.StartIndex
                                }).ToList()
                            }).ToList()
                        });
                    }
                }
            }
        }

    }
}
