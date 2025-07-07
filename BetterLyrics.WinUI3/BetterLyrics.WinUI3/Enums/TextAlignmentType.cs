// 2025/6/23 by Zhe Fang

using Microsoft.Graphics.Canvas.Text;
using System;

namespace BetterLyrics.WinUI3.Enums
{
    public enum TextAlignmentType
    {
        Left,
        Center,
        Right,
    }

    public static class LyricsAlignmentTypeExtensions
    {
        public static CanvasHorizontalAlignment ToCanvasHorizontalAlignment(this TextAlignmentType alignmentType)
        {
            return alignmentType switch
            {
                TextAlignmentType.Left => CanvasHorizontalAlignment.Left,
                TextAlignmentType.Center => CanvasHorizontalAlignment.Center,
                TextAlignmentType.Right => CanvasHorizontalAlignment.Right,
                _ => throw new ArgumentOutOfRangeException(nameof(alignmentType), alignmentType, null),
            };
        }
    }
}
