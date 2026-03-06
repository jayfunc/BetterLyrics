using Lyricify.Lyrics.Helpers.General;
using Lyricify.Lyrics.Models;
using System.Text.RegularExpressions;

namespace Lyricify.Lyrics.Helpers.Optimization
{
    /// <summary>
    /// 信息行 (标题行) 相关判断及处理
    /// </summary>
    public static class InfoLines
    {
        public static List<bool> CheckInfoLines(LyricsData lyrics)
            => CheckInfoLines(lyrics.Lines!, lyrics.TrackMetadata);

        public static int GetHeadingInfoLinesCount(LyricsData lyrics)
            => GetHeadingInfoLinesCount(lyrics.Lines!, lyrics.TrackMetadata);

        public static int GetEndingInfoLinesCount(LyricsData lyrics)
            => GetEndingInfoLinesCount(lyrics.Lines!, lyrics.TrackMetadata);

        /// <summary>
        /// Returns a bool list with the same length as <paramref name="lines"/>:
        /// true => info/credit/claiming line
        /// </summary>
        public static List<bool> CheckInfoLines(List<ILineInfo> lines, ITrackMetadata? trackInfo = null)
        {
            if (lines is null) return null!;
            var n = lines.Count;
            var flags = Enumerable.Repeat(false, n).ToList();
            if (n == 0) return flags;

            int startCount = GetHeadingInfoLinesCount(lines, trackInfo);
            int endCount = GetEndingInfoLinesCount(lines, trackInfo);

            for (int i = 0; i < startCount && i < n; i++)
                flags[i] = true;

            for (int i = n - endCount; i < n; i++)
                if (i >= 0) flags[i] = true;

            // Middle part
            int midStart = Math.Clamp(startCount, 0, n);
            int midEndExclusive = Math.Clamp(n - endCount, 0, n);

            for (int i = midStart; i < midEndExclusive; i++)
            {
                var text = GetLineText(lines[i]);
                if (IsInfoLine(text, trackInfo))
                    flags[i] = true;
            }

            return flags;
        }

        public static int GetHeadingInfoLinesCount(List<ILineInfo> lines, ITrackMetadata? trackInfo = null)
        {
            if (lines is null || lines.Count == 0) return 0;

            // Special-case: "Title + Artist"
            int i = 0;
            if (trackInfo != null && lines.Count > 0)
            {
                var first = GetLineText(lines[0]);

                if (LooksLikeTitleAndArtistLine(first, trackInfo))
                {
                    i++;
                }
                else if (i == 0 && lines.Count >= 3)
                {
                    if (IsInfoLine(GetLineText(lines[1]), trackInfo))
                        i += 2;
                }
            }

            for (; i < lines.Count; i++)
            {
                var text = GetLineText(lines[i]);

                if (string.IsNullOrWhiteSpace(text))
                {
                    if (HasUpcomingInfo(lines, i, trackInfo))
                        continue;

                    break;
                }

                if (IsInfoLine(text, trackInfo))
                    continue;

                if (i + 1 < lines.Count && IsInfoLine(GetLineText(lines[i + 1]), trackInfo))
                {
                    i++;
                    continue;
                }

                break;
            }

            return i;
        }

        public static int GetEndingInfoLinesCount(List<ILineInfo> lines, ITrackMetadata? trackInfo = null)
        {
            if (lines is null || lines.Count == 0) return 0;

            int count = 0;

            for (int i = lines.Count - 1; i >= 0; i--)
            {
                var text = GetLineText(lines[i]);

                if (string.IsNullOrWhiteSpace(text))
                {
                    if (HasPreviousInfo(lines, i, trackInfo))
                    {
                        count++;
                        continue;
                    }
                    break;
                }

                if (IsInfoLine(text, trackInfo))
                {
                    count++;
                    continue;
                }

                break;
            }

            return count;
        }

        public static bool IsInfoLine(string? text, ITrackMetadata? trackInfo = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            var str = text.Replace("：", ": ").ToSC().Trim();

            if (LooksLikeArtistSpeakerLabel(text!, trackInfo))
                return false;

            if (IsStringTencentClaiming(str))
                return true;

            if (IsStringCreditBy(str))
                return true;

            bool hitDict = ContainsAnyKeyword(str, TitleLineInfoDict);
            bool hasColon = str.Contains(':') || str.Contains('：');

            if (!hitDict || !hasColon)
            {
                return IsStringCopyrightClaiming(str);
            }

            return true;
        }

        public static readonly List<string> TitleLineInfoDict = new()
        {
            "音",
            "声",
            "词",
            "曲",
            "鼓",
            "笛",
            "编曲",
            "吉他",
            "吉它",
            "贝斯",
            "缩混",
            "胡琴",
            "扬琴",
            "提琴",
            "录音",
            "弦乐",
            "键盘",
            "钢琴",
            "童声",
            "助理",
            "古筝",
            "琵琶",
            "笛子",
            "二胡",
            "唢呐",
            "出品",
            "和声",
            "和音",
            "编写",
            "混音",
            "封面",
            "营销",
            "发行",
            "制作",
            "监制",
            "策划",
            "企划",
            "推广",
            "母带",
            "编写",
            "文案",
            "编辑",
            "统筹",
            "总监",
            "鸣谢",
            "感谢",
            "设计",
            "视觉",
            "小号",
            "长号",
            "管乐",
            "管弦",
            "工程",
            "簧管",
            "巴松",
            "原唱",
            "配唱",
            "伴唱",
            "轨道",
            "演唱",
            "尺八",
            "乐队",
            "调校",
            "伴奏",
            "主唱",
            "曲绘",
            "呼麦",
            "校对",
            "设计",
            "商务",
            "合作",
            "合唱",
            "指挥",
            "经纪",
            "戏腔",
            "团队",
            "协作",
            "顾问",
            "翻译",
            "摄影",
            "协力",
            "艺人",
            "行销",
            "媒介",
            "运营",
            "宣传",
            "管理",
            "单位",
            "支持",
            "平台",
            "单簧管",
            "萨克斯",
            "打击乐",
            "合成器",
            "马头琴",
            "热瓦普",
            "葫芦丝",
            "冬不拉",
            "工作室",
            "OA",
            "OP",
            "OT",
            "PV",
            "SP",
            "A&R",
            "PGM",
            "ISRC",
            "Bass",
            "Drum",
            "Pads",
            "Brass",
            "Cello",
            "Choir",
            "Horns",
            "Mixed",
            "Mixer",
            "Piano",
            "Synth",
            "Viola",
            "Vocal",
            "Winds",
            "Assist",
            "Violin",
            "Mixing",
            "String",
            "Guitar",
            "Master",
            "Chorus",
            "Record",
            "violins",
            "Arrange",
            "Conduct",
            "Editing",
            "Hormony",
            "Ocarina",
            "Produce",
            "Strings",
            "Whistle",
            "Engineer",
            "Keyboard",
            "Harmonica",
            "Mastering",
            "Recording",
            "Percussion",
            "Programing",
            "Production",
            "Programming",
            "Additional programming",
            "Irish whistle",
        };

        private static string GetLineText(ILineInfo? line)
            => line?.Text ?? string.Empty;

        private static bool ContainsAny(string s, params string[] parts)
        {
            foreach (var p in parts)
            {
                if (string.IsNullOrEmpty(p)) continue;
                if (s.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private static bool ContainsAnyKeyword(string s, List<string> dict)
        {
            if (dict is null || dict.Count == 0) return false;

            for (int i = 0; i < dict.Count; i++)
            {
                var k = dict[i];
                if (string.IsNullOrWhiteSpace(k)) continue;

                if (s.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private static bool HasUpcomingInfo(List<ILineInfo> lines, int index, ITrackMetadata? trackInfo)
        {
            int seen = 0;
            for (int i = index + 1; i < lines.Count && seen < 2; i++)
            {
                var t = GetLineText(lines[i]);
                if (string.IsNullOrWhiteSpace(t)) continue;
                seen++;
                if (IsInfoLine(t, trackInfo)) return true;
            }
            return false;
        }

        private static bool HasPreviousInfo(List<ILineInfo> lines, int index, ITrackMetadata? trackInfo)
        {
            int seen = 0;
            for (int i = index - 1; i >= 0 && seen < 2; i--)
            {
                var t = GetLineText(lines[i]);
                if (string.IsNullOrWhiteSpace(t)) continue;
                seen++;
                if (IsInfoLine(t, trackInfo)) return true;
            }
            return false;
        }

        private static bool LooksLikeArtistSpeakerLabel(string raw, ITrackMetadata? trackInfo)
        {
            // If any artist equals "Artist:" then NOT titleline
            var t = raw.Trim();
            if (!t.EndsWith(":", StringComparison.Ordinal)) return false;

            // Multi-artist
            if (trackInfo is TrackMultiArtistMetadata multi && multi.Artists is { Count: > 0 })
            {
                foreach (var artist in multi.Artists)
                {
                    if (string.IsNullOrWhiteSpace(artist)) continue;
                    if (string.Equals((artist.Trim() + ":"), t, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            // Single artist
            var single = trackInfo?.Artist;
            if (!string.IsNullOrWhiteSpace(single))
            {
                if (string.Equals((single.Trim() + ":"), t, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool LooksLikeTitleAndArtistLine(string line, ITrackMetadata trackInfo)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;

            var lineNorm = (line ?? string.Empty).ToLowerInvariant().Replace("’", "'");
            var titleNorm = (trackInfo.Title ?? string.Empty).ToLowerInvariant().ToSC(true).Replace("’", "'");
            var artistNorm = (trackInfo.Artist ?? string.Empty).ToLowerInvariant().ToSC(true).Replace("’", "'").Replace(", ", "/");

            bool titleHit = ContainsTitle(lineNorm, titleNorm);
            bool artistHit = ContainsArtists(lineNorm, trackInfo, artistNorm);

            if (titleHit && artistHit) return true;

            if ((trackInfo.Title ?? "").Contains(" - ", StringComparison.Ordinal))
            {
                var parts = Regex.Split((trackInfo.Title ?? "").ToLowerInvariant().Replace("’", "'"), " - ", RegexOptions.IgnoreCase);
                if (parts.Length > 0)
                {
                    var t0 = parts[0];
                    if (!string.IsNullOrWhiteSpace(t0)
                        && lineNorm.Contains(t0)
                        && lineNorm.Contains(artistNorm))
                        return true;
                }
            }

            if (StringHelper.HasChinese(trackInfo.Title ?? ""))
            {
                if (line!.Contains((trackInfo.Title ?? "").ToSC())
                    && line.Contains((trackInfo.Artist ?? "").ToSC()))
                    return true;
            }

            return false;
        }

        private static bool ContainsTitle(string lineLower, string titleLower)
        {
            if (string.IsNullOrWhiteSpace(titleLower)) return false;
            if (lineLower.Contains(titleLower)) return true;

            // line has "(...)" => compare before '('
            int idx = lineLower.IndexOf('(');
            if (idx > 0)
            {
                var before = lineLower[..idx];
                if (before.Contains(titleLower)) return true;
            }

            // title has "(...)" => compare before '('
            int tIdx = titleLower.IndexOf('(');
            if (tIdx > 0)
            {
                var before = titleLower[..tIdx].Trim();
                if (before.Length > 0 && lineLower.Contains(before)) return true;
            }

            // title has " - " => compare before " - "
            int dash = titleLower.IndexOf(" - ", StringComparison.Ordinal);
            if (dash > 0)
            {
                var before = titleLower[..dash].Trim();
                if (before.Length > 0 && lineLower.Contains(before)) return true;
            }

            return false;
        }

        private static bool ContainsArtists(string lineLower, ITrackMetadata trackInfo, string singleArtistNorm)
        {
            if (!string.IsNullOrWhiteSpace(singleArtistNorm) && lineLower.Contains(singleArtistNorm))
                return true;

            if (trackInfo is TrackMultiArtistMetadata multi && multi.Artists is { Count: > 0 })
            {
                int hit = 0;
                foreach (var a in multi.Artists)
                {
                    if (string.IsNullOrWhiteSpace(a)) continue;
                    var norm = a.ToLowerInvariant().ToSC(true).Replace("’", "'").Replace(", ", "/");
                    if (norm.Length > 0 && lineLower.Contains(norm))
                        hit++;
                }

                if (hit > 1 || (hit == 1 && multi.Artists.Count == 1))
                    return true;
            }

            return false;
        }

        private static bool IsStringTencentClaiming(string str)
        {
            if (str is null) return false;

            var s = str.ToSC();
            return (s.Contains("腾讯") || s.Contains("TME"))
                   && s.Contains("享有")
                   && s.Contains("翻译")
                   && s.Contains('权');
        }

        private static bool IsStringCreditBy(string str)
        {
            if (ContainsAny(str,
                    "st:", "or:", "Lyrics:", " by:", " By:"))
                return true;

            if (str.Contains("er:", StringComparison.OrdinalIgnoreCase)
                && !str.Contains("Tedder:", StringComparison.OrdinalIgnoreCase)
                && !str.Contains("Bieber:", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private static bool IsStringCopyrightClaiming(string str)
        {
            int score = 0;
            if (str.Contains("未经")) score++;
            if (str.Contains("许可")) score++;
            if (str.Contains("授权")) score++;
            if (str.Contains("不得")) score++;
            if (str.Contains("请勿")) score++;
            if (str.Contains("使用")) score++;
            if (str.Contains("版权")) score++;

            return score >= 4;
        }
    }
}