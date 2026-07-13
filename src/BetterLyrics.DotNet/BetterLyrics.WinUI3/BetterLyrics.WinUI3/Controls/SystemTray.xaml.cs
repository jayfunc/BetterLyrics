using System;
using Windows.UI.ViewManagement;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.ViewModels;
using BetterLyrics.WinUI3.Hooks;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class SystemTray : UserControl, IRecipient<PropertyChangedMessage<bool>>
{
    private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();

    private readonly UISettings _uiSettings;

    public SystemTray()
    {
        InitializeComponent();
        WeakReferenceMessenger.Default.RegisterAll(this);
        DataContext = Ioc.Default.GetRequiredService<SystemTrayViewModel>();

        _uiSettings = new UISettings();
        _uiSettings.ColorValuesChanged += UiSettings_ColorValuesChanged;
    }

    public SystemTrayViewModel ViewModel => (SystemTrayViewModel)DataContext;

    public void Receive(PropertyChangedMessage<bool> message)
    {
        if (message.Sender is SystemTraySettings)
            if (message.PropertyName == nameof(SystemTraySettings.ColorfulSystemTrayIcon))
                UpdateSystemTrayIcon();
    }

    private void UiSettings_ColorValuesChanged(UISettings sender, object args)
    {
        DispatcherQueue.TryEnqueue(() => { UpdateSystemTrayIcon(); });
    }

    private void UpdateSystemTrayIcon()
    {
        if (_settingsService.AppSettings.SystemTraySettings.ColorfulSystemTrayIcon)
        {
            TrayIcon.IconSource = new BitmapImage(new Uri("ms-appx:///Assets/Logo.ico"));
        }
        else
        {
            var currentMode = SystemThemeHook.GetCurrentMode();

            var iconPath = currentMode == ApplicationTheme.Light
                ? "ms-appx:///Assets/LogoBlack.ico"
                : "ms-appx:///Assets/LogoWhite.ico";

            TrayIcon.IconSource = new BitmapImage(new Uri(iconPath));
        }
    }

    private void TrayIcon_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateSystemTrayIcon();
    }
}