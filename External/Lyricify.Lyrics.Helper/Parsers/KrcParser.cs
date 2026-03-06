using Lyricify.Lyrics.Decrypter.Krc;
using Lyricify.Lyrics.Helpers.General;
using Lyricify.Lyrics.Models;
using Newtonsoft.Json;
using System.Text;

namespace Lyricify.Lyrics.Parsers
{
    public static class KrcParser
    {
        public static LyricsData Parse(string krc)
        {
            var data = new LyricsData
            {
                File = new()
                {
                    Type = LyricsTypes.Krc,
                    SyncTypes = SyncTypes.SyllableSynced,
                    AdditionalInfo = new KrcAdditionalInfo()
                    {
                        Attributes = new(),
                    },
                },
                TrackMetadata = new TrackMetadata()
            };

            var lyricsLines = GetSplitedKrc(krc).ToList();

            // 处理 Attributes
            var offset = AttributesHelper.ParseGeneralAttributesToLyricsData(data, lyricsLines);

            // 处理歌词
            var lyrics = ParseLyrics(lyricsLines, offset);
            if (KrcTranslationParser.CheckKrcTranslation(krc))
            {
                var lyricsTrans = KrcTranslationParser.GetTranslationFromKrc(krc);
                if (lyricsTrans != null)
                {
                    for (int i = 0; i < lyrics.Count && i < lyricsTrans.Count; i++)
                    {
                        string t = lyricsTrans[i];
                        t = t != "//" ? t : "";
                        lyrics[i] = new FullSyllableLineInfo((SyllableLineInfo)lyrics[i], chineseTranslation: t);
                    }
                }
            }

            data.Lines = lyrics;

            return data;
        }

        public static List<ILineInfo> ParseLyrics(string krc)
        {
            var lyricsLines = GetSplitedKrcWithoutInfoLine(krc).ToList();
            var lyrics = ParseLyrics(lyricsLines);

            if (KrcTranslationParser.CheckKrcTranslation(krc))
            {
                var lyricsTrans = KrcTranslationParser.GetTranslationFromKrc(krc);
                if (lyricsTrans != null)
                {
                    for (int i = 0; i < lyrics.Count && i < lyricsTrans.Count; i++)
                    {
                        string t = lyricsTrans[i];
                        t = t != "//" ? t : "";
                        lyrics[i] = new FullSyllableLineInfo((SyllableLineInfo)lyrics[i], chineseTranslation: t);
                    }
                }
            }

            return lyrics;
        }

        public static List<ILineInfo> ParseLyrics(List<string> lyricsLines, int? offset = null)
        {
            var lyrics = new List<ILineInfo>();

            foreach (var line in lyricsLines)
            {
                if (line.StartsWith('['))
                {
                    var l = ParseLyricsLine(line);
                    if (l != null)
                    {
                        lyrics.Add(l);
                    }
                }
            }

            if (offset.HasValue && offset != 0)
            {
                Helpers.OffsetHelper.AddOffset(lyrics, offset.Value);
            }

            return lyrics;
        }

        /// <summary>
        /// 按行拆分 Krc 歌词
        /// </summary>
        /// <param name="krc"></param>
        /// <returns></returns>
        private static string[] GetSplitedKrc(string krc)
        {
            string[] lines = krc
                .Replace("\r\n", "\n")
                .Replace("\r", "")
                .Split('\n');
            var stringBuilder = new StringBuilder();
            foreach (string line in lines)
            {
                if (line.StartsWith("["))
                {
                    // 确保错误的行头被去除
                    stringBuilder.AppendLine(line);
                }
            }
            return stringBuilder.ToString()
                .Replace("\r\n", "\n")
                .Replace("\r", "")
                .Split('\n');
        }

        /// <summary>
        /// 按行拆分 Krc 歌词 (移除附加信息)
        /// </summary>
        /// <param name="krc"></param>
        /// <returns></returns>
        private static string[] GetSplitedKrcWithoutInfoLine(string krc)
        {
            string[] lines = krc
                .Replace("\r\n", "\n")
                .Replace("\r", "")
                .Split('\n');
            var stringBuilder = new StringBuilder();
            foreach (string line in lines)
            {
                if (line.StartsWith("[") && line.Length >= 5 && line[1].ToString().IsNumber())
                {
                    stringBuilder.AppendLine(line);
                }
            }
            return stringBuilder.ToString()
                .Replace("\r\n", "\n")
                .Replace("\r", "")
                .Split('\n');
        }

        /// <summary>
        /// 将 Krc 歌词行转换为 SyllableLineInfo
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public static SyllableLineInfo? ParseLyricsLine(string line)
        {
            string[] words = line[(line.IndexOf(']') + 1)..].Split(",0>");
            if (words.Length < 1) return null;
            string[] lineTime = line[1..line.IndexOf(']')].Split(',');
            int lineStart = int.Parse(lineTime[0]);

            var syllables = new List<ISyllableInfo>();

            var time = words[0][1..].Split(',');
            int start = int.Parse(time[0]);
            int duration = int.Parse(time[1]);
            for (int i = 1; i < words.Length; i++)
            {
                var word = words[i];
                if (word.Contains('<'))
                {
                    word = word[..word.LastIndexOf("<")];
                }
                syllables.Add(new SyllableInfo()
                {
                    StartTime = lineStart + start,
                    EndTime = lineStart + start + duration,
                    Text = word,
                });
                if (words[i].Contains('<'))
                {
                    time = words[i][(words[i].LastIndexOf("<") + 1)..].Split(',');
                    start = int.Parse(time[0]);
                    duration = int.Parse(time[1]);
                }
            }
            return new(syllables);
        }
    }

    public static class KrcTranslationParser
    {
        /// <summary>
        /// 检查 KRC 中是否有翻译
        /// </summary>
        public static bool CheckKrcTranslation(string krc)
        {
            if (!krc.Contains("[language:")) return false;

            try
            {
                var language = krc[(krc.IndexOf("[language:") + "[language:".Length)..];
                language = language[..language.IndexOf(']')];
                var decode = Encoding.UTF8.GetString(Convert.FromBase64String(language));

                var translation = JsonConvert.DeserializeObject<KugouTranslation>(decode);
                if (translation!.Content!.Count > 0) return true;
            }
            catch { }

            return false;
        }

        /// <summary>
        /// 提取 KRC 中的翻译
        /// </summary>
        /// <param name="krc">KRC 歌词</param>
        /// <returns>翻译 List，若无翻译，则返回 null</returns>
        public static List<string>? GetTranslationFromKrc(string krc)
        {
            if (!krc.Contains("[language:")) return null;

            var language = krc[(krc.IndexOf("[language:") + "[language:".Length)..];
            language = language[..language.IndexOf(']')];
            var decode = Encoding.UTF8.GetString(Convert.FromBase64String(language));

            var translation = JsonConvert.DeserializeObject<KugouTranslation>(decode);

            if (translation == null || translation!.Content == null || translation!.Content!.Count == 0) return null;

            try
            {
                var result = new List<string>();
                var content = translation!.Content.FirstOrDefault(t => t.Type == 1);
                if (content == null) return null;

                for (int i = 0; i < content.LyricContent!.Count; i++)
                {
                    result.Add(content.LyricContent![i]![0]);
                }

                return result;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 提取 KRC 中的翻译原始数据
        /// </summary>
        /// <param name="krc">KRC 歌词</param>
        /// <returns>翻译 List，若无翻译，则返回 null</returns>
        public static KugouTranslation? GetTranslationRawFromKrc(string krc)
        {
            if (!krc.Contains("[language:")) return null;

            var language = krc[(krc.IndexOf("[language:") + "[language:".Length)..];
            language = language[..language.IndexOf(']')];
            var decode = Encoding.UTF8.GetString(Convert.FromBase64String(language));

            return JsonConvert.DeserializeObject<KugouTranslation>(decode);
        }
    }
}
