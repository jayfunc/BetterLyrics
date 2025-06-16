using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.Playback;
using BetterLyrics.WinUI3.Services.Settings;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class DesktopLyricsRendererViewModel
        : BaseLyricsRendererViewModel,
            IRecipient<PropertyChangedMessage<int>>,
            IRecipient<PropertyChangedMessage<float>>,
            IRecipient<PropertyChangedMessage<double>>,
            IRecipient<PropertyChangedMessage<bool>>,
            IRecipient<PropertyChangedMessage<Color>>,
            IRecipient<PropertyChangedMessage<LyricsFontColorType>>,
            IRecipient<PropertyChangedMessage<LyricsAlignmentType>>
    {
        private Color _startColor;
        private Color _targetColor;
        private float _progress; // From 0 to 1
        private const float TransitionSeconds = 0.3f;
        private bool _isTransitioning;

        public Color ActivatedWindowAccentColor { get; set; }

        public DesktopLyricsRendererViewModel(
            ISettingsService settingsService,
            IPlaybackService playbackService
        )
            : base(settingsService, playbackService)
        {
            LyricsFontColorType = _settingsService.DesktopLyricsFontColorType;
            LyricsFontSelectedAccentColorIndex =
                _settingsService.DesktopLyricsFontSelectedAccentColorIndex;
            LyricsAlignmentType = _settingsService.DesktopLyricsAlignmentType;
            LyricsVerticalEdgeOpacity = _settingsService.DesktopLyricsVerticalEdgeOpacity;
            LyricsLineSpacingFactor = _settingsService.DesktopLyricsLineSpacingFactor;
            LyricsFontSize = _settingsService.DesktopLyricsFontSize;
            LyricsBlurAmount = _settingsService.DesktopLyricsBlurAmount;
            IsLyricsGlowEffectEnabled = _settingsService.IsDesktopLyricsGlowEffectEnabled;
            IsLyricsDynamicGlowEffectEnabled =
                _settingsService.IsDesktopLyricsDynamicGlowEffectEnabled;

            _startColor = _targetColor = ActivatedWindowAccentColor;
            _progress = 1f;
        }

        public override void Calculate(
            ICanvasAnimatedControl control,
            CanvasAnimatedUpdateEventArgs args
        )
        {
            base.Calculate(control, args);

            // Detect if the accent color has changed
            if (ActivatedWindowAccentColor != _targetColor)
            {
                _startColor = _isTransitioning
                    ? ColorHelper.GetInterpolatedColor(_progress, _startColor, _targetColor)
                    : _targetColor;
                _targetColor = ActivatedWindowAccentColor;
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

        public override void Draw(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            var color = _isTransitioning
                ? ColorHelper.GetInterpolatedColor(_progress, _startColor, _targetColor)
                : _targetColor;

            ds.FillRectangle(control.Size.ToRect(), color);

            base.Draw(control, ds);
        }

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender is DesktopLyricsSettingsControlViewModel)
            {
                if (
                    message.PropertyName
                    == nameof(DesktopLyricsSettingsControlViewModel.IsLyricsGlowEffectEnabled)
                )
                {
                    IsLyricsGlowEffectEnabled = message.NewValue;
                }
                else if (
                    message.PropertyName
                    == nameof(
                        DesktopLyricsSettingsControlViewModel.IsLyricsDynamicGlowEffectEnabled
                    )
                )
                {
                    IsLyricsDynamicGlowEffectEnabled = message.NewValue;
                }
            }
        }

        public void Receive(PropertyChangedMessage<int> message)
        {
            if (message.Sender is DesktopLyricsSettingsControlViewModel)
            {
                if (
                    message.PropertyName
                    == nameof(DesktopLyricsSettingsControlViewModel.LyricsVerticalEdgeOpacity)
                )
                {
                    LyricsVerticalEdgeOpacity = message.NewValue;
                }
                else if (
                    message.PropertyName
                    == nameof(DesktopLyricsSettingsControlViewModel.LyricsBlurAmount)
                )
                {
                    LyricsBlurAmount = message.NewValue;
                }
                else if (
                    message.PropertyName
                    == nameof(
                        DesktopLyricsSettingsControlViewModel.LyricsFontSelectedAccentColorIndex
                    )
                )
                {
                    LyricsFontSelectedAccentColorIndex = message.NewValue;
                }
            }
        }

        public void Receive(PropertyChangedMessage<Color> message)
        {
            if (message.Sender is OverlayWindowViewModel)
            {
                if (
                    message.PropertyName
                    == nameof(OverlayWindowViewModel.ActivatedWindowAccentColor)
                )
                    ActivatedWindowAccentColor = message.NewValue;
            }
        }

        public void Receive(PropertyChangedMessage<LyricsFontColorType> message)
        {
            if (message.Sender is DesktopLyricsSettingsControlViewModel)
            {
                if (
                    message.PropertyName
                    == nameof(DesktopLyricsSettingsControlViewModel.LyricsFontColorType)
                )
                {
                    LyricsFontColorType = message.NewValue;
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsAlignmentType> message)
        {
            if (message.Sender is DesktopLyricsSettingsControlViewModel)
            {
                if (
                    message.PropertyName
                    == nameof(DesktopLyricsSettingsControlViewModel.LyricsAlignmentType)
                )
                {
                    LyricsAlignmentType = message.NewValue;
                }
            }
        }

        public void Receive(PropertyChangedMessage<float> message)
        {
            if (message.Sender is DesktopLyricsSettingsControlViewModel)
            {
                if (
                    message.PropertyName
                    == nameof(DesktopLyricsSettingsControlViewModel.LyricsLineSpacingFactor)
                )
                {
                    LyricsLineSpacingFactor = message.NewValue;
                }
                else if (
                    message.PropertyName
                    == nameof(DesktopLyricsSettingsControlViewModel.LyricsFontSize)
                )
                {
                    LyricsFontSize = message.NewValue;
                }
            }
        }

        public void Receive(PropertyChangedMessage<double> message)
        {
            if (message.Sender is DesktopLyricsPageViewModel)
            {
                if (message.PropertyName == nameof(DesktopLyricsPageViewModel.LimitedLineWidth))
                {
                    LimitedLineWidth = message.NewValue;
                }
            }
        }
    }
}
