using System;
using BetterLyrics.Core.Enums;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Xaml;

namespace BetterLyrics.WinUI3.Extensions;

public static class LyricsAlignmentTypeExtensions
{
    public static CanvasHorizontalAlignment ToCanvasHorizontalAlignment(this TextAlignmentType alignmentType)
    {
        return alignmentType switch
        {
            TextAlignmentType.Left => CanvasHorizontalAlignment.Left,
            TextAlignmentType.Center => CanvasHorizontalAlignment.Center,
            TextAlignmentType.Right => CanvasHorizontalAlignment.Right,
            TextAlignmentType.LeftRight => CanvasHorizontalAlignment.Left,
            _ => throw new ArgumentOutOfRangeException(nameof(alignmentType), alignmentType, null)
        };
    }
}