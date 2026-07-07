using BetterLyrics.Core.Enums;

namespace BetterLyrics.Core.Interfaces.Providers;

public interface IGlobalToastProvider
{
    void Initialize();

    void Show(string localizedTitleKey, string? message = null,
        MessageSeverity severity = MessageSeverity.Informational, TimeSpan? duration = null);
}