using BetterLyrics.WinUI3.Helper;
using System;

namespace BetterLyrics.WinUI3.Enums
{
    public enum ChineseRomanization
    {
        Pinyin,
        Jyutping,
    }

    public static class ChineseRomanizationExtensions
    {
        public static string ToPhoneticCode(this ChineseRomanization chineseRomanization)
        {
            return chineseRomanization switch
            {
                ChineseRomanization.Pinyin => PhoneticHelper.PinyinCode,
                ChineseRomanization.Jyutping => PhoneticHelper.JyutpingCode,
                _ => throw new ArgumentOutOfRangeException(nameof(chineseRomanization))
            };
        }
    }
}
