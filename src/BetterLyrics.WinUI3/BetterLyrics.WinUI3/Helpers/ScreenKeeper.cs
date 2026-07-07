using Windows.System.Display;

namespace BetterLyrics.WinUI3.Helpers;

public static class ScreenKeeper
{
    private static readonly DisplayRequest _displayRequest = new();
    private static bool _isActive;
    private static readonly object _lock = new();

    public static void SetState(bool keepOn)
    {
        lock (_lock)
        {
            if (keepOn && !_isActive)
            {
                _displayRequest.RequestActive();
                _isActive = true;
            }
            else if (!keepOn && _isActive)
            {
                _displayRequest.RequestRelease();
                _isActive = false;
            }
        }
    }
}