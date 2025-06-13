using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterInAppLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.ViewModels.Lyrics;

namespace BetterLyrics.WinUI3.Rendering
{
    class InAppLyricsRenderer : BaseLyricsRenderer
    {
        public InAppLyricsRenderer(InAppLyricsViewModel viewModel)
            : base(viewModel) { }
    }
}
