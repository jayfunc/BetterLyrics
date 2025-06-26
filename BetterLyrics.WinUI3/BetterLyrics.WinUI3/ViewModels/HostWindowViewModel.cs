// 2025/6/23 by Zhe Fang

using System;
using System.Threading.Tasks;
using BetterInAppLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using WinRT.Interop;
using WinUIEx;
using WinUIEx.Messaging;

namespace BetterLyrics.WinUI3
{
    /// <summary>
    /// Defines the <see cref="HostWindowViewModel" />
    /// </summary>
    public partial class HostWindowViewModel
        : BaseViewModel,
            IRecipient<PropertyChangedMessage<TitleBarType>>,
            IRecipient<PropertyChangedMessage<ElementTheme>>,
            IRecipient<PropertyChangedMessage<BackdropType>>,
            IRecipient<PropertyChangedMessage<int>>
    {
        #region Fields

        /// <summary>
        /// Defines the _watcherHelper
        /// </summary>
        private ForegroundWindowWatcherHelper? _watcherHelper = null;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="HostWindowViewModel"/> class.
        /// </summary>
        /// <param name="settingsService">The settingsService<see cref="ISettingsService"/></param>
        public HostWindowViewModel(ISettingsService settingsService)
            : base(settingsService)
        {
            TitleBarType = _settingsService.TitleBarType;
            ThemeType = _settingsService.ThemeType;
            OnTitleBarTypeChanged(TitleBarType);

            WeakReferenceMessenger.Default.Register<ShowNotificatonMessage>(
                this,
                async (r, m) =>
                {
                    Notification = m.Value;
                    if (
                        !Notification.IsForeverDismissable
                        || AlreadyForeverDismissedThisMessage() == false
                    )
                    {
                        Notification.Visibility = Notification.IsForeverDismissable
                            ? Visibility.Visible
                            : Visibility.Collapsed;
                        ShowInfoBar = true;
                        await Task.Delay(AnimationHelper.StackedNotificationsShowingDuration);
                        ShowInfoBar = false;
                    }
                }
            );
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the ActivatedWindowAccentColor
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial Color ActivatedWindowAccentColor { get; set; }

        /// <summary>
        /// Gets or sets the AppLogoImageIconHeight
        /// </summary>
        [ObservableProperty]
        public partial double AppLogoImageIconHeight { get; set; }

        /// <summary>
        /// Gets or sets the FramePageType
        /// </summary>
        [ObservableProperty]
        public partial Type FramePageType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether IsDockMode
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsDockMode { get; set; } = false;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsDesktopMode { get; set; } = false;

        /// <summary>
        /// Gets or sets the Notification
        /// </summary>
        [ObservableProperty]
        public partial Notification Notification { get; set; } = new();

        /// <summary>
        /// Gets or sets a value indicating whether ShowInfoBar
        /// </summary>
        [ObservableProperty]
        public partial bool ShowInfoBar { get; set; } = false;

        /// <summary>
        /// Gets or sets the ThemeType
        /// </summary>
        [ObservableProperty]
        public partial ElementTheme ThemeType { get; set; }

        /// <summary>
        /// Gets or sets the TitleBarFontSize
        /// </summary>
        [ObservableProperty]
        public partial double TitleBarFontSize { get; set; }

        /// <summary>
        /// Gets or sets the TitleBarHeight
        /// </summary>
        [ObservableProperty]
        public partial double TitleBarHeight { get; set; }

        /// <summary>
        /// Gets or sets the TitleBarType
        /// </summary>
        [ObservableProperty]
        public partial TitleBarType TitleBarType { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{BackdropType}"/></param>
        public void Receive(PropertyChangedMessage<BackdropType> message)
        {
            WindowHelper.GetWindowByFramePageType(FramePageType).SystemBackdrop =
                SystemBackdropHelper.CreateSystemBackdrop(message.NewValue);
        }

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{ElementTheme}"/></param>
        public void Receive(PropertyChangedMessage<ElementTheme> message)
        {
            ThemeType = message.NewValue;
        }

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{int}"/></param>
        public void Receive(PropertyChangedMessage<int> message)
        {
            if (message.Sender is LyricsSettingsControlViewModel)
            {
                if (message.PropertyName == nameof(LyricsSettingsControlViewModel.LyricsFontSize))
                {
                    if (IsDockMode)
                    {
                        DockModeHelper.UpdateAppBarHeight(
                            WindowNative.GetWindowHandle(
                                WindowHelper.GetWindowByFramePageType(FramePageType)
                            ),
                            message.NewValue * 3
                        );
                    }
                }
            }
        }

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{TitleBarType}"/></param>
        public void Receive(PropertyChangedMessage<TitleBarType> message)
        {
            if (message.Sender is SettingsViewModel)
            {
                if (message.PropertyName == nameof(SettingsViewModel.TitleBarType))
                {
                    TitleBarType = message.NewValue;
                }
            }
        }

        /// <summary>
        /// The UpdateAccentColor
        /// </summary>
        /// <param name="hwnd">The hwnd<see cref="nint"/></param>
        public void UpdateAccentColor(nint hwnd)
        {
            ActivatedWindowAccentColor = WindowColorHelper
                .GetDominantColorBelow(hwnd)
                .ToWindowsUIColor();
        }

        /// <summary>
        /// The AlreadyForeverDismissedThisMessage
        /// </summary>
        /// <returns>The <see cref="bool?"/></returns>
        private bool? AlreadyForeverDismissedThisMessage()
        {
            //if (Notification.RelatedSettingsKeyName is string key)
            //    return _settingsService.Get(key, SettingsDefaultValues.NeverShowMessage);
            //return null;
            return null;
        }

        /// <summary>
        /// The StartWatchWindowColorChange
        /// </summary>
        private void StartWatchWindowColorChange()
        {
            var hwnd = WindowNative.GetWindowHandle(
                WindowHelper.GetWindowByFramePageType(FramePageType)
            );
            _watcherHelper = new ForegroundWindowWatcherHelper(
                hwnd,
                onWindowChanged =>
                {
                    UpdateAccentColor(hwnd);
                }
            );
            _watcherHelper.Start();
            UpdateAccentColor(hwnd);
        }

        /// <summary>
        /// The StopWatchWindowColorChange
        /// </summary>
        private void StopWatchWindowColorChange()
        {
            _watcherHelper?.Stop();
            _watcherHelper = null;
        }

        /// <summary>
        /// The ToggleDockMode
        /// </summary>
        [RelayCommand]
        private void ToggleDockMode()
        {
            var window = WindowHelper.GetWindowByFramePageType(FramePageType);

            IsDockMode = !IsDockMode;
            if (IsDockMode)
            {
                DockModeHelper.Enable(window, _settingsService.LyricsFontSize * 3);
                StartWatchWindowColorChange();
            }
            else
            {
                DockModeHelper.Disable(window);
                StopWatchWindowColorChange();
            }
        }

        [RelayCommand]
        private void ToggleDesktopMode()
        {
            var window = WindowHelper.GetWindowByFramePageType(FramePageType);

            IsDesktopMode = !IsDesktopMode;
            if (IsDesktopMode)
            {
                DesktopModeHelper.Enable(window);
                WindowHelper.GetWindowByFramePageType(typeof(LyricsPage)).SystemBackdrop =
                    SystemBackdropHelper.CreateSystemBackdrop(BackdropType.Transparent);
            }
            else
            {
                DesktopModeHelper.Disable(window);
                WindowHelper.GetWindowByFramePageType(typeof(LyricsPage)).SystemBackdrop =
                    SystemBackdropHelper.CreateSystemBackdrop(_settingsService.BackdropType);
            }
        }

        /// <summary>
        /// The OnFramePageTypeChanged
        /// </summary>
        /// <param name="value">The value<see cref="Type"/></param>
        partial void OnFramePageTypeChanged(Type value)
        {
            if (value != null)
            {
                var window = WindowHelper.GetWindowByFramePageType(FramePageType);
                window.SystemBackdrop = SystemBackdropHelper.CreateSystemBackdrop(
                    _settingsService.BackdropType
                );
            }
        }

        /// <summary>
        /// The OnTitleBarTypeChanged
        /// </summary>
        /// <param name="value">The value<see cref="TitleBarType"/></param>
        partial void OnTitleBarTypeChanged(TitleBarType value)
        {
            switch (value)
            {
                case TitleBarType.Compact:
                    AppLogoImageIconHeight = 18;
                    TitleBarFontSize = 11;
                    break;
                case TitleBarType.Extended:
                    AppLogoImageIconHeight = 20;
                    TitleBarFontSize = 14;
                    break;
                default:
                    break;
            }
            TitleBarHeight = value.GetHeight();
        }

        #endregion
    }
}
