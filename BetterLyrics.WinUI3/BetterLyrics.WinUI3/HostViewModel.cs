using System.Threading.Tasks;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;

namespace BetterLyrics.WinUI3
{
    public partial class HostViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;

        [ObservableProperty]
        private double _appLogoImageIconHeight = 18;

        [ObservableProperty]
        private double _titleBarFontSize = 11;

        [ObservableProperty]
        private double _titleBarHeight = 48;

        [ObservableProperty]
        private Notification _notification = new();

        [ObservableProperty]
        private bool _showInfoBar = false;

        public HostViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;

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

            WeakReferenceMessenger.Default.Register<TitleBarTypeChangedMessage>(
                this,
                (r, m) =>
                {
                    UpdateTitleBarStyle(m.Value);
                }
            );
        }

        public void UpdateTitleBarStyle(TitleBarType titleBarType)
        {
            switch (titleBarType)
            {
                case TitleBarType.Compact:
                    TitleBarHeight = (double)App.Current.Resources["TitleBarCompactHeight"];
                    AppLogoImageIconHeight = 18;
                    TitleBarFontSize = 11;
                    break;
                case TitleBarType.Extended:
                    TitleBarHeight = (double)App.Current.Resources["TitleBarExpandedHeight"];
                    AppLogoImageIconHeight = 20;
                    TitleBarFontSize = 14;
                    break;
                default:
                    break;
            }
        }

        [RelayCommand]
        private void SwitchInfoBarNeverShowItAgainCheckBox(bool value)
        {
            if (Notification.RelatedSettingsKeyName is string key)
                _settingsService.Set(key, value);
        }

        private bool? AlreadyForeverDismissedThisMessage()
        {
            if (Notification.RelatedSettingsKeyName is string key)
                return _settingsService.Get(key, SettingsDefaultValues.NeverShowMessage);
            return null;
        }
    }
}
