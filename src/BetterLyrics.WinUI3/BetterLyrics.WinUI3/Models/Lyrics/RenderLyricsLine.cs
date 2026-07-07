using System.Collections.Generic;
using System.Linq;
using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models.Lyrics;
using BetterLyrics.WinUI3.Extensions;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;

namespace BetterLyrics.WinUI3.Models.Lyrics;

public class RenderLyricsLine : BaseRenderLyricsLine
{
    public RenderLyricsLine(LyricsLine lyricsLine) : base(lyricsLine)
    {
        PrimaryRenderSyllables = lyricsLine.PrimarySyllables.Select(x => new RenderLyricsSyllable(x)).ToList();
    }

    public new List<RenderLyricsChar> PrimaryRenderChars { get; } = [];
    public new List<RenderLyricsSyllable> PrimaryRenderSyllables { get; }

    public CanvasTextLayout? PrimaryTextLayout { get; private set; }
    public CanvasTextLayout? SecondaryTextLayout { get; private set; }
    public CanvasTextLayout? TertiaryTextLayout { get; private set; }

    public CanvasGeometry? PrimaryCanvasGeometry { get; private set; }
    public CanvasGeometry? SecondaryCanvasGeometry { get; private set; }
    public CanvasGeometry? TertiaryCanvasGeometry { get; private set; }

    public CanvasCommandList? CachedStroke { get; private set; }
    public CanvasCommandList? CachedFill { get; private set; }

    public TintEffect? UnplayedFillTint { get; private set; }
    public TintEffect? UnplayedStrokeTint { get; private set; }
    public CompositeEffect? UnplayedComposite { get; private set; }

    public CanvasTextLayoutRegion[]? PrimaryTextRegions { get; private set; }

    public RenderLyricsRegion[]? RenderLyricsRegions { get; private set; }

    public void DisposeTextLayout()
    {
        TertiaryTextLayout?.Dispose();
        TertiaryTextLayout = null;

        PrimaryTextLayout?.Dispose();
        PrimaryTextLayout = null;

        SecondaryTextLayout?.Dispose();
        SecondaryTextLayout = null;
    }

    public void RecreateTextLayout(
        ICanvasAnimatedControl control,
        bool createPhonetic, bool createTranslated,
        int phoneticTextFontSize, int originalTextFontSize, int translatedTextFontSize,
        LyricsFontWeight fontWeight,
        string fontFamilyCJK, string fontFamilyWestern,
        double maxWidth, double maxHeight,
        TextAlignmentType type, bool autoWrap, LyricsLayoutOrientation orientation)
    {
        DisposeTextLayout();

        var wordWrapping = autoWrap ? CanvasWordWrapping.Wrap : CanvasWordWrapping.NoWrap;
        var horizontalAlignment = type.ToCanvasHorizontalAlignment();

        var phoneticVisible = createPhonetic && !string.IsNullOrWhiteSpace(TertiaryText);
        var translatedVisible = createTranslated && !string.IsNullOrWhiteSpace(SecondaryText);

        var verticalAlignment = CanvasVerticalAlignment.Top;
        var canvasTextDirection = orientation == LyricsLayoutOrientation.Vertical
            ? CanvasTextDirection.TopToBottomThenRightToLeft
            : CanvasTextDirection.LeftToRightThenTopToBottom;

        // 音译
        if (phoneticVisible)
        {
            TertiaryTextLayout = new CanvasTextLayout(control, TertiaryText, new CanvasTextFormat
            {
                VerticalAlignment = verticalAlignment,
                FontSize = phoneticTextFontSize,
                FontWeight = fontWeight.ToFontWeight(),
                WordWrapping = wordWrapping
            }, (float)maxWidth, (float)maxHeight)
            {
                HorizontalAlignment = horizontalAlignment,
                Options = CanvasDrawTextOptions.NoPixelSnap,
                Direction = canvasTextDirection
            };
            TertiaryTextLayout.SetFontFamily(TertiaryText, fontFamilyCJK, fontFamilyWestern);
        }

        // 原文
        PrimaryTextLayout = new CanvasTextLayout(control, PrimaryText, new CanvasTextFormat
        {
            VerticalAlignment = verticalAlignment,
            FontSize = originalTextFontSize,
            FontWeight = fontWeight.ToFontWeight(),
            WordWrapping = wordWrapping
        }, (float)maxWidth, (float)maxHeight)
        {
            HorizontalAlignment = horizontalAlignment,
            Options = CanvasDrawTextOptions.NoPixelSnap,
            Direction = canvasTextDirection
        };
        PrimaryTextLayout.SetFontFamily(PrimaryText, fontFamilyCJK, fontFamilyWestern);
        PrimaryTextRegions = PrimaryTextLayout.GetCharacterRegions(0, PrimaryText.Length);

        // 翻译
        if (translatedVisible)
        {
            SecondaryTextLayout = new CanvasTextLayout(control, SecondaryText, new CanvasTextFormat
            {
                VerticalAlignment = verticalAlignment,
                FontSize = translatedTextFontSize,
                FontWeight = fontWeight.ToFontWeight(),
                WordWrapping = wordWrapping
            }, (float)maxWidth, (float)maxHeight)
            {
                HorizontalAlignment = horizontalAlignment,
                Options = CanvasDrawTextOptions.NoPixelSnap,
                Direction = canvasTextDirection
            };
            SecondaryTextLayout.SetFontFamily(SecondaryText, fontFamilyCJK, fontFamilyWestern);
        }
    }

    public void DisposeTextGeometry()
    {
        TertiaryCanvasGeometry?.Dispose();
        TertiaryCanvasGeometry = null;

        PrimaryCanvasGeometry?.Dispose();
        PrimaryCanvasGeometry = null;

        SecondaryCanvasGeometry?.Dispose();
        SecondaryCanvasGeometry = null;
    }

    public void RecreateTextGeometry()
    {
        DisposeTextGeometry();

        if (TertiaryTextLayout != null) TertiaryCanvasGeometry = CanvasGeometry.CreateText(TertiaryTextLayout);

        if (PrimaryTextLayout != null) PrimaryCanvasGeometry = CanvasGeometry.CreateText(PrimaryTextLayout);

        if (SecondaryTextLayout != null) SecondaryCanvasGeometry = CanvasGeometry.CreateText(SecondaryTextLayout);
    }

    public void RecreateRenderChars(int strokeWidth)
    {
        PrimaryRenderChars.Clear();
        if (PrimaryTextLayout == null) return;

        foreach (var syllable in PrimaryRenderSyllables) syllable.ChildrenRenderLyricsChars.Clear();

        var textLength = PrimaryText.Length;

        for (var startCharIndex = 0; startCharIndex < textLength; startCharIndex++)
        {
            var region = PrimaryTextLayout.GetCharacterRegions(startCharIndex, 1).FirstOrDefault();
            var bounds = region.LayoutBounds.Extend(
                startCharIndex == 0 ? strokeWidth : strokeWidth / 4f,
                strokeWidth / 2f,
                startCharIndex == textLength - 1 ? strokeWidth : strokeWidth / 4f,
                strokeWidth / 2f).ToAppRect();

            var syllable = PrimaryRenderSyllables.FirstOrDefault(x =>
                x.StartIndex <= startCharIndex && startCharIndex <= x.EndIndex);
            if (syllable == null) continue;

            var avgCharDuration = syllable.DurationMs / syllable.Length;
            var charStartMs = syllable.StartMs + (startCharIndex - syllable.StartIndex) * avgCharDuration;
            var charEndMs = charStartMs + avgCharDuration;

            var renderLyricsChar = new RenderLyricsChar(new BaseLyrics
            {
                StartIndex = startCharIndex,
                Text = PrimaryText[startCharIndex].ToString(),
                StartMs = charStartMs,
                EndMs = charEndMs
            }, bounds);

            syllable.ChildrenRenderLyricsChars.Add(renderLyricsChar);

            PrimaryRenderChars.Add(renderLyricsChar);
        }
    }

    public void EnsureCaches(ICanvasResourceCreator resourceCreator, double strokeWidth)
    {
        if (CachedStroke != null && CachedFill != null) return;

        // 缓存纯白色的填充（作为 Fill Mask）
        CachedFill = new CanvasCommandList(resourceCreator);
        using (var ds = CachedFill.CreateDrawingSession())
        {
            if (TertiaryTextLayout != null)
                ds.DrawTextLayout(TertiaryTextLayout, TertiaryPosition, ColorExtensions.FromAppColor(Colors.White));
            if (PrimaryTextLayout != null)
                ds.DrawTextLayout(PrimaryTextLayout, PrimaryPosition, ColorExtensions.FromAppColor(Colors.White));
            if (SecondaryTextLayout != null)
                ds.DrawTextLayout(SecondaryTextLayout, SecondaryPosition,
                    ColorExtensions.FromAppColor(Colors.White));
        }

        CachedStroke = new CanvasCommandList(resourceCreator);

        // 缓存纯白色的描边（作为 Stroke Mask）
        if (strokeWidth > 0)
        {
            using var roundStrokeStyle = new CanvasStrokeStyle
            {
                LineJoin = CanvasLineJoin.Round,
                StartCap = CanvasCapStyle.Round,
                EndCap = CanvasCapStyle.Round
            };
            using var ds = CachedStroke.CreateDrawingSession();
            if (TertiaryCanvasGeometry != null)
                ds.DrawGeometry(TertiaryCanvasGeometry, TertiaryPosition,
                    ColorExtensions.FromAppColor(Colors.White), (float)strokeWidth, roundStrokeStyle);
            if (PrimaryCanvasGeometry != null)
                ds.DrawGeometry(PrimaryCanvasGeometry, PrimaryPosition, ColorExtensions.FromAppColor(Colors.White),
                    (float)strokeWidth, roundStrokeStyle);
            if (SecondaryCanvasGeometry != null)
                ds.DrawGeometry(SecondaryCanvasGeometry, SecondaryPosition,
                    ColorExtensions.FromAppColor(Colors.White), (float)strokeWidth, roundStrokeStyle);
        }

        UnplayedFillTint = new TintEffect
            { Source = CachedFill, Color = ColorExtensions.FromAppColor(Colors.White) };
        UnplayedStrokeTint = new TintEffect
            { Source = CachedStroke, Color = ColorExtensions.FromAppColor(Colors.White) };
        UnplayedComposite = new CompositeEffect
            { Sources = { UnplayedStrokeTint, UnplayedFillTint }, Mode = CanvasComposite.SourceOver };

        if (PrimaryTextRegions != null &&
            (RenderLyricsRegions == null || RenderLyricsRegions.Length != PrimaryTextRegions.Length))
        {
            DisposeRenderLyricsRegions();
            RenderLyricsRegions = new RenderLyricsRegion[PrimaryTextRegions.Length];
            for (var i = 0; i < PrimaryTextRegions.Length; i++)
                RenderLyricsRegions[i] = new RenderLyricsRegion(CachedFill, CachedStroke);
        }
    }

    private void DisposePrimaryRenderCharsEffects()
    {
        foreach (var cache in PrimaryRenderChars) cache?.DisposeEffetcts();
    }

    private void DisposeRenderLyricsRegions()
    {
        if (RenderLyricsRegions != null)
        {
            foreach (var region in RenderLyricsRegions) region?.Dispose();

            RenderLyricsRegions = null;
        }
    }

    public void DisposeCaches()
    {
        UnplayedComposite?.Dispose();
        UnplayedStrokeTint?.Dispose();
        UnplayedFillTint?.Dispose();
        CachedStroke?.Dispose();
        CachedFill?.Dispose();

        UnplayedComposite = null;
        UnplayedStrokeTint = null;
        UnplayedFillTint = null;
        CachedStroke = null;
        CachedFill = null;

        DisposeRenderLyricsRegions();
        DisposePrimaryRenderCharsEffects();
    }
}