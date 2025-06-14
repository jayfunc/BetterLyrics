using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.WinUI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Windows.UI;

namespace BetterLyrics.WinUI3.Rendering
{
    public class DesktopBackgroundRenderer : BaseRenderer
    {
        private readonly GlobalViewModel _globalViewModel;

        private Color _startColor;
        private Color _targetColor;
        private float _progress; // From 0 to 1
        private const float TransitionSeconds = 0.3f;
        private bool _isTransitioning;

        public DesktopBackgroundRenderer(GlobalViewModel globalViewModel)
        {
            _globalViewModel = globalViewModel;

            _startColor = _targetColor =
                _globalViewModel.ActivatedWindowAccentColor.ToWindowsUIColor();
            _progress = 1f;
        }

        public void Calculate()
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
                _progress += (float)(ElapsedTime.TotalSeconds / TransitionSeconds);
                if (_progress >= 1f)
                {
                    _progress = 1f;
                    _isTransitioning = false;
                }
            }
        }

        public void Draw(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            var color = _isTransitioning
                ? ColorHelper.GetInterpolatedColor(_progress, _startColor, _targetColor)
                : _targetColor;

            ds.FillRectangle(control.Size.ToRect(), color);
        }
    }
}
