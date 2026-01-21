using BetterLyrics.WinUI3.Models;
using System.IO;
using System.Xml;

namespace BetterLyrics.WinUI3.Parsers.LyricsMetadataParser
{
    public partial class LyricsMetadataParser
    {
        private static LyricsMetadata ParseTtml(string content)
        {
            LyricsMetadata metadata = new();
            if (content == null) return metadata;

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

                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                string tagName = reader.Name.ToLower();

                                switch (tagName)
                                {
                                    case "ttm:title":
                                        metadata.Title = reader.ReadElementContentAsString();
                                        break;
                                    case "ttm:desc":
                                    case "ttm:description":
                                        metadata.Comments.Add(reader.ReadElementContentAsString());
                                        break;
                                    case "ttm:copyright":
                                        metadata.Comments.Add("Copyright: " + reader.ReadElementContentAsString());
                                        break;
                                    case "ttm:agent":
                                        ParseTtmlAgent(reader, metadata);
                                        break;
                                    case "amll:meta":
                                        ParseAmllMeta(reader, metadata);
                                        break;
                                    case "body":
                                        metadata.Length = reader.GetAttribute("dur") ?? "00:00";
                                        return metadata;
                                }
                            }
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
            string? role = reader.GetAttribute("role");

            string content = reader.ReadElementContentAsString();

            if (string.IsNullOrWhiteSpace(role)) return;

            if (role.Contains("artist") || role.Contains("performer"))
            {
                metadata.Artist = content;
            }
            else if (role.Contains("composer") || role.Contains("author"))
            {
                metadata.Author = content;
            }
            else if (role.Contains("lyricist"))
            {
                metadata.Lyricist = content;
            }
        }

        private static void ParseAmllMeta(XmlReader reader, LyricsMetadata metadata)
        {
            string? key = reader.GetAttribute("key");
            string? value = reader.GetAttribute("value");

            if (string.IsNullOrWhiteSpace(key)) return;
            if (string.IsNullOrWhiteSpace(value)) return;

            if (key == "musicName")
            {
                metadata.Title = value;
            }
            else if (key == "artists")
            {
                metadata.Artist = value;
            }
            else if (key == "album")
            {
                metadata.Album = value;
            }
        }

    }
}
