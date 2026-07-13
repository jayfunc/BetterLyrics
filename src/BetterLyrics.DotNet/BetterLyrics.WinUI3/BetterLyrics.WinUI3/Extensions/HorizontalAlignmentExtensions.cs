using System;
using BetterLyrics.Core.Models.Domain;
using Microsoft.UI.Xaml;

namespace BetterLyrics.WinUI3.Extensions;

public static class HorizontalAlignmentExtensions
{
    public static HorizontalAlignment FromAppHorizontalAlignment(AppHorizontalAlignment value)
    {
        return value switch
        {
            AppHorizontalAlignment.Left => HorizontalAlignment.Left,
            AppHorizontalAlignment.Center => HorizontalAlignment.Center,
            AppHorizontalAlignment.Right => HorizontalAlignment.Right,
            AppHorizontalAlignment.Stretch => HorizontalAlignment.Stretch,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
    }
}