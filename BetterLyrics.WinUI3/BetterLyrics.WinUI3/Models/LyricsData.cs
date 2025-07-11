using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models
{
    public class LyricsData
    {
        public List<LyricsLine> LyricsLines { get; set; }
        public string? LanguageCode => LanguageHelper.DetectLanguageCode(WrappedOriginalText);
        public string WrappedOriginalText => string.Join(StringHelper.NewLine, LyricsLines.Select(line => line.OriginalText));

        public LyricsData()
        {
            LyricsLines = [];
        }

        public LyricsData(List<LyricsLine> lyricsLines)
        {
            LyricsLines = lyricsLines;
        }

        public void SetDisplayedTextAlongWith(LyricsData translationData)
        {
            int i = 0;
            foreach (var line in LyricsLines)
            {
                if (i >= translationData.LyricsLines.Count)
                {
                    line.DisplayedText = line.OriginalText; // No translation available, keep original text
                }
                else
                {
                    line.DisplayedText = $"{line.OriginalText}{StringHelper.NewLine}({translationData.LyricsLines[i].OriginalText})";
                }
                i++;
            }
        }

        public void SetDisplayedTextAlongWith(string translation)
        {
            List<string> translationArr = translation.Split(StringHelper.NewLine).ToList();
            int i = 0;
            foreach (var line in LyricsLines)
            {
                if (i >= translationArr.Count)
                {
                    line.DisplayedText = line.OriginalText; // No translation available, keep original text
                }
                else
                {
                    line.DisplayedText = $"{line.OriginalText}{StringHelper.NewLine}({translationArr[i]})";
                }
                i++;
            }
        }

        public void SetDisplayedTextInOriginalText()
        {
            foreach (var line in LyricsLines)
            {
                line.DisplayedText = line.OriginalText;
            }
        }

        public static LyricsData GetNotfoundPlaceholder(int durationMs)
        {
            return new LyricsData([new LyricsLine
            {
                StartMs = 0,
                EndMs = durationMs,
                OriginalText = App.ResourceLoader!.GetString("LyricsNotFound"),
                CharTimings = [],
            }]);
        }

        public static LyricsData GetLoadingPlaceholder()
        {
            return new LyricsData([
                new LyricsLine
                {
                    StartMs = 0,
                    EndMs = (int)TimeSpan.FromMinutes(99).TotalMilliseconds,
                    OriginalText = "● ● ●",
                    DisplayedText = "● ● ●",
                    CharTimings = [],
                },
            ]);
        }
    }
}
