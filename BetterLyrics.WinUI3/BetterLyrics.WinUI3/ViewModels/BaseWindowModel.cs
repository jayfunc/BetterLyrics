using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class BaseWindowModel : ObservableObject
    {
        public SettingsService SettingsService { get; private set; }

        [ObservableProperty]
        private int _titleBarFontSize = 11;

        [ObservableProperty]
        private Notification _notification = new();

        [ObservableProperty]
        private bool _showInfoBar = false;

        public BaseWindowModel(SettingsService settingsService)
        {
            SettingsService = settingsService;

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

        [RelayCommand]
        private void SwitchInfoBarNeverShowItAgainCheckBox(bool value)
        {
            switch (Notification.RelatedSettingsKeyName)
            {
                case SettingsKeys.NeverShowEnterFullScreenMessage:
                    SettingsService.NeverShowEnterFullScreenMessage = value;
                    break;
                case SettingsKeys.NeverShowEnterImmersiveModeMessage:
                    SettingsService.NeverShowEnterImmersiveModeMessage = value;
                    break;
                default:
                    break;
            }
        }

        private bool? AlreadyForeverDismissedThisMessage() =>
            Notification.RelatedSettingsKeyName switch
            {
                SettingsKeys.NeverShowEnterFullScreenMessage =>
                    SettingsService.NeverShowEnterFullScreenMessage,
                SettingsKeys.NeverShowEnterImmersiveModeMessage =>
                    SettingsService.NeverShowEnterImmersiveModeMessage,
                _ => null,
            };
    }
}
