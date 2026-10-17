using BetterLyrics.Core.Enums;

namespace BetterLyrics.Core.Messages.AppSettings;

public sealed class GlobalShortcutChangedMessage(ShortcutId shortcutId, List<string> shortcut)
{
    public ShortcutId ShortcutId { get; } = shortcutId;
    public List<string> Shortcut { get; } = shortcut;
}

