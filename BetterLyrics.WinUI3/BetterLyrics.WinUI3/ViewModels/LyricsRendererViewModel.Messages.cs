using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels
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
            IRecipient<PropertyChangedMessage<ObservableCollection<LyricsSearchProviderInfo>>>,
            IRecipient<PropertyChangedMessage<ObservableCollection<LocalMediaFolder>>>
    {
        public void Receive(PropertyChangedMessage<ObservableCollection<LocalMediaFolder>> message)
        {
            if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.LocalMediaFolders))
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

        public void Receive(PropertyChangedMessage<ObservableCollection<LyricsSearchProviderInfo>> message)
        {
            if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.LyricsSearchProvidersInfo))
                {
                    // Lyrics search providers info changed, re-fetch lyrics
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
            if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.IsDynamicCoverOverlayEnabled))
                {
                    _isDynamicCoverOverlayEnabled = message.NewValue;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.IsDebugOverlayEnabled))
                {
                    _isDebugOverlayEnabled = message.NewValue;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.IsLyricsGlowEffectEnabled))
                {
                    _isLyricsGlowEffectEnabled = message.NewValue;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.IsFanLyricsEnabled))
                {
                    _isFanLyricsEnabled = message.NewValue;
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.IsLyricsFloatAnimationEnabled))
                {
                    _isLyricsFloatAnimationEnabled = message.NewValue;
                }
            }
            else if (message.Sender is LyricsWindowViewModel)
            {
                if (message.PropertyName == nameof(LyricsWindowViewModel.IsDockMode))
                {
                    _isDockMode = message.NewValue;
                    UpdateColorConfig();
                    UpdateImmersiveBackgroundOpacity();
                }
                else if (message.PropertyName == nameof(LyricsWindowViewModel.IsDesktopMode))
                {
                    _isDesktopMode = message.NewValue;
                    UpdateColorConfig();
                    UpdateImmersiveBackgroundOpacity();
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
                    _immersiveBgTransition.StartTransition(message.NewValue);
                    _environmentalColor = message.NewValue;
                    UpdateColorConfig();
                }
            }
            else if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.LyricsCustomBgFontColor))
                {
                    _customBgFontColor = message.NewValue;
                    UpdateColorConfig();
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.LyricsCustomFgFontColor))
                {
                    _customFgFontColor = message.NewValue;
                    UpdateColorConfig();
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.LyricsCustomStrokeFontColor))
                {
                    _customStrokeFontColor = message.NewValue;
                    UpdateColorConfig();
                }
            }
        }

        public void Receive(PropertyChangedMessage<float> message)
        {
            if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.LyricsLineSpacingFactor))
                {
                    _lyricsLineSpacingFactor = message.NewValue;
                    _isLayoutChanged = true;
                }
            }
        }

        public void Receive(PropertyChangedMessage<int> message)
        {
            if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.CoverImageRadius))
                {
                    _albumArtCornerRadius = message.NewValue;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.CoverOverlayOpacity))
                {
                    _albumArtBgOpacity = message.NewValue;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.CoverOverlayBlurAmount))
                {
                    _albumArtBgBlurAmount = message.NewValue;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.LyricsVerticalEdgeOpacity))
                {
                    _lyricsVerticalEdgeOpacity = message.NewValue;
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.LyricsBlurAmount))
                {
                    _lyricsBlurAmount = message.NewValue;
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.LyricsFontSize))
                {
                    _lyricsFontSize = message.NewValue;
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.SelectedTargetLanguageIndex))
                {
                    _targetLanguageIndex = message.NewValue;
                    _logger.LogInformation("Target language index changed: {Index}", _targetLanguageIndex);
                    UpdateTranslations();
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.LyricsFontStrokeWidth))
                {
                    _lyricsFontStrokeWidth = message.NewValue;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.LyricsScrollDuration))
                {
                    _canvasYScrollTransition.SetDuration(message.NewValue / 1000f);
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.TimelineSyncThreshold))
                {
                    _timelineSyncThreshold = message.NewValue;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.LyricsBgFontOpacity))
                {
                    _defaultOpacity = message.NewValue / 100f;
                    _isLayoutChanged = true;
                }
            }
            else if (message.Sender is LyricsPageViewModel)
            {
                if (message.PropertyName == nameof(LyricsPageViewModel.PositionOffset))
                {
                    _positionOffset = TimeSpan.FromMilliseconds(message.NewValue);
                }
            }
        }

        public void Receive(PropertyChangedMessage<LineRenderingType> message)
        {
            if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.LyricsGlowEffectScope))
                {
                    _lyricsGlowEffectScope = message.NewValue;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.LyricsHighlightScope))
                {
                    _lyricsHighlightScope = message.NewValue;
                }
            }
        }

        public void Receive(PropertyChangedMessage<TextAlignmentType> message)
        {
            if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.LyricsAlignmentType))
                {
                    _lyricsAlignmentType = message.NewValue;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.SongInfoAlignmentType))
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
            if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.LyricsBgFontColorType))
                {
                    _lyricsBgFontColorType = message.NewValue;
                    UpdateColorConfig();
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.LyricsFgFontColorType))
                {
                    _lyricsFgFontColorType = message.NewValue;
                    UpdateColorConfig();
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.LyricsStrokeFontColorType))
                {
                    _lyricsStrokeFontColorType = message.NewValue;
                    UpdateColorConfig();
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsFontWeight> message)
        {
            if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.LyricsFontWeight))
                {
                    _lyricsTextFormat.FontWeight = message.NewValue.ToFontWeight();
                    _isLayoutChanged = true;
                }
            }
        }

        public void Receive(PropertyChangedMessage<ElementTheme> message)
        {
            if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.LyricsBackgroundTheme))
                {
                    _lyricsBgTheme = message.NewValue;
                    UpdateColorConfig();
                }
            }
        }

        public void Receive(PropertyChangedMessage<EasingType> message)
        {
            if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.LyricsScrollEasingType))
                {
                    _canvasYScrollTransition.SetEasingType(message.NewValue);
                }
            }
        }

        public void Receive(PropertyChangedMessage<string> message)
        {
            if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.LyricsFontFamily))
                {
                    _lyricsTextFormat.FontFamily = _artistTextFormat.FontFamily = _titleTextFormat.FontFamily = message.NewValue;
                    _isLayoutChanged = true;
                }
            }
        }

    }
}
