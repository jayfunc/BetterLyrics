using BetterLyrics.WinUI3.Services.LocalizationService;
using CommunityToolkit.Mvvm.DependencyInjection;
using System;

namespace BetterLyrics.WinUI3.Helper
{
    public static class PhoneticHelper
    {
        private static readonly ILocalizationService _localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();

        public const string PinyinCode = "zh-cmn-pinyin";
        public const string JyutpingCode = "zh-yue-jyutping";
        public const string RomanCode = "ja-latin";

        public static bool IsPhoneticCode(string code)
        {
            return code == PinyinCode || code == JyutpingCode || code == RomanCode;
        }

        public static string GetDisplayName(string code)
        {
            switch (code)
            {
                case PinyinCode:
                    return _localizationService.GetLocalizedString("Pinyin");
                case JyutpingCode:
                    return _localizationService.GetLocalizedString("Jyutping");
                case RomanCode:
                    return _localizationService.GetLocalizedString("Romaji");
                default:
                    throw new ArgumentOutOfRangeException(nameof(code));
            }
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
