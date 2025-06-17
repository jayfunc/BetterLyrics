using System;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.Settings;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace BetterLyrics.WinUI3.ViewModels
{
    public abstract partial class BaseWindowViewModel
        : BaseViewModel,
            IRecipient<PropertyChangedMessage<Models.TitleBarType>>,
            IRecipient<PropertyChangedMessage<ElementTheme>>,
            IRecipient<PropertyChangedMessage<Models.BackdropType>>
    {
        public abstract Models.BackdropType BackdropType { get; set; }

        public abstract Models.TitleBarType TitleBarType { get; set; }

        [ObservableProperty]
        public partial ElementTheme ThemeType { get; set; }

        public BaseWindowViewModel(ISettingsService settingsService)
            : base(settingsService)
        {
            BackdropType = _settingsService.BackdropType;
            TitleBarType = _settingsService.TitleBarType;
            ThemeType = _settingsService.ThemeType;
        }

        public void Receive(PropertyChangedMessage<Models.TitleBarType> message)
        {
            if (message.Sender is SettingsViewModel)
            {
                if (message.PropertyName == nameof(SettingsViewModel.TitleBarType))
                {
                    TitleBarType = message.NewValue;
                }
            }
        }

        public void Receive(PropertyChangedMessage<Models.BackdropType> message)
        {
            if (message.Sender is SettingsViewModel)
            {
                if (message.PropertyName == nameof(SettingsViewModel.BackdropType))
                {
                    BackdropType = message.NewValue;
                }
            }
        }

        public void Receive(PropertyChangedMessage<ElementTheme> message)
        {
            ThemeType = message.NewValue;
        }
    }
}
