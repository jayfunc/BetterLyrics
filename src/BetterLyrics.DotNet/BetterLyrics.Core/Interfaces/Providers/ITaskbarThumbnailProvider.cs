namespace BetterLyrics.Core.Interfaces.Providers;

public interface ITaskbarThumbnailProvider : IDisposable
{
    void InitializeButtons(IntPtr hwnd);
    void UpdatePlayPauseState(IntPtr hwnd, bool isPlaying);
}
