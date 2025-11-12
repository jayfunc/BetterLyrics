using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels.LyricsRendererViewModel
{
    public partial class LyricsRendererViewModel
    {
        private readonly ValueTransition<double> _canvasYScrollTransition = new(
            initialValue: 0f,
            durationSeconds: 0.3f,
            easingType: EasingType.EaseInOutQuad
        );

        private readonly ValueTransition<Color> _immersiveBgColorTransition = new(
            initialValue: Colors.Transparent,
            durationSeconds: 0.3f,
            interpolator: (from, to, progress) => Helper.ColorHelper.GetInterpolatedColor(progress, from, to)
        );

        private readonly ValueTransition<Color> _albumArtAccentColor1Transition = new(
            initialValue: Colors.Transparent,
            durationSeconds: 0.3f,
            interpolator: (from, to, progress) => Helper.ColorHelper.GetInterpolatedColor(progress, from, to)
        );

        private readonly ValueTransition<Color> _albumArtAccentColor2Transition = new(
            initialValue: Colors.Transparent,
            durationSeconds: 0.3f,
            interpolator: (from, to, progress) => Helper.ColorHelper.GetInterpolatedColor(progress, from, to)
        );

        private readonly ValueTransition<Color> _albumArtAccentColor3Transition = new(
            initialValue: Colors.Transparent,
            durationSeconds: 0.3f,
            interpolator: (from, to, progress) => Helper.ColorHelper.GetInterpolatedColor(progress, from, to)
        );

        private readonly ValueTransition<Color> _albumArtAccentColor4Transition = new(
            initialValue: Colors.Transparent,
            durationSeconds: 0.3f,
            interpolator: (from, to, progress) => Helper.ColorHelper.GetInterpolatedColor(progress, from, to)
        );

        private readonly ValueTransition<double> _immersiveBgOpacityTransition = new(
            initialValue: 1f,
            durationSeconds: 0.3f
        );

        private readonly ValueTransition<double> _titleXTransition = new(
            initialValue: 0f,
            durationSeconds: 0.3f,
            easingType: EasingType.EaseInOutQuad
        );

        private readonly ValueTransition<double> _titleYTransition = new(
            initialValue: 0f,
            durationSeconds: 0.3f,
            easingType: EasingType.EaseInOutQuad
        );

        private readonly ValueTransition<double> _lyricsYTransition = new(
            initialValue: 0f,
            durationSeconds: 0.3f,
            easingType: EasingType.EaseInOutQuad
        );

        private readonly ValueTransition<double> _lyricsOpacityTransition = new(
            initialValue: 0f,
            durationSeconds: 0.3f
        );

        private readonly ValueTransition<double> _albumArtBgTransition = new(
            initialValue: 0f,
            durationSeconds: 1f
        );

        private readonly ValueTransition<double> _albumArtOpacityTransition = new(
            initialValue: 0f,
            durationSeconds: 1f
        );

        private readonly ValueTransition<double> _albumArtXTransition = new(
            initialValue: 0f,
            durationSeconds: 0.3f,
            easingType: EasingType.EaseInOutQuad
        );

        private readonly ValueTransition<double> _albumArtYTransition = new(
            initialValue: 0f,
            durationSeconds: 0.3f,
            easingType: EasingType.EaseInOutQuad
        );

        private readonly ValueTransition<double> _songInfoOpacityTransition = new(
            initialValue: 0f,
            durationSeconds: 1f
        );

        private readonly ValueTransition<double> _lyricsBgBrightnessTransition = new(
            initialValue: 0f,
            durationSeconds: 1f
        );

    }
}
