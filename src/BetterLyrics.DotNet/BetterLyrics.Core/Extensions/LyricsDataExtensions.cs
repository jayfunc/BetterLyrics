using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Lyrics;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace BetterLyrics.Core.Extensions;

public static class LyricsDataExtensions
{
    private static readonly ILocalizationService _localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();

    extension(LyricsData lyricsData)
    {
        public static LyricsData GetLoadingPlaceholder(int attempt = 1, int maxRetries = 1)
        {
            var loadingText = $"{_localizationService.GetLocalizedString("LyricsLoading")} ({attempt}/{maxRetries})";
            return new LyricsData
            {
                LyricsLines =
                [
                    new LyricsLine
                    {
                        StartMs = 0,
                        EndMs = (int)TimeSpan.FromSeconds(30).TotalMilliseconds,
                        PrimaryText = loadingText,
                        PrimarySyllables =
                        [
                            new BaseLyrics
                            {
                                Text = loadingText, StartMs = 0, EndMs = (int)TimeSpan.FromSeconds(30).TotalMilliseconds
                            }
                        ],
                        IsPrimaryHasRealSyllableInfo = true
                    }
                ],
            };
        }

        public void SetTranslatedText(LyricsData translationData, int toleranceMs = 50)
        {
            foreach (var line in lyricsData.LyricsLines)
            {
                // 在翻译歌词中查找与当前行开始时间最接近且在容忍范围内的行
                var transLine = translationData.LyricsLines
                    .FirstOrDefault(t => Math.Abs(t.StartMs - line.StartMs) <= toleranceMs);

                if (transLine != null)
                    // 此处 transLine.PrimaryText 指翻译中的“原文”属性
                    line.SecondaryText = transLine.PrimaryText;
                else
                    // 没有匹配的翻译
                    line.SecondaryText = "";
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
                    // 此处 transLine.PrimaryText 指音译中的“原文”属性
                    line.TertiaryText = transLine.PrimaryText;
                else
                    // 没有匹配的音译
                    line.TertiaryText = "";
            }
        }

        public void SetTranslation(string translation)
        {
            var translationArr = translation.Split(StringHelper.NewLine).ToList();
            var i = 0;
            foreach (var line in lyricsData.LyricsLines)
            {
                if (i >= translationArr.Count)
                    line.SecondaryText = ""; // No translation available, keep empty
                else
                    line.SecondaryText = translationArr[i];
                i++;
            }
        }

        public void SetTransliteration(string transliteration)
        {
            var transliterationArr = transliteration.Split(StringHelper.NewLine).ToList();
            var i = 0;
            foreach (var line in lyricsData.LyricsLines)
            {
                if (i >= transliterationArr.Count)
                    line.TertiaryText = ""; // No transliteration available, keep empty
                else
                    line.TertiaryText = transliterationArr[i];
                i++;
            }
        }

        public LyricsLine? GetLyricsLine(double sec)
        {
            for (var i = 0; i < lyricsData.LyricsLines.Count; i++)
            {
                var line = lyricsData.LyricsLines[i];
                if (line.StartMs > sec * 1000) return lyricsData.LyricsLines.ElementAtOrDefault(i - 1);
            }

            return null;
        }
    }

    public static readonly LyricsData NotFoundPlaceholder = new([
        new LyricsLine
        {
            StartMs = 0,
            EndMs = (int)TimeSpan.FromMinutes(99).TotalMilliseconds,
            PrimaryText = _localizationService.GetLocalizedString("LyricsNotFound"),
            PrimarySyllables =
            [
                new BaseLyrics
                {
                    Text = _localizationService.GetLocalizedString("LyricsNotFound"), StartMs = 0, EndMs = (int)TimeSpan.FromMinutes(99).TotalMilliseconds
                }
            ]
        }
    ]);
}