using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models.Lyrics;
using BetterLyrics.Avalonia.Extensions; // Adjusted namespace

namespace BetterLyrics.Avalonia.Models.Lyrics;

public class RenderLyricsLine : BaseRenderLyricsLine
{
    public RenderLyricsLine(LyricsLine lyricsLine) : base(lyricsLine)
    {
        PrimaryRenderSyllables = lyricsLine.PrimarySyllables.Select(x => new RenderLyricsSyllable(x)).ToList();
    }

    public new List<RenderLyricsChar> PrimaryRenderChars { get; } = [];
    public new List<RenderLyricsSyllable> PrimaryRenderSyllables { get; }

    public TextLayout? PrimaryTextLayout { get; private set; }
    public TextLayout? SecondaryTextLayout { get; private set; }
    public TextLayout? TertiaryTextLayout { get; private set; }

    public Geometry? PrimaryCanvasGeometry { get; private set; }
    public Geometry? SecondaryCanvasGeometry { get; private set; }
    public Geometry? TertiaryCanvasGeometry { get; private set; }

    // Replaces CanvasCommandList
    public DrawingGroup? CachedStroke { get; private set; }
    public DrawingGroup? CachedFill { get; private set; }

    // Note: Win2D Effects (TintEffect, CompositeEffect) are removed here.
    // In Skia/Avalonia, tinting and composition are handled at the render phase 
    // by configuring SKPaint (ColorFilter, BlendMode) when drawing these caches.

    public IReadOnlyList<Rect>? PrimaryTextRegions { get; private set; }

    public RenderLyricsRegion[]? RenderLyricsRegions { get; private set; }

    public void DisposeTextLayout()
    {
        // Avalonia TextLayout is not IDisposable, simply nullifying is sufficient
        TertiaryTextLayout = null;
        PrimaryTextLayout = null;
        SecondaryTextLayout = null;
    }

    public void RecreateTextLayout(
        bool createPhonetic, bool createTranslated,
        int phoneticTextFontSize, int originalTextFontSize, int translatedTextFontSize,
        LyricsFontWeight fontWeight,
        string fontFamilyCJK, string fontFamilyWestern,
        double maxWidth, double maxHeight,
        TextAlignmentType type, bool autoWrap, LyricsLayoutOrientation orientation)
    {
        DisposeTextLayout();

        var textWrapping = autoWrap ? TextWrapping.Wrap : TextWrapping.NoWrap;

        var horizontalAlignment = type switch
        {
            TextAlignmentType.Left => TextAlignment.Left,
            TextAlignmentType.Center => TextAlignment.Center,
            TextAlignmentType.Right => TextAlignment.Right,
            _ => TextAlignment.Left
        };

        var phoneticVisible = createPhonetic && !string.IsNullOrWhiteSpace(TertiaryText);
        var translatedVisible = createTranslated && !string.IsNullOrWhiteSpace(SecondaryText);

        // Note: Avalonia handles vertical text natively via TextLayout's orientation or LayoutMode,
        // depending on the Avalonia version. Assuming default flow for standard setup.

        if (phoneticVisible)
        {
            var typeface = new Typeface(fontFamilyCJK, FontStyle.Normal,
                FontWeightExtensions.FromLyricsFontWeight(fontWeight));
            TertiaryTextLayout = new TextLayout(
                TertiaryText,
                typeface,
                phoneticTextFontSize,
                Brushes.White,
                horizontalAlignment,
                textWrapping,
                maxWidth: maxWidth,
                maxHeight: maxHeight
            );
            // If you have an extension method to handle CJK/Western fallbacks:
            // TertiaryTextLayout.SetFontFamily(TertiaryText, fontFamilyCJK, fontFamilyWestern);
        }

        var primaryTypeface = new Typeface(fontFamilyCJK, FontStyle.Normal,
            FontWeightExtensions.FromLyricsFontWeight(fontWeight));
        PrimaryTextLayout = new TextLayout(
            PrimaryText,
            primaryTypeface,
            originalTextFontSize,
            Brushes.White,
            horizontalAlignment,
            textWrapping,
            maxWidth: maxWidth,
            maxHeight: maxHeight
        );
        PrimaryTextRegions = PrimaryTextLayout.HitTestTextRange(0, PrimaryText.Length).ToList();

        if (translatedVisible)
        {
            var secondaryTypeface = new Typeface(fontFamilyCJK, FontStyle.Normal,
                FontWeightExtensions.FromLyricsFontWeight(fontWeight));
            SecondaryTextLayout = new TextLayout(
                SecondaryText,
                secondaryTypeface,
                translatedTextFontSize,
                Brushes.White,
                horizontalAlignment,
                textWrapping,
                maxWidth: maxWidth,
                maxHeight: maxHeight
            );
        }
    }

    public void DisposeTextGeometry()
    {
        TertiaryCanvasGeometry = null;
        PrimaryCanvasGeometry = null;
        SecondaryCanvasGeometry = null;
    }

    public void RecreateTextGeometry()
    {
        DisposeTextGeometry();

        var origin = new Point(0, 0);

        // BuildGeometry extracts the path data out of the TextLayout
        if (TertiaryTextLayout != null) TertiaryCanvasGeometry = TertiaryTextLayout.BuildGeometry(origin);
        if (PrimaryTextLayout != null) PrimaryCanvasGeometry = PrimaryTextLayout.BuildGeometry(origin);
        if (SecondaryTextLayout != null) SecondaryCanvasGeometry = SecondaryTextLayout.BuildGeometry(origin);
    }

    public void RecreateRenderChars(int strokeWidth)
    {
        PrimaryRenderChars.Clear();
        if (PrimaryTextLayout == null) return;

        foreach (var syllable in PrimaryRenderSyllables) syllable.ChildrenRenderLyricsChars.Clear();

        var textLength = PrimaryText.Length;

        for (var startCharIndex = 0; startCharIndex < textLength; startCharIndex++)
        {
            // Avalonia HitTestTextRange returns a list of text runs. We take the first layout bounds.
            var hitTestResult = PrimaryTextLayout.HitTestTextRange(startCharIndex, 1).FirstOrDefault();
            if (hitTestResult == null) continue;

            var region = hitTestResult;

            // Note: Ensure your .Extend() extension method supports Avalonia.Rect
            var bounds = region.Extend(
                startCharIndex == 0 ? strokeWidth : strokeWidth / 4f,
                strokeWidth / 2f,
                startCharIndex == textLength - 1 ? strokeWidth : strokeWidth / 4f,
                strokeWidth / 2f);

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
            }, bounds.ToAppRect());

            syllable.ChildrenRenderLyricsChars.Add(renderLyricsChar);
            PrimaryRenderChars.Add(renderLyricsChar);
        }
    }

    public void EnsureCaches(double strokeWidth)
    {
        if (CachedStroke != null && CachedFill != null) return;

        // Cache the pure white fill (as Fill Mask) using Avalonia DrawingGroup
        CachedFill = new DrawingGroup();
        using (var ctx = CachedFill.Open())
        {
            if (TertiaryTextLayout != null)
                TertiaryTextLayout.Draw(ctx, new Point(TertiaryPosition.X, TertiaryPosition.Y));

            if (PrimaryTextLayout != null)
                PrimaryTextLayout.Draw(ctx, new Point(PrimaryPosition.X, PrimaryPosition.Y));

            if (SecondaryTextLayout != null)
                SecondaryTextLayout.Draw(ctx, new Point(SecondaryPosition.X, SecondaryPosition.Y));
        }

        // Cache the pure white stroke (as Stroke Mask)
        CachedStroke = new DrawingGroup();
        if (strokeWidth > 0)
        {
            using var ctx = CachedStroke.Open();
            var pen = new Pen(Brushes.White, strokeWidth, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);

            if (TertiaryCanvasGeometry != null)
            {
                // To offset geometry in Avalonia DrawingContext, we push a transform
                using (ctx.PushTransform(Matrix.CreateTranslation(TertiaryPosition.X, TertiaryPosition.Y)))
                {
                    ctx.DrawGeometry(null, pen, TertiaryCanvasGeometry);
                }
            }

            if (PrimaryCanvasGeometry != null)
            {
                using (ctx.PushTransform(Matrix.CreateTranslation(PrimaryPosition.X, PrimaryPosition.Y)))
                {
                    ctx.DrawGeometry(null, pen, PrimaryCanvasGeometry);
                }
            }

            if (SecondaryCanvasGeometry != null)
            {
                using (ctx.PushTransform(Matrix.CreateTranslation(SecondaryPosition.X, SecondaryPosition.Y)))
                {
                    ctx.DrawGeometry(null, pen, SecondaryCanvasGeometry);
                }
            }
        }

        if (PrimaryTextRegions != null &&
            (RenderLyricsRegions == null || RenderLyricsRegions.Length != PrimaryTextRegions.Count))
        {
            DisposeRenderLyricsRegions();
            RenderLyricsRegions = new RenderLyricsRegion[PrimaryTextRegions.Count];
            for (var i = 0; i < PrimaryTextRegions.Count; i++)
                RenderLyricsRegions[i] = new RenderLyricsRegion(CachedFill, CachedStroke);
        }
    }

    private void DisposePrimaryRenderCharsEffects()
    {
        foreach (var cache in PrimaryRenderChars)
            cache?.DisposeEffetcts(); // Ensure typo matches your Base class if it exists
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
        CachedStroke = null;
        CachedFill = null;

        DisposeRenderLyricsRegions();
        DisposePrimaryRenderCharsEffects();
    }
}