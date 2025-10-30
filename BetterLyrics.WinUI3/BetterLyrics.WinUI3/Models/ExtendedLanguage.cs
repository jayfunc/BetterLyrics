using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Globalization;

namespace BetterLyrics.WinUI3.Models
{
    public class ExtendedLanguage
    {
        public string Name { get; private set; }
        public string LanguageCode { get; private set; }

        public ExtendedLanguage(string languageCode, string? name = null)
        {
            LanguageCode = languageCode;
            Name = name ?? new Language(languageCode).NativeName;
        }
    }
}
