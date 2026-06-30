using BetterLyrics.Core.Interfaces.Providers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using WanaKanaNet;

namespace BetterLyrics.Avalonia.Providers
{
    public class StringConverterProvider : IStringConverterProvider
    {
        public string RomajiToKanji(string romaji)
        {
            return romaji;
        }
    }
}
