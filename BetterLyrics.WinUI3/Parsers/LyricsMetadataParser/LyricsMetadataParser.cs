using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BetterLyrics.WinUI3.Parsers.LyricsMetadataParser
{
    public partial class LyricsMetadataParser
    {
        public static LyricsMetadata Parse(string content, string ext)
        {
            LyricsMetadata metadata = new();
            if (ext == ".ttml")
            {
                metadata = ParseTtml(content);
            }
            else
            {
                metadata = ParseLrc(content);
            }
            return metadata;
        }
    }
}
