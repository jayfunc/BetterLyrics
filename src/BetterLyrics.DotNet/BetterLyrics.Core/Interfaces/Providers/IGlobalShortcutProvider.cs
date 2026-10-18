namespace BetterLyrics.Core.Interfaces.Providers;

public interface IGlobalShortcutProvider
{
    bool RegisterHotKey(int id, List<string> keys, Action action);
    void UnregisterHotKey(int id);
    void UpdateHotKey(int id, List<string> keys, Action action);
    bool IsHotKeyRegistered(List<string> keys);
    bool IsHotKeyRegistered(int id);
}
