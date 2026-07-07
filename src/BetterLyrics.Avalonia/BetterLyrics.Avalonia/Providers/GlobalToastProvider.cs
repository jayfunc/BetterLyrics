using System;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;

namespace BetterLyrics.Avalonia.Providers;

public class GlobalToastProvider : IGlobalToastProvider
{
    public void Initialize()
    {
        throw new NotImplementedException();
    }

    public void Show(string localizedTitleKey, string? message = null, MessageSeverity severity = MessageSeverity.Informational,
        TimeSpan? duration = null)
    {
        // TODO
    }
}