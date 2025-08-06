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
        private readonly ValueTransition<float> _canvasYScrollTransition = new(
            initialValue: 0f,
            durationSeconds: 0.3f,
            easingType: EasingType.EaseInOutSine
        );

        private readonly ValueTransition<Color> _immersiveBgColorTransition = new(
            initialValue: Colors.Transparent,
            durationSeconds: 0.3f,
            interpolator: (from, to, progress) => Helper.ColorHelper.GetInterpolatedColor(progress, from, to)
        );

        private readonly ValueTransition<Color> _albumArtAccentColorTransition = new(
            initialValue: Colors.Transparent,
            durationSeconds: 0.3f,
            interpolator: (from, to, progress) => Helper.ColorHelper.GetInterpolatedColor(progress, from, to)
        );

        private readonly ValueTransition<float> _immersiveBgOpacityTransition = new(
            initialValue: 1f,
            durationSeconds: 0.3f
        );

        private readonly ValueTransition<float> _titleXTransition = new(
            initialValue: 0f,
            durationSeconds: 0.3f,
            easingType: EasingType.EaseInOutBack
        );

        private readonly ValueTransition<float> _titleYTransition = new(
            initialValue: 0f,
            durationSeconds: 0.3f,
            easingType: EasingType.EaseInOutBack
        );

        private readonly ValueTransition<float> _lyricsXTransition = new(
            initialValue: 0f,
            durationSeconds: 0.3f,
            easingType: EasingType.EaseInOutBack
        );

        private readonly ValueTransition<float> _lyricsYTransition = new(
            initialValue: 0f,
            durationSeconds: 0.3f,
            easingType: EasingType.EaseInOutBack
        );

        private readonly ValueTransition<float> _lyricsOpacityTransition = new(
            initialValue: 0f,
            durationSeconds: 0.3f
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
            durationSeconds: 0.3f,
            easingType: EasingType.EaseInOutBack
        );

        private readonly ValueTransition<float> _albumArtYTransition = new(
            initialValue: 0f,
            durationSeconds: 0.3f,
            easingType: EasingType.EaseInOutBack
        );

        private readonly ValueTransition<float> _songInfoOpacityTransition = new(
            initialValue: 0f,
            durationSeconds: 1f
        );

        private readonly ValueTransition<float> _lyricsBgBrightnessTransition = new(
            initialValue: 0f,
            durationSeconds: 1f
        );

    }
}
