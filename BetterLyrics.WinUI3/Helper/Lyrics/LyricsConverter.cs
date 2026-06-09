using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models.Lyrics;
using BetterLyrics.WinUI3.Models.Settings;
using System;
using System.Linq;
using System.Text;

namespace BetterLyrics.WinUI3.Helper.Lyrics
{
    public class LyricsConverter
    {
        public static string? Convert(LyricsData? lyricsData, string? title, string? artist, string? album, double? duration, LyricsSaveConfig lyricsSaveConfig, LyricsFormat lyricsFormat)
        {
            if (lyricsData == null) return null;

            StringBuilder stringBuilder = new StringBuilder();

            if (lyricsFormat == LyricsFormat.Lrc)
            {
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
                    if (lyricsSaveConfig.InSyllablesFormat && line.PrimarySyllables != null)
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
            }
            else if (lyricsFormat == LyricsFormat.Ttml)
            {
                // XML声明和TTML根节点
                stringBuilder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                stringBuilder.AppendLine("<tt xmlns=\"http://www.w3.org/ns/ttml\" xmlns:ttm=\"http://www.w3.org/ns/ttml#metadata\">");

                // 构建元数据 (Head)
                stringBuilder.AppendLine("  <head>");
                stringBuilder.AppendLine("    <metadata>");
                if (!string.IsNullOrWhiteSpace(title))
                {
                    stringBuilder.AppendLine($"      <ttm:title>{System.Net.WebUtility.HtmlEncode(title)}</ttm:title>");
                }
                if (!string.IsNullOrWhiteSpace(artist) || !string.IsNullOrWhiteSpace(album))
                {
                    var descElements = new[] { artist, album }.Where(s => !string.IsNullOrWhiteSpace(s));
                    var desc = string.Join(" - ", descElements);
                    stringBuilder.AppendLine($"      <ttm:desc>{System.Net.WebUtility.HtmlEncode(desc)}</ttm:desc>");
                }
                stringBuilder.AppendLine("    </metadata>");
                stringBuilder.AppendLine("  </head>");

                // 构建歌词主体 (Body)
                stringBuilder.AppendLine("  <body>");
                stringBuilder.AppendLine("    <div>");

                foreach (var line in lyricsData.LyricsLines)
                {
                    var beginTime = FormatToTtmlTimestamp(line.StartMs);
                    stringBuilder.Append($"      <p begin=\"{beginTime}\">");

                    // 构建原文
                    if (lyricsSaveConfig.InSyllablesFormat && line.PrimarySyllables != null)
                    {
                        foreach (var syllable in line.PrimarySyllables)
                        {
                            string sylBeginTime = FormatToTtmlTimestamp(syllable.StartMs);
                            stringBuilder.Append($"<span begin=\"{sylBeginTime}\">{System.Net.WebUtility.HtmlEncode(syllable.Text)}</span>");
                        }
                    }
                    else
                    {
                        stringBuilder.Append(System.Net.WebUtility.HtmlEncode(line.PrimaryText));
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
                                // TTML中换行推荐使用 <br/>
                                stringBuilder.Append("<br/>");
                            }
                            stringBuilder.Append(System.Net.WebUtility.HtmlEncode(translation));
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
                                stringBuilder.Append("<br/>");
                            }
                            stringBuilder.Append(System.Net.WebUtility.HtmlEncode(transliteration));
                        }
                    }

                    stringBuilder.AppendLine("</p>");
                }

                stringBuilder.AppendLine("    </div>");
                stringBuilder.AppendLine("  </body>");
                stringBuilder.AppendLine("</tt>");
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

            return $"[{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}]";
        }

        public static string FormatToSyllableTimestamp(double milliseconds)
        {
            TimeSpan ts = TimeSpan.FromMilliseconds(milliseconds);

            return $"<{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}>";
        }

        public static string FormatToTtmlTimestamp(double milliseconds)
        {
            TimeSpan ts = TimeSpan.FromMilliseconds(milliseconds);

            return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
        }
    }
}