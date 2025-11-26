using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using Windows.Graphics.Imaging;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels.LyricsRendererViewModel
{
    public partial class LyricsRendererViewModel
        : IRecipient<PropertyChangedMessage<int>>,
            IRecipient<PropertyChangedMessage<string>>,
            IRecipient<PropertyChangedMessage<double>>,
            IRecipient<PropertyChangedMessage<bool>>,
            IRecipient<PropertyChangedMessage<Color>>,
            IRecipient<PropertyChangedMessage<LyricsDisplayType>>,
            IRecipient<PropertyChangedMessage<LyricsLayoutOrientation>>,
            IRecipient<PropertyChangedMessage<LyricsFontColorType>>,
            IRecipient<PropertyChangedMessage<TextAlignmentType>>,
            IRecipient<PropertyChangedMessage<LyricsFontWeight>>,
            IRecipient<PropertyChangedMessage<ElementTheme>>,
            IRecipient<PropertyChangedMessage<EasingType>>,
            IRecipient<PropertyChangedMessage<TimeSpan>>,
            IRecipient<PropertyChangedMessage<AlbumArtLayoutSettings>>,
            IRecipient<PropertyChangedMessage<LyricsBackgroundSettings>>,
            IRecipient<PropertyChangedMessage<LyricsWindowStatus>>,
            IRecipient<PropertyChangedMessage<SongInfo?>>,
            IRecipient<PropertyChangedMessage<List<Color>>>
    {
        private void OnSongInfoChanged()
        {
            _songDurationMs = (int)(_mediaSessionsService.CurrentSongInfo?.DurationMs ?? TimeSpan.FromMinutes(99).TotalMilliseconds);

            _songInfoOpacityTransition.Reset(0f);
            _songInfoOpacityTransition.StartTransition(1f);

            TotalTime = TimeSpan.Zero;

            // 处理 Last.fm 追踪
            _totalPlayingTime = TimeSpan.Zero;
            _isLastFMTracked = false;
        }

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender is AboutControlViewModel)
            {
                if (message.PropertyName == nameof(AboutControlViewModel.IsDebugOverlayEnabled))
                {
                    _isDebugOverlayEnabled = message.NewValue;
                    _isDebugOverlayEnabledChanged = true;
                }
            }
            else if (message.Sender is LyricsEffectSettings)
            {
                if (message.PropertyName == nameof(LyricsEffectSettings.IsLyricsGlowEffectEnabled))
                {
                }
                else if (message.PropertyName == nameof(LyricsEffectSettings.IsFanLyricsEnabled))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsEffectSettings.IsLyricsFloatAnimationEnabled))
                {
                }
                else if (message.PropertyName == nameof(LyricsEffectSettings.Is3DLyricsEnabled))
                {
                    _isLyrics3DMatrixChanged = true;
                }
            }
            else if (message.Sender is LyricsStyleSettings)
            {
                if (message.PropertyName == nameof(LyricsStyleSettings.IsDynamicLyricsFontSize))
                {
                    _isLayoutChanged = true;
                }
            }
            else if (message.Sender is AlbumArtLayoutSettings)
            {
                if (message.PropertyName == nameof(AlbumArtLayoutSettings.AutoAlbumArtSize))
                {
                    _isAlbumArtSizeChanged = true;
                }
            }
            else if (message.Sender is LyricsBackgroundSettings)
            {
                if (message.PropertyName == nameof(LyricsBackgroundSettings.IsSpectrumOverlayEnabled))
                {
                    _isSpectrumOverlayEnabledChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsBackgroundSettings.IsFluidOverlayEnabled))
                {
                    _isFluidOverlayEnabledChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsBackgroundSettings.IsSnowFlakeOverlayEnabled))
                {
                    _isSnowOverlayEnabledChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsBackgroundSettings.IsFogOverlayEnabled))
                {
                    _isFogOverlayEnabledChanged = true;
                }
            }
        }

        public void Receive(PropertyChangedMessage<Color> message)
        {
            if (message.Sender is LyricsWindowViewModel)
            {
                if (message.PropertyName == nameof(LyricsWindowViewModel.BackdropAccentColor))
                {
                    _immersiveBgColorTransition.StartTransition(message.NewValue);
                    _environmentalColor = message.NewValue;
                    UpdateColorConfig();
                }
            }
            else if (message.Sender is LyricsStyleSettings)
            {
                if (message.PropertyName == nameof(LyricsStyleSettings.LyricsCustomBgFontColor))
                {
                    UpdateColorConfig();
                }
                else if (message.PropertyName == nameof(LyricsStyleSettings.LyricsCustomFgFontColor))
                {
                    UpdateColorConfig();
                }
                else if (message.PropertyName == nameof(LyricsStyleSettings.LyricsCustomStrokeFontColor))
                {
                    UpdateColorConfig();
                }
            }
        }

        public void Receive(PropertyChangedMessage<double> message)
        {
            if (message.Sender is LyricsStyleSettings)
            {
                if (message.PropertyName == nameof(LyricsStyleSettings.LyricsLineSpacingFactor))
                {
                    _isLayoutChanged = true;
                }
            }
        }

        public void Receive(PropertyChangedMessage<int> message)
        {
            if (message.Sender is AlbumArtLayoutSettings)
            {
                if (message.PropertyName == nameof(AlbumArtLayoutSettings.AlbumArtSize))
                {
                    _isAlbumArtSizeChanged = true;
                }
            }
            else if (message.Sender is LyricsEffectSettings)
            {
                if (message.PropertyName == nameof(LyricsEffectSettings.LyricsScrollDuration))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsEffectSettings.LyricsScrollTopDuration))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsEffectSettings.LyricsScrollBottomDuration))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsEffectSettings.LyricsScrollTopDelay))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsEffectSettings.LyricsScrollBottomDelay))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsEffectSettings.FanLyricsAngle))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsEffectSettings.Lyrics3DXAngle))
                {
                    _isLyrics3DMatrixChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsEffectSettings.Lyrics3DYAngle))
                {
                    _isLyrics3DMatrixChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsEffectSettings.Lyrics3DZAngle))
                {
                    _isLyrics3DMatrixChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsEffectSettings.Lyrics3DDepth))
                {
                    _isLyrics3DMatrixChanged = true;
                }
            }
            else if (message.Sender is LyricsStyleSettings)
            {
                if (message.PropertyName == nameof(LyricsStyleSettings.PhoneticLyricsFontSize))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsStyleSettings.OriginalLyricsFontSize))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsStyleSettings.TranslatedLyricsFontSize))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsStyleSettings.LyricsFontStrokeWidth))
                {
                    _isLayoutChanged = true;
                }
            }
        }

        public void Receive(PropertyChangedMessage<TextAlignmentType> message)
        {
            if (message.Sender is LyricsStyleSettings)
            {
                if (message.PropertyName == nameof(LyricsStyleSettings.LyricsAlignmentType))
                {
                    _isLayoutChanged = true;
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsDisplayType> message)
        {
            if (message.Sender is LyricsWindowStatus)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.LyricsDisplayType))
                {
                    _isDisplayTypeChanged = true;
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsLayoutOrientation> message)
        {
            if (message.Sender is LyricsWindowStatus)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.LyricsLayoutOrientation))
                {
                    _isLyricsLayoutOrientationChanged = true;
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsFontColorType> message)
        {
            if (message.Sender is LyricsStyleSettings)
            {
                if (message.PropertyName == nameof(LyricsStyleSettings.LyricsBgFontColorType))
                {
                    UpdateColorConfig();
                }
                else if (message.PropertyName == nameof(LyricsStyleSettings.LyricsFgFontColorType))
                {
                    UpdateColorConfig();
                }
                else if (message.PropertyName == nameof(LyricsStyleSettings.LyricsStrokeFontColorType))
                {
                    UpdateColorConfig();
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsFontWeight> message)
        {
            if (message.Sender is LyricsStyleSettings)
            {
                if (message.PropertyName == nameof(LyricsStyleSettings.LyricsFontWeight))
                {
                    _isLyricsFontWeightChanged = true;
                }
            }
        }

        public void Receive(PropertyChangedMessage<ElementTheme> message)
        {
            if (message.Sender is LyricsBackgroundSettings)
            {
                if (message.PropertyName == nameof(LyricsBackgroundSettings.LyricsBackgroundTheme))
                {
                    UpdateColorConfig();
                }
            }
        }

        public void Receive(PropertyChangedMessage<EasingType> message)
        {
            if (message.Sender is LyricsEffectSettings)
            {
                if (message.PropertyName == nameof(LyricsEffectSettings.LyricsScrollEasingType))
                {
                    _canvasYScrollTransition.SetEasingType(message.NewValue);
                }
            }
        }

        public void Receive(PropertyChangedMessage<string> message)
        {
            if (message.Sender is LyricsStyleSettings)
            {
                if (message.PropertyName == nameof(LyricsStyleSettings.LyricsCJKFontFamily))
                {
                    _isLyricsFontFamilyChanged = true;
                }
            }
            if (message.Sender is LyricsStyleSettings)
            {
                if (message.PropertyName == nameof(LyricsStyleSettings.LyricsWesternFontFamily))
                {
                    _isLyricsFontFamilyChanged = true;
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsWindowStatus> message)
        {
            if (message.Sender is LiveStates)
            {
                if (message.PropertyName == nameof(LiveStates.LyricsWindowStatus))
                {
                    UpdateColorConfig();

                    _isLayoutChanged = true;

                    _isLyricsWindowsStatusChanged = true;
                }
            }
        }

        public void Receive(PropertyChangedMessage<AlbumArtLayoutSettings> message)
        {
            if (message.Sender is LyricsWindowStatus)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.AlbumArtLayoutSettings))
                {
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsBackgroundSettings> message)
        {
            if (message.Sender is LyricsWindowStatus)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.LyricsBackgroundSettings))
                {
                    UpdateColorConfig();
                }
            }
        }

        public void Receive(PropertyChangedMessage<SongInfo?> message)
        {
            if (message.Sender is IMediaSessionsService)
            {
                if (message.PropertyName == nameof(IMediaSessionsService.CurrentSongInfo))
                {
                    OnSongInfoChanged();
                }
            }
        }

        public void Receive(PropertyChangedMessage<TimeSpan> message)
        {
            if (message.Sender is IMediaSessionsService)
            {
                if (message.PropertyName == nameof(IMediaSessionsService.CurrentPosition))
                {
                    var diff = Math.Abs(TotalTime.TotalMilliseconds - _mediaSessionsService.CurrentPosition.TotalMilliseconds);
                    var timelineSyncThreshold = _mediaSessionsService.CurrentMediaSourceProviderInfo?.TimelineSyncThreshold ?? 0;
                    if (diff >= timelineSyncThreshold)
                    {
                        TotalTime = _mediaSessionsService.CurrentPosition;
                        if (TotalTime.TotalSeconds <= 1)
                        {
                            _totalPlayingTime = TimeSpan.Zero;
                            _isLastFMTracked = false;
                        }
                    }
                    // 大跨度，刷新布局，避免歌词不显示
                    if (diff >= timelineSyncThreshold + 5000)
                    {
                        _isLayoutChanged = true;
                    }
                }
            }
        }

        public void Receive(PropertyChangedMessage<List<Color>> message)
        {
            if (message.Sender is IMediaSessionsService)
            {
                if (message.PropertyName == nameof(IMediaSessionsService.LightAccentColors))
                {
                    UpdateColorConfig();
                }
                else if (message.PropertyName == nameof(IMediaSessionsService.DarkAccentColors))
                {
                    UpdateColorConfig();
                }
            }
        }
    }
}
