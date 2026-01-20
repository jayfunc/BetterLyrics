using BetterLyrics.WinUI3.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using TagLib.Flac;

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
                            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "body")
                            {
                                break;
                            }

                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                string tagName = reader.LocalName.ToLower();

                                switch (tagName)
                                {
                                    case "title":
                                        metadata.Title = reader.ReadElementContentAsString();
                                        break;
                                    case "desc":
                                    case "description":
                                        metadata.Comments.Add(reader.ReadElementContentAsString());
                                        break;
                                    case "copyright":
                                        metadata.Comments.Add("Copyright: " + reader.ReadElementContentAsString());
                                        break;
                                    case "agent":
                                        ParseTtmlAgent(reader, metadata);
                                        break;
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

    }
}
