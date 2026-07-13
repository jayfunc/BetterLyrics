using BetterLyrics.Core.Models.Lyrics;

namespace BetterLyrics.Core.Helpers.Lyrics;

public static class LyricsLayoutHelper
{
    // 硬性限制
    private const float BaseMinFontSize = 14f;
    private const float BaseMaxFontSize = 80f;
    private const float TargetMinVisibleLines = 5f;
    private const float WidthPaddingRatio = 0.85f;

    // 比例配置
    private const float RatioSongTitle = 1f;
    private const float RatioArtist = 0.85f;
    private const float RatioAlbum = 0.75f;
    private const float RatioTranslation = 0.7f;
    private const float RatioTransliteration = 0.55f;
    private const float AbsoluteMinReadableSize = 10f;

    public static LyricsLayoutMetrics CalculateLayout(double width, double height)
    {
        var baseSize = CalculateBaseFontSize(width, height);

        return new LyricsLayoutMetrics
        {
            MainLyricsSize = baseSize,
            TranslationSize = ApplyRatio(baseSize, RatioTranslation),
            TransliterationSize = ApplyRatio(baseSize, RatioTransliteration),
            SongTitleSize = ApplyRatio(baseSize, RatioSongTitle),
            ArtistNameSize = ApplyRatio(baseSize, RatioArtist),
            AlbumNameSize = ApplyRatio(baseSize, RatioAlbum)
        };
    }

    private static float CalculateBaseFontSize(double width, double height)
    {
        var usableWidth = (float)width * WidthPaddingRatio;

        // 宽度 300~500px 时，除以 14 (字大)
        // 宽度 >1000px 时，除以 30 (字适中，展示更多内容)
        float targetCharsPerLine;
        if (width < 500)
        {
            targetCharsPerLine = 14f;
        }
        else if (width > 1000)
        {
            targetCharsPerLine = 30f;
        }
        else
        {
            // 平滑过渡
            var t = (float)(width - 500) / 500f;
            targetCharsPerLine = 14f + 16f * t;
        }

        var sizeByWidth = usableWidth / targetCharsPerLine;
        var sizeByHeight = (float)height / TargetMinVisibleLines;

        var targetSize = Math.Min(sizeByWidth, sizeByHeight);

        // 窄屏时底线设高一点 (16px)，宽屏如果高度不够可能允许更小
        var currentMinLimit = width < 400 ? 16f : BaseMinFontSize;

        return Math.Clamp(targetSize, currentMinLimit, BaseMaxFontSize);
    }

    private static float ApplyRatio(float baseSize, float ratio)
    {
        return Math.Max(baseSize * ratio, AbsoluteMinReadableSize);
    }
}