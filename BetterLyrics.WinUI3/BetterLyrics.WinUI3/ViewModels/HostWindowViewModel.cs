using System.Threading.Tasks;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.Settings;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml;

namespace BetterLyrics.WinUI3
{
    public partial class HostWindowViewModel
        : BaseWindowViewModel,
            IRecipient<PropertyChangedMessage<bool>>
    {
        [ObservableProperty]
        public partial double AppLogoImageIconHeight { get; set; }

        [ObservableProperty]
        public partial double TitleBarFontSize { get; set; }

        [ObservableProperty]
        public partial double TitleBarHeight { get; set; }

        [ObservableProperty]
        public partial Notification Notification { get; set; } = new();

        [ObservableProperty]
        public partial bool ShowInfoBar { get; set; } = false;

        [ObservableProperty]
        public override partial BackdropType BackdropType { get; set; }

        [ObservableProperty]
        public override partial TitleBarType TitleBarType { get; set; }

        [ObservableProperty]
        public partial bool IsImmersiveMode { get; set; }

        [ObservableProperty]
        public partial int TopCommandGridOpacity { get; set; } = 1;

        public HostWindowViewModel(ISettingsService settingsService)
            : base(settingsService)
        {
            TitleBarType = _settingsService.TitleBarType;
            OnTitleBarTypeChanged(TitleBarType);
            BackdropType = _settingsService.BackdropType;

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

        partial void OnIsImmersiveModeChanged(bool value)
        {
            if (value)
            {
                TopCommandGridOpacity = 0;
            }
            else
            {
                TopCommandGridOpacity = 1;
            }
        }

        partial void OnBackdropTypeChanged(BackdropType value) { }

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

        [RelayCommand]
        private void SwitchInfoBarNeverShowItAgainCheckBox(bool value)
        {
            //if (Notification.RelatedSettingsKeyName is string key)
            //    _settingsService.SetValue(key, value);
        }

        private bool? AlreadyForeverDismissedThisMessage()
        {
            //if (Notification.RelatedSettingsKeyName is string key)
            //    return _settingsService.Get(key, SettingsDefaultValues.NeverShowMessage);
            //return null;
            return null;
        }

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender is InAppLyricsPageViewModel)
            {
                if (message.PropertyName == nameof(InAppLyricsPageViewModel.IsImmersiveMode))
                {
                    IsImmersiveMode = message.NewValue;
                }
            }
        }
    }
}
