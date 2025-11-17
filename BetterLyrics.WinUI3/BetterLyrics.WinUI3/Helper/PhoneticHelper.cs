using BetterLyrics.WinUI3.Services.ResourceService;
using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.Linq;

namespace BetterLyrics.WinUI3.Helper
{
    public static class PhoneticHelper
    {
        private static readonly IResourceService _resourceService = Ioc.Default.GetRequiredService<IResourceService>();

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
                    return _resourceService.GetLocalizedString("Pinyin");
                case JyutpingCode:
                    return _resourceService.GetLocalizedString("Jyutping");
                case RomajiCode:
                    return _resourceService.GetLocalizedString("Romaji");
                default:
                    throw new ArgumentOutOfRangeException(nameof(code));
            }
        }

        public static string ToRomaji(string text)
        {
            return Kana.Kana.KanaToRomaji(text, Kana.Error.Ignore).ToStr();
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
