// 2025/6/23 by Zhe Fang

using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Xaml;
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
        public static HorizontalAlignment ToHorizontalAlignment(this TextAlignmentType alignmentType)
        {
            return alignmentType switch
            {
                TextAlignmentType.Left => HorizontalAlignment.Left,
                TextAlignmentType.Center => HorizontalAlignment.Center,
                TextAlignmentType.Right => HorizontalAlignment.Right,
                _ => throw new ArgumentOutOfRangeException(nameof(alignmentType), alignmentType, null),
            };
        }

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
