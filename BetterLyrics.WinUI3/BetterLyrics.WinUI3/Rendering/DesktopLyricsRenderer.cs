using BetterLyrics.WinUI3.ViewModels;

namespace BetterLyrics.WinUI3.Rendering
{
    public class DesktopLyricsRenderer : BaseLyricsRenderer
    {
        public DesktopLyricsRenderer(
            DesktopLyricsViewModel viewModel,
            GlobalViewModel globalViewModel
        )
            : base(viewModel, globalViewModel) { }
    }
}
