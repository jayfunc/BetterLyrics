using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.ViewModels.Lyrics;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Windows.UI;

namespace BetterLyrics.WinUI3.Rendering
{
    public class DesktopLyricsRenderer : BaseLyricsRenderer
    {
        private readonly GlobalViewModel _globalViewModel;

        private Color _startColor;
        private Color _targetColor;
        private float _progress; // From 0 to 1
        private const float TransitionSeconds = 0.3f;
        private bool _isTransitioning;

        public DesktopLyricsRenderer(
            DesktopLyricsViewModel viewModel,
            GlobalViewModel globalViewModel
        )
            : base(viewModel)
        {
            _globalViewModel = globalViewModel;
            _startColor = _targetColor =
                _globalViewModel.ActivatedWindowAccentColor.ToWindowsUIColor();
            _progress = 1f;
        }

        public void UpdateTransition(float elapsedSeconds)
        {
            // Detect if the accent color has changed
            var currentAccent = _globalViewModel.ActivatedWindowAccentColor.ToWindowsUIColor();
            if (currentAccent != _targetColor)
            {
                _startColor = _isTransitioning
                    ? ColorHelper.GetInterpolatedColor(_progress, _startColor, _targetColor)
                    : _targetColor;
                _targetColor = currentAccent;
                _progress = 0f;
                _isTransitioning = true;
            }

            // Update the transition progress
            if (_isTransitioning)
            {
                _progress += elapsedSeconds / TransitionSeconds;
                if (_progress >= 1f)
                {
                    _progress = 1f;
                    _isTransitioning = false;
                }
            }
        }

        public void DrawBackground(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            var color = _isTransitioning
                ? ColorHelper.GetInterpolatedColor(_progress, _startColor, _targetColor)
                : _targetColor;

            ds.FillRectangle(control.Size.ToRect(), color);
        }
    }
}
