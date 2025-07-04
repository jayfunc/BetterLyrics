using BetterLyrics.WinUI3.Helper;
using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsRendererViewModel
    {
        private readonly ValueTransition<float> _canvasYScrollTransition = new(
            initialValue: 0f,
            durationSeconds: 0.3f
        );

        private readonly ValueTransition<Color> _immersiveBgTransition = new(
            initialValue: Colors.Transparent,
            durationSeconds: 1f,
            interpolator: (from, to, progress) => Helper.ColorHelper.GetInterpolatedColor(progress, from, to)
        );

        private readonly ValueTransition<float> _lyricsXTransition = new(
            initialValue: 0f,
            durationSeconds: 0.3f
        );

        private readonly ValueTransition<float> _lyricsOpacityTransition = new(
            initialValue: 0f,
            durationSeconds: 1f
        );

        private readonly ValueTransition<float> _albumArtBgTransition = new(
            initialValue: 0f,
            durationSeconds: 1f
        );

        private readonly ValueTransition<float> _albumArtOpacityTransition = new(
            initialValue: 0f,
            durationSeconds: 1f
        );

        private readonly ValueTransition<float> _albumArtXTransition = new(
            initialValue: 0f,
            durationSeconds: 0.3f
        );

        private readonly ValueTransition<float> _songInfoOpacityTransition = new(
            initialValue: 0f,
            durationSeconds: 1f
        );

    }
}
