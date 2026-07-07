using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Extensions;

public static class LyricsCardConfigExtensions
{
    public static LyricsCardConfig GetDefaultLyricsCardConfig(string resourceKey)
    {
        return new LyricsCardConfig
        {
            ResourceKey = resourceKey,
            FontFamily = resourceKey switch
            {
                "LyricsCardAncientBookStyle" =>
                    "FZSongKeBenXiuKai, Kangxi Zidian Ti, FZBoYaSong, Noto Serif SC, SimSun",
                "LyricsCardBambooSlipsStyle" => "FZLiShu, LiSu, STZhongsong",
                "LyricsCardCDStyle" => "Futura, Century Gothic, Helvetica, Segoe UI",
                "LyricsCardCinematicStyle" => "Optima, Trajan Pro, Noto Serif SC, Georgia, 霞鹜致宋 CL",
                "LyricsCardCyberpunkStyle" => "Orbitron, Rajdhani, Roboto Mono, Consolas, LXGW WenKai Mono",
                "LyricsCardDunhuangStyle" => "FZWeiBei, FZSuXinShiLiuKai, STKaiti, KaiTi",
                "LyricsCardInkWashStyle" => "STXingkai, FZShouJinShu, STKaiti, KaiTi",
                "LyricsCardJournalStyle" => "Bradley Hand, Caveat, LXGW WenKai, KaiTi, Segoe Print",
                "LyricsCardMagazineStyle" => "Didot, Bodoni MT, Playfair Display, Noto Serif SC, 霞鹜致宋 CL",
                "LyricsCardMinimalStyle" => "Inter, SF Pro Display, Helvetica Neue, Segoe UI",
                "LyricsCardPodStyle" => "Myriad Pro, SF Pro Text, Helvetica, Segoe UI",
                "LyricsCardPolaroidStyle" => "Permanent Marker, Caveat, Ink Free, Xiaolai, 方正舒体",
                "LyricsCardReceiptStyle" => "VT323, Space Mono, DotMatrix, Courier New, 霞鹜新致宋",
                "LyricsCardRetroMSNStyle" => "Tahoma, MS Sans Serif",
                "LyricsCardRetroQQStyle" => "SimSun, Tahoma",
                "LyricsCardStickyNoteStyle" => "Patrick Hand, Comic Sans MS, LXGW WenKai, KaiTi",
                "LyricsCardSwissStyle" => "Helvetica Neue, Helvetica, Neue Haas Grotesk, Arial",
                "LyricsCardTicketStyle" => "OCR A Extended, DIN Alternate, Courier New, 霞鹜新致宋",
                "LyricsCardVinylStyle" => "Cooper Black, Futura, Helvetica, Segoe UI",
                "LyricsCardWindowsPhoneStyle" => "Segoe WP, Segoe UI",
                _ => "Segoe UI"
            }
        };
    }
}