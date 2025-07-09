using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsRendererViewModel
        : IRecipient<PropertyChangedMessage<int>>,
            IRecipient<PropertyChangedMessage<float>>,
            IRecipient<PropertyChangedMessage<bool>>,
            IRecipient<PropertyChangedMessage<Color>>,
            IRecipient<PropertyChangedMessage<LyricsDisplayType>>,
            IRecipient<PropertyChangedMessage<LyricsFontColorType>>,
            IRecipient<PropertyChangedMessage<TextAlignmentType>>,
            IRecipient<PropertyChangedMessage<LyricsFontWeight>>,
            IRecipient<PropertyChangedMessage<LineRenderingType>>,
            IRecipient<PropertyChangedMessage<ElementTheme>>,
            IRecipient<PropertyChangedMessage<ObservableCollection<LyricsSearchProviderInfo>>>,
            IRecipient<PropertyChangedMessage<ObservableCollection<LocalLyricsFolder>>>
    {
        public void Receive(PropertyChangedMessage<ObservableCollection<LocalLyricsFolder>> message)
        {
            if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.LocalLyricsFolders))
                {
                    // Music lib changed, re-fetch lyrics
                    _logger.LogInformation("Local lyrics folders changed, refreshing lyrics.");
                    RefreshLyricsAsync();
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
                    RefreshLyricsAsync();
                }
            }
        }

        // Receive methods for handling messages from other view models

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.IsDynamicCoverOverlayEnabled))
                {
                    IsDynamicCoverOverlayEnabled = message.NewValue;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.IsDebugOverlayEnabled))
                {
                    _isDebugOverlayEnabled = message.NewValue;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.IsLyricsGlowEffectEnabled))
                {
                    IsLyricsGlowEffectEnabled = message.NewValue;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.IsFanLyricsEnabled))
                {
                    _isFanLyricsEnabled = message.NewValue;
                    _isLayoutChanged = true;
                }
            }
            else if (message.Sender is LyricsWindowViewModel)
            {
                if (message.PropertyName == nameof(LyricsWindowViewModel.IsDockMode))
                {
                    _isDockMode = message.NewValue;
                    UpdateFontColor();
                }
                else if (message.PropertyName == nameof(LyricsWindowViewModel.IsDesktopMode))
                {
                    _isDesktopMode = message.NewValue;
                    UpdateFontColor();
                }
            }
            else if (message.Sender is LyricsPageViewModel)
            {
                if (message.PropertyName == nameof(LyricsPageViewModel.IsTranslationEnabled))
                {
                    _isTranslationEnabled = message.NewValue;
                    _logger.LogInformation("Translation enabled state changed: {IsEnabled}", _isTranslationEnabled);
                    UpdateTranslationsAsync();
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
                    UpdateFontColor();
                }
            }
            else if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.LyricsCustomBgFontColor))
                {
                    _customBgFontColor = message.NewValue;
                    UpdateFontColor();
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.LyricsCustomFgFontColor))
                {
                    _customFgFontColor = message.NewValue;
                    UpdateFontColor();
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.LyricsCustomStrokeFontColor))
                {
                    _customStrokeFontColor = message.NewValue;
                    UpdateFontColor();
                }
            }
        }

        public void Receive(PropertyChangedMessage<float> message)
        {
            if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.LyricsLineSpacingFactor))
                {
                    LyricsLineSpacingFactor = message.NewValue;
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
                    CoverOverlayOpacity = message.NewValue;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.CoverOverlayBlurAmount))
                {
                    CoverOverlayBlurAmount = message.NewValue;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.LyricsVerticalEdgeOpacity))
                {
                    LyricsVerticalEdgeOpacity = message.NewValue;
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.LyricsBlurAmount))
                {
                    LyricsBlurAmount = message.NewValue;
                    _isLayoutChanged = true;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.LyricsFontSize))
                {
                    LyricsFontSize = message.NewValue;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.SelectedTargetLanguageIndex))
                {
                    _targetLanguageIndex = message.NewValue;
                    _logger.LogInformation("Target language index changed: {Index}", _targetLanguageIndex);
                    UpdateTranslationsAsync();
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.LyricsFontStrokeWidth))
                {
                    _lyricsFontStrokeWidth = message.NewValue;
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
                    LyricsGlowEffectScope = message.NewValue;
                }
            }
        }

        public void Receive(PropertyChangedMessage<TextAlignmentType> message)
        {
            if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.LyricsAlignmentType))
                {
                    LyricsAlignmentType = message.NewValue;
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
                    UpdateFontColor();
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.LyricsFgFontColorType))
                {
                    _lyricsFgFontColorType = message.NewValue;
                    UpdateFontColor();
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.LyricsStrokeFontColorType))
                {
                    _lyricsStrokeFontColorType = message.NewValue;
                    UpdateFontColor();
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsFontWeight> message)
        {
            if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.LyricsFontWeight))
                {
                    LyricsFontWeight = message.NewValue;
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
                    UpdateFontColor();
                }
            }
        }

        partial void OnLyricsFontSizeChanged(int value)
        {
            _isLayoutChanged = true;
        }

        partial void OnLyricsFontWeightChanged(LyricsFontWeight value)
        {
            _lyricsTextFormat.FontWeight = value.ToFontWeight();
            _isLayoutChanged = true;
        }

        partial void OnLyricsLineSpacingFactorChanged(float value)
        {
            _isLayoutChanged = true;
        }
    }
}
