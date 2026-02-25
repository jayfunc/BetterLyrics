using Lyricify.Lyrics.Helpers.General;
using Lyricify.Lyrics.Models;

namespace Lyricify.Lyrics.Parsers
{
    internal static class AttributesHelper
    {
        /// <summary>
        /// 将 Attributes 信息解析到 LyricsData 中
        /// </summary>
        /// <returns>Offset 值，若 Attributes 中没有，则为 <see langword="null"/></returns>
        public static int? ParseGeneralAttributesToLyricsData(LyricsData data, string input, out int index)
        {
            int? offset = null;
            data.TrackMetadata ??= new TrackMetadata();

            index = 0;
            for (; index < input.Length; index++)
            {
                if (input[index] == '[')
                {
                    var endIndex = input.IndexOf('\n', index);
                    var infoLine = input[index..endIndex];
                    if (IsAttributeLine(infoLine))
                    {
                        var attribute = GetAttribute(infoLine);
                        switch (attribute.Key)
                        {
                            case "ar":
                                data.TrackMetadata.Artist = attribute.Value;
                                break;
                            case "al":
                                data.TrackMetadata.Album = attribute.Value;
                                break;
                            case "ti":
                                data.TrackMetadata.Title = attribute.Value;
                                break;
                            case "length":
                                if (int.TryParse(attribute.Value, out int result))
                                    data.TrackMetadata.DurationMs = result;
                                break;
                            case "offset":
                                try { offset = int.Parse(attribute.Value); } catch { }
                                break;
                        }
                        ((GeneralAdditionalInfo)data.File!.AdditionalInfo!).Attributes!.Add(attribute);

                        index = endIndex;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            return offset;
        }

        /// <summary>
        /// 将 Attributes 信息解析到 LyricsData 中
        /// </summary>
        /// <returns>Offset 值，若 Attributes 中没有，则为 <see langword="null"/></returns>
        public static int? ParseGeneralAttributesToLyricsData(LyricsData data, List<string> lines)
        {
            int? offset = null;
            data.TrackMetadata ??= new TrackMetadata();
            for (int i = 0; i < lines.Count; i++)
            {
                if (IsAttributeLine(lines[i]))
                {
                    var attribute = GetAttribute(lines[i]);
                    switch (attribute.Key)
                    {
                        case "ar":
                            data.TrackMetadata.Artist = attribute.Value;
                            break;
                        case "al":
                            data.TrackMetadata.Album = attribute.Value;
                            break;
                        case "ti":
                            data.TrackMetadata.Title = attribute.Value;
                            break;
                        case "length":
                            if (int.TryParse(attribute.Value, out int result))
                                data.TrackMetadata.DurationMs = result;
                            break;
                        case "offset":
                            try { offset = int.Parse(attribute.Value); } catch { }
                            break;
                    }
                    if (attribute.Key == "hash" && data.File!.AdditionalInfo is KrcAdditionalInfo krcAdditionalInfo)
                        krcAdditionalInfo.Hash = attribute.Value;
                    else
                        ((GeneralAdditionalInfo)data.File!.AdditionalInfo!).Attributes!.Add(attribute);

                    lines.RemoveAt(i--);
                }
                else
                {
                    break;
                }
            }
            return offset;
        }

        /// <summary>
        /// 是否是 Attribute 信息行
        /// </summary>
        public static bool IsAttributeLine(string line)
        {
            line = line.Trim(); // 防止 \r 干扰
            return line.StartsWith('[') && line.EndsWith(']') && line.Contains(':');
        }

        /// <summary>
        /// 获取 Attribute 信息
        /// </summary>
        private static KeyValuePair<string, string> GetAttribute(string line)
        {
            line = line.Trim(); // 防止 \r 干扰
            string key = line.Between("[", ":");
            string value = line[(line.IndexOf(':') + 1)..^1];
            return new KeyValuePair<string, string>(key, value);
        }
    }
}
