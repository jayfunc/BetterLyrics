using System;
using System.Collections.ObjectModel;
using System.Linq;
using BetterInAppLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.Graphics.Imaging;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsRendererViewModel
        : IRecipient<PropertyChangedMessage<int>>,
            IRecipient<PropertyChangedMessage<float>>,
            IRecipient<PropertyChangedMessage<double>>,
            IRecipient<PropertyChangedMessage<bool>>,
            IRecipient<PropertyChangedMessage<Color>>,
            IRecipient<PropertyChangedMessage<LyricsDisplayType>>,
            IRecipient<PropertyChangedMessage<LyricsFontColorType>>,
            IRecipient<PropertyChangedMessage<LyricsAlignmentType>>,
            IRecipient<PropertyChangedMessage<LyricsFontWeight>>,
            IRecipient<PropertyChangedMessage<LineRenderingType>>,
            IRecipient<PropertyChangedMessage<ObservableCollection<LyricsSearchProviderInfo>>>,
            IRecipient<PropertyChangedMessage<ObservableCollection<LocalLyricsFolder>>>
    {
        public async void Receive(
            PropertyChangedMessage<ObservableCollection<LocalLyricsFolder>> message
        )
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

        public async void Receive(
            PropertyChangedMessage<ObservableCollection<LyricsSearchProviderInfo>> message
        )
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
                if (
                    message.PropertyName
                    == nameof(SettingsPageViewModel.IsDynamicCoverOverlayEnabled)
                )
                {
                    IsDynamicCoverOverlayEnabled = message.NewValue;
                }
                else if (
                    message.PropertyName == nameof(SettingsPageViewModel.IsDebugOverlayEnabled)
                )
                {
                    _isDebugOverlayEnabled = message.NewValue;
                }
            }
            else if (message.Sender is LyricsSettingsControlViewModel)
            {
                if (
                    message.PropertyName
                    == nameof(LyricsSettingsControlViewModel.IsLyricsGlowEffectEnabled)
                )
                {
                    IsLyricsGlowEffectEnabled = message.NewValue;
                }
                else if (
                    message.PropertyName
                    == nameof(LyricsSettingsControlViewModel.IsFanLyricsEnabled)
                )
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
            else if (message.Sender is LyricsSettingsControlViewModel)
            {
                if (message.PropertyName == nameof(LyricsSettingsControlViewModel.LyricsCustomFontColor))
                {
                    _customFontColor = message.NewValue;
                    UpdateFontColor();
                }
            }
        }

        public void Receive(PropertyChangedMessage<double> message)
        {
            if (message.Sender is LyricsPageViewModel)
            {
                if (message.PropertyName == nameof(LyricsPageViewModel.MaxLyricsWidth))
                {
                    _maxLyricsWidthTransition.StartTransition((float)message.NewValue);
                }
            }
        }

        public void Receive(PropertyChangedMessage<float> message)
        {
            if (message.Sender is LyricsSettingsControlViewModel)
            {
                if (
                    message.PropertyName
                    == nameof(LyricsSettingsControlViewModel.LyricsLineSpacingFactor)
                )
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
                    CoverImageRadius = message.NewValue;
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.CoverOverlayOpacity))
                {
                    CoverOverlayOpacity = message.NewValue;
                }
                else if (
                    message.PropertyName == nameof(SettingsPageViewModel.CoverOverlayBlurAmount)
                )
                {
                    CoverOverlayBlurAmount = message.NewValue;
                }
            }
            else if (message.Sender is LyricsSettingsControlViewModel)
            {
                if (
                    message.PropertyName
                    == nameof(LyricsSettingsControlViewModel.LyricsVerticalEdgeOpacity)
                )
                {
                    LyricsVerticalEdgeOpacity = message.NewValue;
                }
                else if (
                    message.PropertyName == nameof(LyricsSettingsControlViewModel.LyricsBlurAmount)
                )
                {
                    LyricsBlurAmount = message.NewValue;
                }
                else if (
                    message.PropertyName == nameof(LyricsSettingsControlViewModel.LyricsFontSize)
                )
                {
                    LyricsFontSize = message.NewValue;
                }
            }
        }

        public void Receive(PropertyChangedMessage<LineRenderingType> message)
        {
            if (message.Sender is LyricsSettingsControlViewModel)
            {
                if (
                    message.PropertyName
                    == nameof(LyricsSettingsControlViewModel.LyricsGlowEffectScope)
                )
                {
                    LyricsGlowEffectScope = message.NewValue;
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsAlignmentType> message)
        {
            if (message.Sender is LyricsSettingsControlViewModel)
            {
                if (
                    message.PropertyName
                    == nameof(LyricsSettingsControlViewModel.LyricsAlignmentType)
                )
                {
                    LyricsAlignmentType = message.NewValue;
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsDisplayType> message)
        {
            DisplayType = message.NewValue;
        }

        public void Receive(PropertyChangedMessage<LyricsFontColorType> message)
        {
            if (message.Sender is LyricsSettingsControlViewModel)
            {
                if (
                    message.PropertyName
                    == nameof(LyricsSettingsControlViewModel.LyricsFontColorType)
                )
                {
                    LyricsFontColorType = message.NewValue;
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsFontWeight> message)
        {
            if (message.Sender is LyricsSettingsControlViewModel)
            {
                if (message.PropertyName == nameof(LyricsSettingsControlViewModel.LyricsFontWeight))
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
            _textFormat.FontWeight = value.ToFontWeight();
            _isRelayoutNeeded = true;
        }

        partial void OnLyricsLineSpacingFactorChanged(float value)
        {
            _isRelayoutNeeded = true;
        }

        async partial void OnSongInfoChanged(SongInfo? oldValue, SongInfo? newValue)
        {
            TotalTime = TimeSpan.Zero;

            _lastAlbumArtBitmap = _albumArtBitmap;

            if (newValue?.AlbumArt is byte[] bytes)
            {
                var decoder = await ImageHelper.GetDecoderFromByte(bytes);
                _albumArtBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                _albumArtAccentColor = (ImageHelper.GetAccentColorsFromByte(bytes)).SafeGet(0);
            }
            else
            {
                _albumArtBitmap = null;
                _albumArtAccentColor = null;
            }

            _lyricsWindowBgColor = _albumArtAccentColor ?? Colors.Gray;

            if (!_isDesktopMode && !_isDockMode) _adaptiveFontColor = Helper.ColorHelper.GetForegroundColor(_lyricsWindowBgColor);

            UpdateFontColor();

            _albumArtBgTransition.Reset(0f);
            _albumArtBgTransition.StartTransition(1f);

            await RefreshLyricsAsync();
        }
    }
}
