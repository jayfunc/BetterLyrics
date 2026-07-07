using System;
using BetterLyrics.Core.Enums;
using Microsoft.UI.Xaml.Controls;

namespace BetterLyrics.WinUI3.Extensions;

public static class InfoBarSeverityExtensions
{
    public static InfoBarSeverity FromMessageSeverity(MessageSeverity messageSeverity)
    {
        return messageSeverity switch
        {
            MessageSeverity.Informational => InfoBarSeverity.Informational,
            MessageSeverity.Success => InfoBarSeverity.Success,
            MessageSeverity.Warning => InfoBarSeverity.Warning,
            MessageSeverity.Error => InfoBarSeverity.Error,
            _ => throw new ArgumentOutOfRangeException(nameof(messageSeverity), messageSeverity, null)
        };
    }
}