using System.Xml;
using BetterLyrics.Core.Models.Lyrics;

namespace BetterLyrics.Core.Helpers.Lyrics.MetadataParser;

/// <summary>
///     This TTML metadata parser follows the format specification:
///     https://github.com/amll-dev/amll-ttml-db/wiki/%E6%A0%BC%E5%BC%8F%E8%A7%84%E8%8C%83
/// </summary>
public partial class LyricsMetadataParser
{
    private static LyricsMetadata ParseTtml(string content)
    {
        LyricsMetadata metadata = new();
        if (string.IsNullOrWhiteSpace(content)) return metadata;

        var settings = new XmlReaderSettings
        {
            IgnoreWhitespace = true,
            IgnoreComments = true,
            DtdProcessing = DtdProcessing.Ignore
        };

        using (var stringReader = new StringReader(content))
        {
            using (var reader = XmlReader.Create(stringReader, settings))
            {
                try
                {
                    reader.MoveToContent();

                    while (!reader.EOF)
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            var tagName = reader.Name.ToLower();

                            switch (tagName)
                            {
                                case "ttm:title":
                                    // 不要同时在 <ttm:title> 和 musicName 标签添加相同的值
                                    var title = reader.ReadElementContentAsString();
                                    if (string.IsNullOrWhiteSpace(metadata.Title))
                                        metadata.Title = title;
                                    continue;

                                case "ttm:agent":
                                    ParseTtmlAgent(reader, metadata);
                                    break;

                                case "amll:meta":
                                    ParseAmllMeta(reader, metadata);
                                    break;

                                case "songwriter":
                                    // Apple Music 扩展的歌曲创作者信息
                                    var songwriter = reader.ReadElementContentAsString();
                                    if (!string.IsNullOrWhiteSpace(songwriter))
                                    {
                                        if (string.IsNullOrWhiteSpace(metadata.Author))
                                            metadata.Author = songwriter;
                                        else if (!metadata.Author.Contains(songwriter))
                                            metadata.Author += "/" + songwriter;

                                        if (string.IsNullOrWhiteSpace(metadata.Lyricist))
                                            metadata.Lyricist = songwriter;
                                        else if (!metadata.Lyricist.Contains(songwriter))
                                            metadata.Lyricist += "/" + songwriter;
                                    }

                                    continue;

                                case "body":
                                    // dur 是可选的。如果为空，保留原有 Length
                                    var dur = reader.GetAttribute("dur");
                                    if (!string.IsNullOrWhiteSpace(dur)) metadata.Length = dur;
                                    return metadata;
                            }
                        }

                        reader.Read();
                    }
                }
                catch (XmlException)
                {
                }
            }
        }

        return metadata;
    }

    private static void ParseTtmlAgent(XmlReader reader, LyricsMetadata metadata)
    {
        if (reader.IsEmptyElement) return;

        using (var innerReader = reader.ReadSubtree())
        {
            while (innerReader.Read())
                if (innerReader.NodeType == XmlNodeType.Element && innerReader.Name.ToLower() == "ttm:name")
                {
                    var name = innerReader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        if (string.IsNullOrWhiteSpace(metadata.Artist))
                            metadata.Artist = name;
                        else if (!metadata.Artist.Contains(name)) metadata.Artist += "/" + name;
                    }
                }
        }
    }

    private static void ParseAmllMeta(XmlReader reader, LyricsMetadata metadata)
    {
        var key = reader.GetAttribute("key");
        var value = reader.GetAttribute("value");

        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value)) return;

        switch (key)
        {
            case "musicName":
                // 存在多个 musicName 标签时，优先使用第一个非空值作为标题
                if (string.IsNullOrWhiteSpace(metadata.Title)) metadata.Title = value;
                break;
            case "artists":
                // 此标签通常提供完整的艺人名称，优先级高于 ttm:agent 累加
                metadata.Artist = value;
                break;
            case "album":
                metadata.Album = value;
                break;
            case "ttmlAuthorGithub":
            case "ttmlAuthorGithubLogin":
                // 将 AMLL 规定的逐词歌词作者映射到 LRC 创建者字段
                if (string.IsNullOrWhiteSpace(metadata.LrcCreator))
                    metadata.LrcCreator = value;
                else if (!metadata.LrcCreator.Contains(value)) metadata.LrcCreator += $" ({value})";
                break;
        }
    }
}