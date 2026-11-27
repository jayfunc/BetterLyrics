using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Extensions;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;

namespace BetterLyrics.WinUI3.Renderer;

public enum SongInfoKind
{
    Title,
    Artist,
    Album
}

public enum SongInfoAlignment
{
    Left,
    Center,
    Right
}

/// <summary>
/// 歌曲信息三行（标题 / 艺术家 / 专辑）的 Win2D 渲染。
/// 与歌词共用 CanvasAnimatedControl 的帧率，偏移不吸附像素，保证与歌词一致的高帧率滚动。
/// </summary>
public sealed class SongInfoRenderer : IDisposable
{
    // 与原 AutoScrollView 的默认 Spacing 保持一致
    private const float ItemSpacing = 20f;

    // 左右渐隐宽度占区域宽度的比例，对应原先 OpacityMaskView 的 0.02 / 0.98 渐变节点
    private const float FadeWidthRatio = 0.02f;

    private const double OpacityFadeSeconds = 0.35;

    private const float MaxLayoutExtent = 100000f;

    private readonly object _gate = new();

    private readonly Input _input = new();

    private readonly RenderLine _titleLine = new();
    private readonly RenderLine _artistLine = new();
    private readonly RenderLine _albumLine = new();

    private double _alpha = 1;

    public void SetContent(string? title, string? artist, string? album,
        double titleFontSize, double artistFontSize, double albumFontSize)
    {
        lock (_gate)
        {
            _input.Title = title ?? string.Empty;
            _input.Artist = artist ?? string.Empty;
            _input.Album = album ?? string.Empty;
            _input.TitleFontSize = titleFontSize;
            _input.ArtistFontSize = artistFontSize;
            _input.AlbumFontSize = albumFontSize;
        }
    }

    public void SetLayout(Rect title, Rect artist, Rect album)
    {
        lock (_gate)
        {
            _input.TitleBounds = title;
            _input.ArtistBounds = artist;
            _input.AlbumBounds = album;
        }
    }

    public void SetHover(SongInfoKind kind, bool hovered)
    {
        lock (_gate)
        {
            switch (kind)
            {
                case SongInfoKind.Title:
                    _input.TitleHovered = hovered;
                    break;
                case SongInfoKind.Artist:
                    _input.ArtistHovered = hovered;
                    break;
                case SongInfoKind.Album:
                    _input.AlbumHovered = hovered;
                    break;
            }
        }
    }

    public void SetAlignment(SongInfoKind kind, SongInfoAlignment alignment)
    {
        lock (_gate)
        {
            switch (kind)
            {
                case SongInfoKind.Title:
                    _input.TitleAlignment = alignment;
                    break;
                case SongInfoKind.Artist:
                    _input.ArtistAlignment = alignment;
                    break;
                case SongInfoKind.Album:
                    _input.AlbumAlignment = alignment;
                    break;
            }
        }
    }

    public void SetOpacityTarget(double target)
    {
        lock (_gate)
        {
            _input.OpacityTarget = Math.Clamp(target, 0, 1);
        }
    }

    public void Update(ICanvasAnimatedControl sender, double elapsedSeconds, LyricsWindowStatus? status)
    {
        if (status == null) return;

        string title, artist, album;
        double titleFontSize, artistFontSize, albumFontSize;
        Rect titleBounds, artistBounds, albumBounds;
        bool titleHovered, artistHovered, albumHovered;
        SongInfoAlignment titleAlignment, artistAlignment, albumAlignment;
        double opacityTarget;

        lock (_gate)
        {
            title = _input.Title;
            artist = _input.Artist;
            album = _input.Album;
            titleFontSize = _input.TitleFontSize;
            artistFontSize = _input.ArtistFontSize;
            albumFontSize = _input.AlbumFontSize;
            titleBounds = _input.TitleBounds;
            artistBounds = _input.ArtistBounds;
            albumBounds = _input.AlbumBounds;
            titleHovered = _input.TitleHovered;
            artistHovered = _input.ArtistHovered;
            albumHovered = _input.AlbumHovered;
            titleAlignment = _input.TitleAlignment;
            artistAlignment = _input.ArtistAlignment;
            albumAlignment = _input.AlbumAlignment;
            opacityTarget = _input.OpacityTarget;
        }

        // 暂停或掉帧后不要让偏移量跳变
        var elapsed = Math.Clamp(elapsedSeconds, 0, 0.1);

        UpdateAlpha(elapsed, opacityTarget);

        var style = status.LyricsStyleSettings;
        var autoScroll = status.AlbumArtAreaEffectSettings.SongInfoAutoScroll;

        UpdateLine(sender, _titleLine, title, titleFontSize, titleBounds, titleAlignment,
            autoScroll || titleHovered, style, elapsed);
        UpdateLine(sender, _artistLine, artist, artistFontSize, artistBounds, artistAlignment,
            autoScroll || artistHovered, style, elapsed);
        UpdateLine(sender, _albumLine, album, albumFontSize, albumBounds, albumAlignment,
            autoScroll || albumHovered, style, elapsed);
    }

    public void Draw(CanvasDrawingSession ds, LyricsWindowStatus? status)
    {
        if (status == null || _alpha <= 0.001) return;

        var color = ColorExtensions.FromAppColor(status.WindowPalette.NonCurrentLineFillColor);

        DrawLine(ds, _titleLine, color, 1f);
        DrawLine(ds, _artistLine, color, 0.6f);
        DrawLine(ds, _albumLine, color, 0.3f);
    }

    /// <summary>
    /// 丢弃与 CanvasDevice 绑定的资源（设备丢失或控件重新加载时调用）。
    /// </summary>
    public void ResetDeviceResources()
    {
        lock (_gate)
        {
            _titleLine.Dispose();
            _artistLine.Dispose();
            _albumLine.Dispose();
            _alpha = 1;
        }
    }

    public void Dispose() => ResetDeviceResources();

    private void UpdateAlpha(double elapsed, double target)
    {
        if (Math.Abs(_alpha - target) <= 0.001)
        {
            _alpha = target;
            return;
        }

        var step = OpacityFadeSeconds <= 0 ? 1 : elapsed / OpacityFadeSeconds;
        _alpha = _alpha < target
            ? Math.Min(target, _alpha + step)
            : Math.Max(target, _alpha - step);
    }

    private void UpdateLine(ICanvasAnimatedControl sender, RenderLine line, string text, double fontSize,
        Rect bounds, SongInfoAlignment alignment, bool playing, LyricsStyleSettings style, double elapsed)
    {
        if (line.Layout == null ||
            line.BuiltText != text ||
            Math.Abs(line.BuiltFontSize - fontSize) > 0.01 ||
            line.BuiltCjk != style.LyricsCJKFontFamily ||
            line.BuiltLatin != style.LyricsWesternFontFamily ||
            line.BuiltWeight != style.LyricsFontWeight)
        {
            RebuildLayout(sender, line, text, fontSize, style);
        }

        line.Bounds = bounds;
        line.Alignment = alignment;
        line.Alpha = (float)_alpha;

        if (!playing || line.Layout == null || bounds.Width <= 0 || line.TextWidth <= bounds.Width + 0.5)
        {
            line.Offset = 0;
            return;
        }

        var loop = line.TextWidth + ItemSpacing;
        line.Offset += elapsed * Math.Max(1.0, fontSize);
        line.Offset %= loop;
    }

    private static void RebuildLayout(ICanvasAnimatedControl control, RenderLine line, string text,
        double fontSize, LyricsStyleSettings style)
    {
        line.Layout?.Dispose();
        line.Layout = null;
        line.TextWidth = 0;

        line.BuiltText = text;
        line.BuiltFontSize = fontSize;
        line.BuiltCjk = style.LyricsCJKFontFamily;
        line.BuiltLatin = style.LyricsWesternFontFamily;
        line.BuiltWeight = style.LyricsFontWeight;
        line.Offset = 0;

        if (string.IsNullOrEmpty(text) || !double.IsFinite(fontSize) || fontSize <= 0) return;

        using var format = new CanvasTextFormat
        {
            FontSize = (float)fontSize,
            FontWeight = style.LyricsFontWeight.ToFontWeight(),
            WordWrapping = CanvasWordWrapping.NoWrap,
            TrimmingGranularity = CanvasTextTrimmingGranularity.None,
            VerticalAlignment = CanvasVerticalAlignment.Top,
            HorizontalAlignment = CanvasHorizontalAlignment.Left
        };

        line.Layout = new CanvasTextLayout(control, text, format, MaxLayoutExtent, MaxLayoutExtent)
        {
            Options = CanvasDrawTextOptions.NoPixelSnap
        };
        line.Layout.SetFontFamily(text, style.LyricsCJKFontFamily, style.LyricsWesternFontFamily);
        line.TextWidth = (float)line.Layout.LayoutBounds.Width;
    }

    private static void DrawLine(CanvasDrawingSession ds, RenderLine line, Color color, float opacityScale)
    {
        var layout = line.Layout;
        if (layout == null) return;

        var bounds = line.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        var alpha = (byte)Math.Clamp((int)Math.Round(color.A * opacityScale * line.Alpha), 0, 255);
        if (alpha == 0) return;

        var textColor = color.WithAlpha(alpha);

        var y = (float)bounds.Y;
        var overflows = line.TextWidth > bounds.Width + 0.5;

        // 文本未溢出时按布局中的水平对齐绘制；溢出滚动时从视口左边缘开始
        var alignOffset = overflows
            ? 0.0
            : line.Alignment switch
            {
                SongInfoAlignment.Center => (bounds.Width - line.TextWidth) / 2,
                SongInfoAlignment.Right => bounds.Width - line.TextWidth,
                _ => 0.0
            };
        var x = bounds.X + alignOffset - line.Offset;

        // 文本未溢出时不做羽化，避免裁到首尾字符
        if (!overflows)
        {
            ds.DrawTextLayout(layout, new Vector2((float)x, y), textColor);
            return;
        }

        var fadeWidth = Math.Clamp((float)bounds.Width * FadeWidthRatio, 0f, (float)bounds.Width / 2f);

        // 未滚动时文本从视口左边缘开始，左边缘不应渐隐；滚动后左边缘才是被截断的文本
        var fadeLeft = line.Offset > 0.5 ? fadeWidth : 0f;

        line.Mask.Update(ds, bounds, fadeLeft, 0f, fadeWidth, 0f);
        if (line.Mask.Brush == null) return;

        using (ds.CreateLayer(line.Mask.Brush, bounds))
        {
            ds.DrawTextLayout(layout, new Vector2((float)x, y), textColor);
            ds.DrawTextLayout(layout, new Vector2((float)(x + line.TextWidth + ItemSpacing), y), textColor);
        }
    }

    private sealed class Input
    {
        public string Title = string.Empty;
        public string Artist = string.Empty;
        public string Album = string.Empty;
        public double TitleFontSize;
        public double ArtistFontSize;
        public double AlbumFontSize;
        public Rect TitleBounds;
        public Rect ArtistBounds;
        public Rect AlbumBounds;
        public bool TitleHovered;
        public bool ArtistHovered;
        public bool AlbumHovered;
        public SongInfoAlignment TitleAlignment;
        public SongInfoAlignment ArtistAlignment;
        public SongInfoAlignment AlbumAlignment;
        public double OpacityTarget = 1;
    }

    private sealed class RenderLine : IDisposable
    {
        public CanvasTextLayout? Layout;
        public float TextWidth;
        public double Offset;
        public Rect Bounds;
        public double Alpha = 1;
        public SongInfoAlignment Alignment;
        public string? BuiltText;
        public double BuiltFontSize = -1;
        public string? BuiltCjk;
        public string? BuiltLatin;
        public LyricsFontWeight BuiltWeight;
        public readonly EdgeFadeMaskRenderer Mask = new();

        public void Dispose()
        {
            Layout?.Dispose();
            Layout = null;
            Mask.Dispose();

            BuiltText = null;
            BuiltFontSize = -1;
            BuiltCjk = null;
            BuiltLatin = null;
            TextWidth = 0;
            Offset = 0;
        }
    }
}