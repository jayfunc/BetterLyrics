using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class SystemTray : UserControl, IRecipient<PropertyChangedMessage<bool>>
    {
        private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
        public SystemTrayViewModel ViewModel => (SystemTrayViewModel)DataContext;

        public SystemTray()
        {
            InitializeComponent();
            WeakReferenceMessenger.Default.RegisterAll(this);
            DataContext = Ioc.Default.GetService<SystemTrayViewModel>();
            SystemThemeHook.ThemeChanged += (mode) =>
            {
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    UpdateSystemTrayIcon();
                });
            };
        }

        private void UpdateSystemTrayIcon()
        {
            if (_settingsService.AppSettings.SystemTraySettings.ColorfulSystemTrayIcon)
            {
                TrayIcon.IconSource = new BitmapImage(new System.Uri("ms-appx:///Assets/Logo.ico"));
            }
            else
            {
                var currentMode = SystemThemeHook.GetCurrentMode();

                string iconPath = currentMode == ApplicationTheme.Light
                    ? "ms-appx:///Assets/LogoBlack.ico"
                    : "ms-appx:///Assets/LogoWhite.ico";

                TrayIcon.IconSource = new BitmapImage(new System.Uri(iconPath));
            }
        }

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender is SystemTraySettings)
            {
                if (message.PropertyName == nameof(SystemTraySettings.ColorfulSystemTrayIcon))
                {
                    UpdateSystemTrayIcon();
                }
            }
        }

        private void TrayIcon_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            UpdateSystemTrayIcon();
        }
    }
}
