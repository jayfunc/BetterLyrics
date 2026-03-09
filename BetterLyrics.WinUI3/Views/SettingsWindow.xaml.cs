using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models.Settings;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace BetterLyrics.WinUI3.Views
{
    public sealed partial class SettingsWindow : Window,
        IRecipient<PropertyChangedMessage<ElementTheme>>
    {
        public SettingsWindow()
        {
            InitializeComponent();

            WeakReferenceMessenger.Default.RegisterAll(this);

            this.Init("SettingsPageTitle");
            this.SyncTheme();

            AppWindow.Closing += AppWindow_Closing;
        }

        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            this.CloseWindow();
        }

        private void MusicGalleryButton_Click(object sender, RoutedEventArgs e)
        {
            WindowHook.OpenOrShowWindow<MusicGalleryWindow>();
        }

        private void LyricsWindowSwitchButton_Click(object sender, RoutedEventArgs e)
        {
            WindowHook.OpenOrShowWindow<LyricsWindowSwitchWindow>();
        }

        public void Receive(PropertyChangedMessage<ElementTheme> message)
        {
            if (message.Sender is GeneralSettings)
            {
                if (message.PropertyName == nameof(GeneralSettings.AppTheme))
                {
                    this.SyncTheme();
                }
            }
        }

    }
}
