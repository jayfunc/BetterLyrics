using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class GlobalViewModel : BaseSettingsViewModel
    {
        private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        public bool IsFirstRun
        {
            get => Get(SettingsKeys.IsFirstRun, SettingsDefaultValues.IsFirstRun);
            set => Set(SettingsKeys.IsFirstRun, value);
        }

        public ElementTheme Theme
        {
            get => (ElementTheme)Get(SettingsKeys.ThemeType, SettingsDefaultValues.ThemeType);
            set
            {
                Set(SettingsKeys.ThemeType, (int)value);
                WeakReferenceMessenger.Default.Send(new ThemeChangedMessage(value));
            }
        }

        public BackdropType BackdropType
        {
            get => (BackdropType)Get(SettingsKeys.BackdropType, SettingsDefaultValues.BackdropType);
            set
            {
                Set(SettingsKeys.BackdropType, (int)value);
                WeakReferenceMessenger.Default.Send(new SystemBackdropChangedMessage(value));
            }
        }

        public TitleBarType TitleBarType
        {
            get => (TitleBarType)Get(SettingsKeys.TitleBarType, SettingsDefaultValues.TitleBarType);
            set
            {
                Set(SettingsKeys.TitleBarType, (int)value);
                WeakReferenceMessenger.Default.Send(new TitleBarTypeChangedMessage(value));
            }
        }

        [ObservableProperty]
        private DisplayType _displayType = DisplayType.PlaceholderOnly;

        [ObservableProperty]
        private bool _isPlaying = false;

        public GlobalViewModel(ISettingsService settingsService)
            : base(settingsService)
        {
            WeakReferenceMessenger.Default.Register<GlobalViewModel, DisplayTypeChangedMessage>(
                this,
                (r, m) =>
                {
                    DisplayType = m.Value;
                }
            );

            WeakReferenceMessenger.Default.Register<GlobalViewModel, PlayingStatusChangedMessage>(
                this,
                (r, m) =>
                {
                    _dispatcherQueue.TryEnqueue(
                        DispatcherQueuePriority.High,
                        () => IsPlaying = m.Value
                    );
                }
            );
        }
    }
}
