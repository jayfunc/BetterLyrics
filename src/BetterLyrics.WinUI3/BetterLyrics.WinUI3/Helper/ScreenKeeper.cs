using Windows.System.Display;

namespace BetterLyrics.WinUI3.Helper
{
    public static class ScreenKeeper
    {
        private static readonly DisplayRequest _displayRequest = new DisplayRequest();
        private static bool _isActive = false;
        private static readonly object _lock = new object();

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
}
