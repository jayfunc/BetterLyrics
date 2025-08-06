using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Constants
{
    public static class AmllTTmlDB
    {
        private const string BaseUrl = "https://raw.githubusercontent.com/Steve-xmh/amll-ttml-db/refs/heads/main/";
        public const string QueryPrefix = $"{BaseUrl}raw-lyrics/";
        public const string Index = $"{BaseUrl}metadata/raw-lyrics-index.jsonl";
    }
}
