using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Helper
{
    public class PhoneticHelper
    {
        public const string PinyinCode = "zh-pinyin";
        public const string JyutpingCode = "zh-jyutping";
        public const string RomajiCode = "ja-romaji";

        public static bool IsPhoneticCode(string code)
        {
            return code == PinyinCode || code == JyutpingCode || code == RomajiCode;
        }

        public static string GetDisplayName(string code)
        {
            switch (code)
            {
                case PinyinCode:
                    return App.ResourceLoader!.GetString("Pinyin");
                case JyutpingCode:
                    return App.ResourceLoader!.GetString("Jyutping");
                case RomajiCode:
                    return App.ResourceLoader!.GetString("Romaji");
                default:
                    throw new ArgumentOutOfRangeException(nameof(code));
            }
        }

        public static string ToRomaji(string text)
        {
            return Kana.Kana.KanaToRomaji(text).ToStr();
        }

        public static string ToPinyin(string text, Pinyin.ManTone.Style style = Pinyin.ManTone.Style.TONE)
        {
            return Pinyin.Pinyin.Instance.HanziToPinyin(text, style).ToStr();
        }

        public static string ToJyutping(string text)
        {
            return Pinyin.Jyutping.Instance.HanziToPinyin(text).ToStr();
        }
    }
}
