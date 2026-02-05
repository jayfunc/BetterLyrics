using BetterLyrics.WinUI3.Models.Lyrics;
using BetterLyrics.WinUI3.Models.Settings;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Helper.Lyrics
{
    public class LyricsConverter
    {
        public static string? Convert(LyricsData? lyricsData, string? title, string? artist, string? album, double? duration, LyricsSaveConfig lyricsSaveConfig)
        {
            if (lyricsData == null) return null;

            StringBuilder stringBuilder = new StringBuilder();

            // 构建元数据
            if (title != null) stringBuilder.AppendLine($"[ti:{title}]");
            if (artist != null) stringBuilder.AppendLine($"[ar:{artist}]");
            if (album != null) stringBuilder.AppendLine($"[al:{album}]");
            if (duration != null) stringBuilder.AppendLine($"[length:{FormatToMetadataTimestamp(duration.Value)}]");
            stringBuilder.AppendLine($"[re:{Constants.App.AppName}]");
            stringBuilder.AppendLine($"[ve:{MetadataHelper.AppVersion}]");
            stringBuilder.AppendLine($"[#:{Constants.Link.BetterLyricsGitHub}]");

            // 换行
            stringBuilder.AppendLine();

            foreach (var line in lyricsData.LyricsLines)
            {
                var lineTimestamp = FormatToLineTimestamp(line.StartMs);

                // 构建原文
                stringBuilder.Append(lineTimestamp);
                if (lyricsSaveConfig.InSyllablesFormat)
                {
                    foreach (var syllable in line.PrimarySyllables)
                    {
                        stringBuilder.Append(FormatToSyllableTimestamp(syllable.StartMs));
                        stringBuilder.Append(syllable.Text);
                    }
                }
                else
                {
                    stringBuilder.Append(line.PrimaryText);
                }

                // 构建翻译
                if (lyricsSaveConfig.IncludeTranslation)
                {
                    var translation = line.SecondaryText;
                    if (!string.IsNullOrWhiteSpace(translation))
                    {
                        if (lyricsSaveConfig.InOneLine)
                        {
                            stringBuilder.Append(" / ");
                        }
                        else
                        {
                            stringBuilder.AppendLine();
                        }
                        stringBuilder.Append(lineTimestamp);
                        stringBuilder.Append(translation);
                    }
                }

                // 构建音译
                if (lyricsSaveConfig.IncludeTransliteration)
                {
                    var transliteration = line.TertiaryText;
                    if (!string.IsNullOrWhiteSpace(transliteration))
                    {
                        if (lyricsSaveConfig.InOneLine)
                        {
                            stringBuilder.Append(" / ");
                        }
                        else
                        {
                            stringBuilder.AppendLine();
                        }
                        stringBuilder.Append(lineTimestamp);
                        stringBuilder.Append(transliteration);
                    }
                }

                // 换行，为下一次遍历做准备
                stringBuilder.AppendLine();
            }

            return stringBuilder.ToString();
        }

        public static string FormatToMetadataTimestamp(double seconds)
        {
            TimeSpan ts = TimeSpan.FromSeconds(seconds);

            return $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}";
        }

        public static string FormatToLineTimestamp(double milliseconds)
        {
            TimeSpan ts = TimeSpan.FromMilliseconds(milliseconds);

            return $"[{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D2}]";
        }

        public static string FormatToSyllableTimestamp(double milliseconds)
        {
            TimeSpan ts = TimeSpan.FromMilliseconds(milliseconds);

            return $"<{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D2}>";
        }
    }
}
