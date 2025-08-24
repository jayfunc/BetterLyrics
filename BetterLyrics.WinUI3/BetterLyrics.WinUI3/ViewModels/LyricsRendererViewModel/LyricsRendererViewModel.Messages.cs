using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels.LyricsRendererViewModel
{
    public partial class LyricsRendererViewModel
        : IRecipient<PropertyChangedMessage<int>>,
            IRecipient<PropertyChangedMessage<string>>,
            IRecipient<PropertyChangedMessage<double>>,
            IRecipient<PropertyChangedMessage<bool>>,
            IRecipient<PropertyChangedMessage<Color>>,
            IRecipient<PropertyChangedMessage<LyricsWindowMode>>,
            IRecipient<PropertyChangedMessage<LyricsDisplayType>>,
            IRecipient<PropertyChangedMessage<LyricsFontColorType>>,
            IRecipient<PropertyChangedMessage<TextAlignmentType>>,
            IRecipient<PropertyChangedMessage<LyricsFontWeight>>,
            IRecipient<PropertyChangedMessage<LineRenderingType>>,
            IRecipient<PropertyChangedMessage<ElementTheme>>,
            IRecipient<PropertyChangedMessage<EasingType>>
    {

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.IsDebugOverlayEnabled))
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
            }
            else if (message.Sender is LyricsWindowViewModel)
            {
                if (message.PropertyName == nameof(LyricsWindowViewModel.IsLyricsWindowLocked))
                {
                    _isLyricsWindowLocked = message.NewValue;
                    UpdateImmersiveBackgroundOpacity();
                }
                else if (message.PropertyName == nameof(LyricsWindowViewModel.IsMouseWithinWindow))
                {
                    _isMouseWithinWindow = message.NewValue;
                    UpdateImmersiveBackgroundOpacity();
                }
            }
            else if (message.Sender is MediaSourceProviderInfo)
            {
                if (message.PropertyName == nameof(MediaSourceProviderInfo.IsLastFMTrackEnabled))
                {
                    UpdateIsLastFMTrackEnabled();
                }
            }
            else if (message.Sender is LyricsStyleSettings)
            {
                if (message.PropertyName == nameof(LyricsStyleSettings.IsDynamicLyricsFontSize))
                {
                    _isLayoutChanged = true;
                }
            }
        }

        public void Receive(PropertyChangedMessage<Color> message)
        {
            if (message.Sender is LyricsWindowViewModel)
            {
                if (message.PropertyName == nameof(LyricsWindowViewModel.ActivatedWindowAccentColor))
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
                if (message.PropertyName == nameof(AlbumArtLayoutSettings.CoverImageRadius))
                {
                    _isAlbumArtCornerRadiusChanged = true;
                }
                else if (message.PropertyName == nameof(AlbumArtLayoutSettings.CoverImageShadowAmount))
                {
                    _isAlbumArtShadowAmountChanged = true;
                }
                else if (message.PropertyName == nameof(AlbumArtLayoutSettings.SongInfoFontSize))
                {
                    UpdateSongInfoFontSize();
                }
            }
            else if (message.Sender is LyricsBackgroundSettings)
            {
                if (message.PropertyName == nameof(LyricsBackgroundSettings.CoverOverlayOpacity))
                {
                    _isAlbumArtBgOpacityChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsBackgroundSettings.CoverOverlayBlurAmount))
                {
                    _isAlbumArtBgBlurAmountChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsBackgroundSettings.CoverAcrylicEffectAmount))
                {
                    _isCoverAcrylicEffectAmountChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsBackgroundSettings.CoverOverlaySpeed))
                {
                }
            }
            else if (message.Sender is LyricsEffectSettings)
            {
                if (message.PropertyName == nameof(LyricsEffectSettings.LyricsVerticalEdgeOpacity))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsEffectSettings.LyricsBlurAmount))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsEffectSettings.LyricsScrollDuration))
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
            }
            else if (message.Sender is LyricsStyleSettings)
            {
                if (message.PropertyName == nameof(LyricsStyleSettings.LyricsFontSize))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsStyleSettings.LyricsFontStrokeWidth))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsStyleSettings.LyricsBgFontOpacity))
                {
                    _isLayoutChanged = true;
                }
            }
            else if (message.Sender is MediaSourceProviderInfo)
            {
                if (message.PropertyName == nameof(MediaSourceProviderInfo.TimelineSyncThreshold))
                {
                    UpdateTimelineSyncThreshold();
                }
                else if (message.PropertyName == nameof(MediaSourceProviderInfo.PositionOffset))
                {
                    UpdatePositionOffset();
                }
            }
        }

        public void Receive(PropertyChangedMessage<LineRenderingType> message)
        {
            if (message.Sender is LyricsEffectSettings)
            {
                if (message.PropertyName == nameof(LyricsEffectSettings.LyricsGlowEffectScope))
                {
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsEffectSettings.LyricsHighlightScope))
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
            else if (message.Sender is AlbumArtLayoutSettings)
            {
                if (message.PropertyName == nameof(AlbumArtLayoutSettings.SongInfoAlignmentType))
                {
                    _titleTextFormat.HorizontalAlignment = _artistTextFormat.HorizontalAlignment =
                        _settingsService.AppSettings.AlbumArtLayoutSettings.SongInfoAlignmentType.ToCanvasHorizontalAlignment();
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsDisplayType> message)
        {
            if (message.Sender is LiveStates)
            {
                if (message.PropertyName == nameof(LiveStates.LyricsDisplayType))
                {
                    _isDisplayTypeChanged = true;
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
                    _isLayoutChanged = true;
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
                if (message.PropertyName == nameof(LyricsStyleSettings.LyricsFontFamily))
                {
                    _isLayoutChanged = true;
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsWindowMode> message)
        {
            if (message.Sender is LiveStates)
            {
                if (message.PropertyName == nameof(LiveStates.LyricsWindowMode))
                {
                    UpdateColorConfig();
                    UpdateImmersiveBackgroundOpacity();
                    _isLayoutChanged = true;
                }
            }
        }
    }
}
