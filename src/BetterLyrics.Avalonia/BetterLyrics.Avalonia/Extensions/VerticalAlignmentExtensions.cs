using Avalonia.Layout;
using BetterLyrics.Core.Models.Domain;
using System;

namespace BetterLyrics.Avalonia.Extensions;

public static class VerticalAlignmentExtensions
{
    public static VerticalAlignment FromAppVerticalAlignment(AppVerticalAlignment value)
    {
        return value switch
        {
            AppVerticalAlignment.Top => VerticalAlignment.Top,
            AppVerticalAlignment.Center => VerticalAlignment.Center,
            AppVerticalAlignment.Bottom => VerticalAlignment.Bottom,
            AppVerticalAlignment.Stretch => VerticalAlignment.Stretch,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
    }
}
