using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
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
            IRecipient<PropertyChangedMessage<ObservableCollection<LyricsSearchProviderInfo>>>,
            IRecipient<PropertyChangedMessage<ObservableCollection<LocalLyricsFolder>>>
    {
        public async void Receive(PropertyChangedMessage<ObservableCollection<LocalLyricsFolder>> message)
        {
            if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.LocalLyricsFolders))
                {
                    // Music lib changed, re-fetch lyrics
                    await RefreshLyricsAsync();
                }
            }
        }

        public async void Receive(PropertyChangedMessage<ObservableCollection<LyricsSearchProviderInfo>> message)
        {
            if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.LyricsSearchProvidersInfo))
                {
                    // Lyrics search providers info changed, re-fetch lyrics
                    await RefreshLyricsAsync();
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
                }
            }
            else if (message.Sender is LyricsWindowViewModel)
            {
                if (message.PropertyName == nameof(LyricsWindowViewModel.IsDockMode))
                {
                    _isDockMode = message.NewValue;
                }
                else if (message.PropertyName == nameof(LyricsWindowViewModel.IsDesktopMode))
                {
                    _isDesktopMode = message.NewValue;
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
                    _lyricsWindowBgColor = message.NewValue;
                    _adaptiveFontColor = Helper.ColorHelper.GetForegroundColor(_lyricsWindowBgColor);
                    UpdateFontColor();
                }
            }
            else if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.LyricsCustomFontColor))
                {
                    _customFontColor = message.NewValue;
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
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.LyricsBlurAmount))
                {
                    LyricsBlurAmount = message.NewValue;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.LyricsFontSize))
                {
                    LyricsFontSize = message.NewValue;
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
                if (message.PropertyName == nameof(SettingsPageViewModel.LyricsFontColorType))
                {
                    LyricsFontColorType = message.NewValue;
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

        partial void OnLyricsFontColorTypeChanged(LyricsFontColorType value)
        {
            UpdateFontColor();
        }

        partial void OnLyricsFontSizeChanged(int value)
        {
            _isRelayoutNeeded = true;
        }

        partial void OnLyricsFontWeightChanged(LyricsFontWeight value)
        {
            _lyricsTextFormat.FontWeight = value.ToFontWeight();
            _isRelayoutNeeded = true;
        }

        partial void OnLyricsLineSpacingFactorChanged(float value)
        {
            _isRelayoutNeeded = true;
        }
    }
}
