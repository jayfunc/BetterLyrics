using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
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
            IRecipient<PropertyChangedMessage<float>>,
            IRecipient<PropertyChangedMessage<bool>>,
            IRecipient<PropertyChangedMessage<Color>>,
            IRecipient<PropertyChangedMessage<LyricsDisplayType>>,
            IRecipient<PropertyChangedMessage<LyricsFontColorType>>,
            IRecipient<PropertyChangedMessage<TextAlignmentType>>,
            IRecipient<PropertyChangedMessage<LyricsFontWeight>>,
            IRecipient<PropertyChangedMessage<LineRenderingType>>,
            IRecipient<PropertyChangedMessage<ElementTheme>>,
            IRecipient<PropertyChangedMessage<EasingType>>,
            IRecipient<PropertyChangedMessage<DockPlacement>>,
            IRecipient<PropertyChangedMessage<ObservableCollection<MediaSourceProviderInfo>>>,
            IRecipient<PropertyChangedMessage<ObservableCollection<LocalMediaFolder>>>
    {
        public void Receive(PropertyChangedMessage<ObservableCollection<LocalMediaFolder>> message)
        {
            if (message.Sender is SettingsPageViewModel.SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.LocalMediaFolders))
                {
                    // Music lib changed, re-fetch lyrics
                    _logger.LogInformation("Local lyrics folders changed, refreshing lyrics.");
                    _ = _refreshLyricsRunner.RunAsync(async tokne =>
                    {
                        await RefreshLyricsAsync(tokne);
                    });
                }
            }
        }

        public void Receive(PropertyChangedMessage<ObservableCollection<MediaSourceProviderInfo>> message)
        {
            if (message.Sender is SettingsPageViewModel.SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.MediaSourceProvidersInfo))
                {
                    _mediaSourceProvidersInfo = message.NewValue.ToList();

                    UpdateTimelineSyncThreshold();
                    UpdatePositionOffset();
                    UpdateIsLastFMTrackEnabled();

                    // Media source providers info changed (maybe include lyrics search providers info changed), re-fetch lyrics
                    _logger.LogInformation("Lyrics search providers info changed, refreshing lyrics.");
                    _ = _refreshLyricsRunner.RunAsync(async token =>
                    {
                        await RefreshLyricsAsync(token);
                    });
                }
            }
        }

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender is SettingsPageViewModel.SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.IsDynamicCoverOverlayEnabled))
                {
                    _isDynamicCoverOverlayEnabled = message.NewValue;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.IsDebugOverlayEnabled))
                {
                    _isDebugOverlayEnabled = message.NewValue;
                    _isDebugOverlayEnabledChanged = true;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.IsLyricsGlowEffectEnabled))
                {
                    _isLyricsGlowEffectEnabled = message.NewValue;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.IsFanLyricsEnabled))
                {
                    _isFanLyricsEnabled = message.NewValue;
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.IsLyricsFloatAnimationEnabled))
                {
                    _isLyricsFloatAnimationEnabled = message.NewValue;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.IsLibreTranslateEnabled))
                {
                    _isLibreTranslateEnabled = message.NewValue;
                    UpdateTranslations();
                }
            }
            else if (message.Sender is LyricsWindowViewModel)
            {
                if (message.PropertyName == nameof(LyricsWindowViewModel.IsDockMode))
                {
                    _isDockMode = message.NewValue;
                    UpdateColorConfig();
                    UpdateImmersiveBackgroundOpacity();
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsWindowViewModel.IsDesktopMode))
                {
                    _isDesktopMode = message.NewValue;
                    UpdateColorConfig();
                    UpdateImmersiveBackgroundOpacity();
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(LyricsWindowViewModel.IsLyricsWindowLocked))
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
            else if (message.Sender is LyricsPageViewModel)
            {
                if (message.PropertyName == nameof(LyricsPageViewModel.IsTranslationEnabled))
                {
                    _isTranslationEnabled = message.NewValue;
                    _logger.LogInformation("Translation enabled state changed: {IsEnabled}", _isTranslationEnabled);
                    UpdateTranslations();
                }
                else if (message.PropertyName == nameof(LyricsPageViewModel.ShowTranslationOnly))
                {
                    _showTranslationOnly = message.NewValue;
                    UpdateTranslations();
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
            else if (message.Sender is SettingsPageViewModel.SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.LyricsCustomBgFontColor))
                {
                    _customBgFontColor = message.NewValue;
                    UpdateColorConfig();
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.LyricsCustomFgFontColor))
                {
                    _customFgFontColor = message.NewValue;
                    UpdateColorConfig();
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.LyricsCustomStrokeFontColor))
                {
                    _customStrokeFontColor = message.NewValue;
                    UpdateColorConfig();
                }
            }
        }

        public void Receive(PropertyChangedMessage<float> message)
        {
            if (message.Sender is SettingsPageViewModel.SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.LyricsLineSpacingFactor))
                {
                    _lyricsLineSpacingFactor = message.NewValue;
                    _isLayoutChanged = true;
                }
            }
        }

        public void Receive(PropertyChangedMessage<int> message)
        {
            if (message.Sender is SettingsPageViewModel.SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.CoverImageRadius))
                {
                    _albumArtCornerRadius = message.NewValue;
                    _isAlbumArtCornerRadiusChanged = true;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.CoverOverlayOpacity))
                {
                    _albumArtBgOpacity = message.NewValue;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.CoverOverlayBlurAmount))
                {
                    _albumArtBgBlurAmount = message.NewValue;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.CoverAcrylicEffectAmount))
                {
                    _coverAcrylicEffectAmount = message.NewValue;
                    _isCoverAcrylicEffectAmountChanged = true;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.LyricsVerticalEdgeOpacity))
                {
                    _lyricsVerticalEdgeOpacity = message.NewValue;
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.LyricsBlurAmount))
                {
                    _lyricsBlurAmount = message.NewValue;
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.LyricsStandardFontSize))
                {
                    _lyricsStandardFontSize = message.NewValue;
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.LyricsDockFontSize))
                {
                    _lyricsDockFontSize = message.NewValue;
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.LyricsDesktopFontSize))
                {
                    _lyricsDesktopFontSize = message.NewValue;
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.SelectedTargetLanguageIndex))
                {
                    _targetLanguageIndex = message.NewValue;
                    _logger.LogInformation("Target language index changed: {Index}", _targetLanguageIndex);
                    UpdateTranslations();
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.LyricsFontStrokeWidth))
                {
                    _lyricsFontStrokeWidth = message.NewValue;
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.LyricsScrollDuration))
                {
                    _canvasYScrollTransition.SetDuration(message.NewValue / 1000f);
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.LyricsBgFontOpacity))
                {
                    _defaultOpacity = message.NewValue / 100f;
                    _isLayoutChanged = true;
                }
            }
        }

        public void Receive(PropertyChangedMessage<LineRenderingType> message)
        {
            if (message.Sender is SettingsPageViewModel.SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.LyricsGlowEffectScope))
                {
                    _lyricsGlowEffectScope = message.NewValue;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.LyricsHighlightScope))
                {
                    _lyricsHighlightScope = message.NewValue;
                }
            }
        }

        public void Receive(PropertyChangedMessage<TextAlignmentType> message)
        {
            if (message.Sender is SettingsPageViewModel.SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.LyricsAlignmentType))
                {
                    _lyricsAlignmentType = message.NewValue;
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.SongInfoAlignmentType))
                {
                    _titleTextFormat.HorizontalAlignment = _artistTextFormat.HorizontalAlignment =
                        message.NewValue.ToCanvasHorizontalAlignment();
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsDisplayType> message)
        {
            _displayTypeReceived = message.NewValue;
        }

        public void Receive(PropertyChangedMessage<LyricsFontColorType> message)
        {
            if (message.Sender is SettingsPageViewModel.SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.LyricsBgFontColorType))
                {
                    _lyricsBgFontColorType = message.NewValue;
                    UpdateColorConfig();
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.LyricsFgFontColorType))
                {
                    _lyricsFgFontColorType = message.NewValue;
                    UpdateColorConfig();
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.LyricsStrokeFontColorType))
                {
                    _lyricsStrokeFontColorType = message.NewValue;
                    UpdateColorConfig();
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsFontWeight> message)
        {
            if (message.Sender is SettingsPageViewModel.SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.LyricsFontWeight))
                {
                    _lyricsTextFormat.FontWeight = message.NewValue.ToFontWeight();
                    _isLayoutChanged = true;
                }
            }
        }

        public void Receive(PropertyChangedMessage<ElementTheme> message)
        {
            if (message.Sender is SettingsPageViewModel.SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.LyricsBackgroundTheme))
                {
                    _lyricsBgTheme = message.NewValue;
                    UpdateColorConfig();
                }
            }
        }

        public void Receive(PropertyChangedMessage<EasingType> message)
        {
            if (message.Sender is SettingsPageViewModel.SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.LyricsScrollEasingType))
                {
                    _canvasYScrollTransition.SetEasingType(message.NewValue);
                }
            }
        }

        public void Receive(PropertyChangedMessage<string> message)
        {
            if (message.Sender is SettingsPageViewModel.SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.LyricsFontFamily))
                {
                    _lyricsTextFormat.FontFamily = _artistTextFormat.FontFamily = _titleTextFormat.FontFamily = message.NewValue;
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.LyricsTranslationSeparator))
                {
                    _lyricsTranslationSeparator = message.NewValue;
                    UpdateTranslations();
                }
            }
        }

        public void Receive(PropertyChangedMessage<DockPlacement> message)
        {
            if (message.Sender is SettingsPageViewModel.SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.DockPlacement))
                {
                    _dockPlacement = message.NewValue;
                }
            }
        }
    }
}
