using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterInAppLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
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
            IRecipient<PropertyChangedMessage<ElementTheme>>,
            IRecipient<PropertyChangedMessage<LyricsFontWeight>>,
            IRecipient<PropertyChangedMessage<LineRenderingType>>,
            IRecipient<PropertyChangedMessage<ObservableCollection<LyricsSearchProviderInfo>>>,
            IRecipient<PropertyChangedMessage<ObservableCollection<LocalLyricsFolder>>>
    {
        /// <summary>
        /// The OnLyricsFontColorTypeChanged
        /// </summary>
        /// <param name="value">The value<see cref="LyricsFontColorType"/></param>
        partial void OnLyricsFontColorTypeChanged(LyricsFontColorType value)
        {
            UpdateFontColor();
        }

        /// <summary>
        /// The OnLyricsFontSizeChanged
        /// </summary>
        /// <param name="value">The value<see cref="int"/></param>
        partial void OnLyricsFontSizeChanged(int value)
        {
            _isRelayoutNeeded = true;
        }

        /// <summary>
        /// The OnLyricsFontWeightChanged
        /// </summary>
        /// <param name="value">The value<see cref="LyricsFontWeight"/></param>
        partial void OnLyricsFontWeightChanged(LyricsFontWeight value)
        {
            _textFormat.FontWeight = value.ToFontWeight();
        }

        /// <summary>
        /// The OnLyricsLineSpacingFactorChanged
        /// </summary>
        /// <param name="value">The value<see cref="float"/></param>
        partial void OnLyricsLineSpacingFactorChanged(float value)
        {
            _isRelayoutNeeded = true;
        }

        /// <summary>
        /// The OnSongInfoChanged
        /// </summary>
        /// <param name="oldValue">The oldValue<see cref="SongInfo?"/></param>
        /// <param name="newValue">The newValue<see cref="SongInfo?"/></param>
        async partial void OnSongInfoChanged(SongInfo? oldValue, SongInfo? newValue)
        {
            TotalTime = TimeSpan.Zero;

            _lastAlbumArtBitmap = _albumArtBitmap;

            if (newValue?.AlbumArt is byte[] bytes)
            {
                _albumArtBitmap = await (
                    await ImageHelper.GetDecoderFromByte(bytes)
                ).GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                _albumArtAccentColor = (
                    await ImageHelper.GetAccentColorsFromByte(bytes)
                ).FirstOrDefault();
            }
            else
            {
                _albumArtBitmap = null;
                _albumArtAccentColor = null;
            }

            UpdateFontColor();

            _albumArtBgTransition.Reset(0f);
            _albumArtBgTransition.StartTransition(1f);

            await RefreshLyricsAsync();
        }

        /// <summary>
        /// The OnThemeChanged
        /// </summary>
        /// <param name="value">The value<see cref="ElementTheme"/></param>
        partial void OnThemeChanged(ElementTheme value)
        {
            UpdateFontColor();
        }

        // Receive methods for handling messages from other view models

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{bool}"/></param>
        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender is SettingsViewModel)
            {
                if (message.PropertyName == nameof(SettingsViewModel.IsDynamicCoverOverlayEnabled))
                {
                    IsDynamicCoverOverlayEnabled = message.NewValue;
                }
                else if (message.PropertyName == nameof(SettingsViewModel.IsCoverOverlayEnabled))
                {
                    IsCoverOverlayEnabled = message.NewValue;
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
            }
            else if (message.Sender is HostWindowViewModel)
            {
                if (message.PropertyName == nameof(HostWindowViewModel.IsDockMode))
                {
                    IsDockMode = message.NewValue;
                }
            }
        }

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{Color}"/></param>
        public void Receive(PropertyChangedMessage<Color> message)
        {
            if (message.Sender is HostWindowViewModel)
            {
                if (message.PropertyName == nameof(HostWindowViewModel.ActivatedWindowAccentColor))
                {
                    _immersiveBgTransition.StartTransition(message.NewValue);
                }
            }
        }

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{double}"/></param>
        public void Receive(PropertyChangedMessage<double> message)
        {
            if (message.Sender is LyricsPageViewModel)
            {
                if (message.PropertyName == nameof(LyricsPageViewModel.LimitedLineWidth))
                {
                    _limitedLineWidthTransition.StartTransition((float)message.NewValue);
                }
            }
        }

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{ElementTheme}"/></param>
        public void Receive(PropertyChangedMessage<ElementTheme> message)
        {
            if (message.Sender is SettingsViewModel)
            {
                if (message.PropertyName == nameof(SettingsViewModel.ThemeType))
                {
                    Theme = message.NewValue;
                }
            }
        }

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{float}"/></param>
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

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{int}"/></param>
        public void Receive(PropertyChangedMessage<int> message)
        {
            if (message.Sender is SettingsViewModel)
            {
                if (message.PropertyName == nameof(SettingsViewModel.CoverImageRadius))
                {
                    CoverImageRadius = message.NewValue;
                }
                else if (message.PropertyName == nameof(SettingsViewModel.CoverOverlayOpacity))
                {
                    CoverOverlayOpacity = message.NewValue;
                }
                else if (message.PropertyName == nameof(SettingsViewModel.CoverOverlayBlurAmount))
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

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{LyricsAlignmentType}"/></param>
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

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{LyricsDisplayType}"/></param>
        public void Receive(PropertyChangedMessage<LyricsDisplayType> message)
        {
            DisplayType = message.NewValue;
        }

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{LyricsFontColorType}"/></param>
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

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{LyricsFontWeight}"/></param>
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

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{LyricsGlowEffectScope}"/></param>
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

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{ObservableCollection{LocalLyricsFolder}}"/></param>
        public void Receive(PropertyChangedMessage<ObservableCollection<LocalLyricsFolder>> message)
        {
            if (message.Sender is SettingsViewModel)
            {
                if (message.PropertyName == nameof(SettingsViewModel.LocalLyricsFolders))
                {
                    // Music lib changed, re-fetch lyrics
                    RefreshLyricsAsync().ConfigureAwait(true);
                }
            }
        }

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{ObservableCollection{LyricsSearchProviderInfo}}"/></param>
        public void Receive(
            PropertyChangedMessage<ObservableCollection<LyricsSearchProviderInfo>> message
        )
        {
            if (message.Sender is SettingsViewModel)
            {
                if (message.PropertyName == nameof(SettingsViewModel.LyricsSearchProvidersInfo))
                {
                    // Lyrics search providers info changed, re-fetch lyrics
                    RefreshLyricsAsync().ConfigureAwait(true);
                }
            }
        }
    }
}
