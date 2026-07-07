using System.Net;
using System.Text;
using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models.Lyrics;
using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Helpers.Lyrics;

public class LyricsConverter
{
    public static string? Convert(LyricsData? lyricsData, string? title, string? artist, string? album,
        double? duration, LyricsSaveConfig lyricsSaveConfig, LyricsFormat lyricsFormat)
    {
        if (lyricsData == null) return null;

        StringBuilder stringBuilder = new();

        if (lyricsFormat == LyricsFormat.Lrc)
        {
            // 构建元数据
            if (title != null) stringBuilder.AppendLine($"[ti:{title}]");
            if (artist != null) stringBuilder.AppendLine($"[ar:{artist}]");
            if (album != null) stringBuilder.AppendLine($"[al:{album}]");
            if (duration != null) stringBuilder.AppendLine($"[length:{FormatToMetadataTimestamp(duration.Value)}]");
            stringBuilder.AppendLine($"[re:{App.AppName}]");
            stringBuilder.AppendLine($"[ve:{MetadataHelper.AppVersion}]");
            stringBuilder.AppendLine($"[#:{Link.BetterLyricsGitHub}]");

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

                    var lastSyllable = line.PrimarySyllables[^1];
                    if (lastSyllable.EndMs > lastSyllable.StartMs && lastSyllable.Text.Trim().Length > 0)
                        stringBuilder.Append(FormatToSyllableTimestamp(lastSyllable.EndMs));
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
                            stringBuilder.Append(" / ");
                        else
                            stringBuilder.AppendLine();
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
                            stringBuilder.Append(" / ");
                        else
                            stringBuilder.AppendLine();
                        stringBuilder.Append(lineTimestamp);
                        stringBuilder.Append(transliteration);
                    }
                }

                // 换行，为下一次遍历做准备
                stringBuilder.AppendLine();
            }
        }
        // 规范参考 https://github.com/amll-dev/amll-ttml-db/wiki/%E6%A0%BC%E5%BC%8F%E8%A7%84%E8%8C%83
        else if (lyricsFormat == LyricsFormat.Ttml)
        {
            // XML声明和TTML根节点，添加规范要求的命名空间
            stringBuilder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");

            var timing = lyricsSaveConfig.InSyllablesFormat ? "Word" : "Line";
            stringBuilder.AppendLine("<tt xmlns=\"http://www.w3.org/ns/ttml\"");
            stringBuilder.AppendLine("    xmlns:ttm=\"http://www.w3.org/ns/ttml#metadata\"");
            stringBuilder.AppendLine("    xmlns:tts=\"http://www.w3.org/ns/ttml#styling\"");
            stringBuilder.AppendLine("    xmlns:itunes=\"http://itunes.apple.com/lyric-ttml-extensions\"");
            stringBuilder.AppendLine("    xmlns:amll=\"http://www.example.com/ns/amll\"");
            stringBuilder.AppendLine($"    xmlns:betterlyrics=\"{Link.BetterLyricsGitHub}\"");
            stringBuilder.AppendLine($"    itunes:timing=\"{timing}\">");

            // 构建元数据 (Head)
            stringBuilder.AppendLine("  <head>");
            stringBuilder.AppendLine("    <metadata>");

            stringBuilder.AppendLine($"      <betterlyrics:meta key=\"generator\" value=\"{App.AppName}\" />");
            stringBuilder.AppendLine(
                $"      <betterlyrics:meta key=\"version\" value=\"{MetadataHelper.AppVersion}\" />");

            // 规范 4.1：使用 ttm:title 定义歌曲名
            if (!string.IsNullOrWhiteSpace(title))
                stringBuilder.AppendLine($"      <ttm:title>{WebUtility.HtmlEncode(title)}</ttm:title>");

            // 规范 4.1：使用 ttm:agent 定义演唱者
            var safeArtist = WebUtility.HtmlEncode(artist ?? "Unknown Artist");
            stringBuilder.AppendLine("      <ttm:agent type=\"person\" xml:id=\"v1\">");
            stringBuilder.AppendLine($"        <ttm:name type=\"full\">{safeArtist}</ttm:name>");
            stringBuilder.AppendLine("      </ttm:agent>");

            // 规范 4.2：使用 amll:meta 定义歌曲核心信息（为符合规范，不再重复写入 amll:meta 的 musicName）
            if (!string.IsNullOrWhiteSpace(artist))
                stringBuilder.AppendLine($"      <amll:meta key=\"artists\" value=\"{safeArtist}\" />");
            if (!string.IsNullOrWhiteSpace(album))
                stringBuilder.AppendLine($"      <amll:meta key=\"album\" value=\"{WebUtility.HtmlEncode(album)}\" />");

            stringBuilder.AppendLine("    </metadata>");
            stringBuilder.AppendLine("  </head>");

            // 构建歌词主体 (Body)
            var durAttribute = duration != null ? $" dur=\"{FormatToTtmlTimestamp(duration.Value * 1000)}\"" : "";
            stringBuilder.AppendLine($"  <body{durAttribute}>");
            stringBuilder.AppendLine("    <div>");

            var lineIndex = 1;
            foreach (var line in lyricsData.LyricsLines)
            {
                // 规范 6.1 & 7.2：必须包含 begin, end, itunes:key, ttm:agent
                var beginTime = FormatToTtmlTimestamp(line.StartMs);
                var endTime =
                    FormatToTtmlTimestamp(line.EndMs > line.StartMs
                        ? line.EndMs
                        : line.StartMs + 2000); // 如果 EndMs 缺失，兜底加2秒防止报错

                stringBuilder.Append(
                    $"      <p begin=\"{beginTime}\" end=\"{endTime}\" itunes:key=\"L{lineIndex}\" ttm:agent=\"v1\">");

                // 构建原文
                if (lyricsSaveConfig.InSyllablesFormat && line.PrimarySyllables != null &&
                    line.PrimarySyllables.Count > 0)
                    foreach (var syllable in line.PrimarySyllables)
                    {
                        // 逐字歌词 span 必须带 begin 和 end
                        var sylBegin = FormatToTtmlTimestamp(syllable.StartMs);
                        var sylEnd = FormatToTtmlTimestamp(syllable.EndMs > syllable.StartMs
                            ? syllable.EndMs
                            : syllable.StartMs + 500);
                        stringBuilder.Append(
                            $"<span begin=\"{sylBegin}\" end=\"{sylEnd}\">{WebUtility.HtmlEncode(syllable.Text)}</span>");
                    }
                else
                    stringBuilder.Append(WebUtility.HtmlEncode(line.PrimaryText));

                // 规范 7.3：辅助歌词（内嵌翻译与罗马音）使用带有 ttm:role 的 span 标签，不再使用 <br/> 或 / 拼接
                if (lyricsSaveConfig.IncludeTranslation && !string.IsNullOrWhiteSpace(line.SecondaryText))
                    stringBuilder.Append(
                        $"<span ttm:role=\"x-translation\">{WebUtility.HtmlEncode(line.SecondaryText)}</span>");

                if (lyricsSaveConfig.IncludeTransliteration && !string.IsNullOrWhiteSpace(line.TertiaryText))
                    stringBuilder.Append(
                        $"<span ttm:role=\"x-roman\">{WebUtility.HtmlEncode(line.TertiaryText)}</span>");

                stringBuilder.AppendLine("</p>");
                lineIndex++;
            }

            stringBuilder.AppendLine("    </div>");
            stringBuilder.AppendLine("  </body>");
            stringBuilder.AppendLine("</tt>");
        }

        return stringBuilder.ToString();
    }

    public static string FormatToMetadataTimestamp(double seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}";
    }

    public static string FormatToLineTimestamp(double milliseconds)
    {
        var ts = TimeSpan.FromMilliseconds(milliseconds);
        return $"[{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}]";
    }

    public static string FormatToSyllableTimestamp(double? milliseconds)
    {
        var ts = TimeSpan.FromMilliseconds(milliseconds ?? 0);
        return $"<{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}>";
    }

    public static string FormatToTtmlTimestamp(double? milliseconds)
    {
        var ts = TimeSpan.FromMilliseconds(milliseconds ?? 0);
        // 规范 3.1: 推荐使用 hh:mm:ss.xxx 或 mm:ss.xxx 格式
        return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
    }
}