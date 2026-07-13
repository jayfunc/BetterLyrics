using System;
using BetterLyrics.Core.Models.Domain;
using Microsoft.UI.Xaml;

namespace BetterLyrics.WinUI3.Extensions;

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