namespace BetterLyrics.Core.Interfaces.Services;

public interface IGlobalShortcutService
{
    void RefreshAllHotKeys();
    bool IsHotKeyRegistered(List<string> keys);
}
