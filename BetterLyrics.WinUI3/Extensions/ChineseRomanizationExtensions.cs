using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using System;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class ChineseRomanizationExtensions
    {
        extension(ChineseRomanization chineseRomanization)
        {
            public string ToPhoneticCode() => chineseRomanization switch
            {
                ChineseRomanization.Pinyin => LanguageHelper.PinyinCode,
                ChineseRomanization.Jyutping => LanguageHelper.JyutpingCode,
                _ => throw new ArgumentOutOfRangeException(nameof(chineseRomanization))
            };
        }
    }
}
