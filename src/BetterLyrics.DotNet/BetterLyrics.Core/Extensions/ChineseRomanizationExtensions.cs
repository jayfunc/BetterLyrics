using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;

namespace BetterLyrics.Core.Extensions;

public static class ChineseRomanizationExtensions
{
    extension(ChineseRomanization chineseRomanization)
    {
        public string ToPhoneticCode()
        {
            return chineseRomanization switch
            {
                ChineseRomanization.Pinyin => LanguageHelper.PinyinCode,
                ChineseRomanization.Jyutping => LanguageHelper.JyutpingCode,
                _ => throw new ArgumentOutOfRangeException(nameof(chineseRomanization))
            };
        }
    }
}