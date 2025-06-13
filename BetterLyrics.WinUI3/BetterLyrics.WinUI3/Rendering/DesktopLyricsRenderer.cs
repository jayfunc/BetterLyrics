using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.ViewModels.Lyrics;

namespace BetterLyrics.WinUI3.Rendering
{
    public class DesktopLyricsRenderer : BaseLyricsRenderer
    {
        public DesktopLyricsRenderer(DesktopLyricsViewModel viewModel)
            : base(viewModel) { }
    }
}
