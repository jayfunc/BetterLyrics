using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models.Lyrics;
using BetterLyrics.WinUI3.Services.LocalizationService;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class LyricsDataExtensions
    {
        extension(LyricsData lyricsData)
        {
            public static LyricsData GetLoadingPlaceholder()
            {
                return new LyricsData()
                {
                    LyricsLines = [
                        new LyricsLine
                        {
                            StartMs = 0,
                            EndMs = (int)TimeSpan.FromMinutes(99).TotalMilliseconds,
                            PrimaryText = "● ● ●",
                            PrimarySyllables = [new BaseLyrics { Text = "● ● ●", StartMs = 0, EndMs = (int)TimeSpan.FromMinutes(99).TotalMilliseconds }],
                        },
                    ],
                    LanguageCode = "N/A",
                };
            }

            public static LyricsData GetNotfoundPlaceholder()
            {
                return new LyricsData([new LyricsLine
                {
                    StartMs = 0,
                    EndMs = (int)TimeSpan.FromMinutes(99).TotalMilliseconds,
                    PrimaryText = "N/A",
                    PrimarySyllables = [new BaseLyrics { Text = "N/A", StartMs = 0, EndMs = (int)TimeSpan.FromMinutes(99).TotalMilliseconds }],
                }]);
            }

            public void SetTranslatedText(LyricsData translationData, int toleranceMs = 50)
            {
                foreach (var line in lyricsData.LyricsLines)
                {
                    // 在翻译歌词中查找与当前行开始时间最接近且在容忍范围内的行
                    var transLine = translationData.LyricsLines
                        .FirstOrDefault(t => Math.Abs(t.StartMs - line.StartMs) <= toleranceMs);

                    if (transLine != null)
                    {
                        // 此处 transLine.OriginalText 指翻译中的“原文”属性
                        line.SecondaryText = transLine.PrimaryText;
                    }
                    else
                    {
                        // 没有匹配的翻译
                        line.SecondaryText = "";
                    }
                }
            }

            public void SetPhoneticText(LyricsData phoneticData, int toleranceMs = 50)
            {
                foreach (var line in lyricsData.LyricsLines)
                {
                    // 在音译歌词中查找与当前行开始时间最接近且在容忍范围内的行
                    var transLine = phoneticData.LyricsLines
                        .FirstOrDefault(t => Math.Abs(t.StartMs - line.StartMs) <= toleranceMs);

                    if (transLine != null)
                    {
                        // 此处 transLine.OriginalText 指音译中的“原文”属性
                        line.TertiaryText = transLine.PrimaryText;
                    }
                    else
                    {
                        // 没有匹配的音译
                        line.TertiaryText = "";
                    }
                }
            }

            public void SetTranslation(string translation)
            {
                List<string> translationArr = translation.Split(StringHelper.NewLine).ToList();
                int i = 0;
                foreach (var line in lyricsData.LyricsLines)
                {
                    if (i >= translationArr.Count)
                    {
                        line.SecondaryText = ""; // No translation available, keep empty
                    }
                    else
                    {
                        line.SecondaryText = translationArr[i];
                    }
                    i++;
                }
            }

            public void SetTransliteration(string transliteration)
            {
                List<string> transliterationArr = transliteration.Split(StringHelper.NewLine).ToList();
                int i = 0;
                foreach (var line in lyricsData.LyricsLines)
                {
                    if (i >= transliterationArr.Count)
                    {
                        line.TertiaryText = ""; // No transliteration available, keep empty
                    }
                    else
                    {
                        line.TertiaryText = transliterationArr[i];
                    }
                    i++;
                }
            }

            public LyricsLine? GetLyricsLine(double sec)
            {
                for (int i = 0; i < lyricsData.LyricsLines.Count; i++)
                {
                    var line = lyricsData.LyricsLines[i];
                    if (line.StartMs > sec * 1000)
                    {
                        return lyricsData.LyricsLines.ElementAtOrDefault(i - 1);
                    }
                }
                return null;
            }

        }
    }
}
