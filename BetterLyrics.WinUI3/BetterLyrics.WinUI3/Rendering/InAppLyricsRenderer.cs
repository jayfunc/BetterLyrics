using BetterInAppLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.ViewModels;

namespace BetterLyrics.WinUI3.Rendering
{
    class InAppLyricsRenderer : BaseLyricsRenderer
    {
        public InAppLyricsRenderer(InAppLyricsViewModel viewModel, GlobalViewModel globalViewModel)
            : base(viewModel, globalViewModel) { }
    }
}
