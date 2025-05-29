using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using ABI.System;
using Windows.System;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using System.Diagnostics;
using WinRT.Interop;
using Windows.Storage.Pickers;
using BetterLyrics.WinUI3.Models;
using Microsoft.Windows.ApplicationModel.Resources;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page {

        public SettingsViewModel ViewModel => (SettingsViewModel)DataContext;

        public SettingsPage() {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetService<SettingsViewModel>();
        }

        private async void GitHubSettingsCard_Click(object sender, RoutedEventArgs e) {
            var uri = new System.Uri("https://github.com/jayfunc/BetterLyrics");
            await Launcher.LaunchUriAsync(uri);
        }

        private async void AddFolderButton_Click(object sender, RoutedEventArgs e) {
            var picker = new FolderPicker();

            picker.FileTypeFilter.Add("*");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Current.MainWindow);
            InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();

            if (folder != null) {
                string path = folder.Path;
                bool existed = ViewModel.LocalMusicFolders.Where((x) => x.Path == path).Count() > 0;
                if (existed) {
                    MainWindow.StackedNotificationsBehavior?.Show(App.ResourceLoader.GetString("SettingsPagePathExistedInfo"), 3900);
                } else {
                    ViewModel.AddMusicLibrary(path);
                }
            } else {
            }

        }

        private void OpenInFileExplorerButton_Click(object sender, RoutedEventArgs e) {
            Process.Start(new ProcessStartInfo {
                FileName = "explorer.exe",
                Arguments = ((sender as FrameworkElement).DataContext as MusicFolder).Path,
                UseShellExecute = true
            });
        }

        private void RemoveFromAppButton_Click(object sender, RoutedEventArgs e) {
            ViewModel.RemoveMusicLibrary((sender as FrameworkElement).DataContext as MusicFolder);
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            ViewModel.SetTheme();
        }

        private void BackdropComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            ViewModel.SetBackdropType();
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e) {
            var exePath = Process.GetCurrentProcess().MainModule!.FileName!;
            Process.Start(new ProcessStartInfo {
                FileName = exePath,
                UseShellExecute = true
            });

            Application.Current.Exit();
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            ViewModel.SetLanguage();
        }

        private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e) {
            ViewModel.SetCoverOverlayOpacity();
        }

        private void CoverOverlayToggleSwitch_Toggled(object sender, RoutedEventArgs e) {
            ViewModel.SetIsCoverOverlayEnabled((sender as ToggleSwitch).IsOn);
        }

        private void DynamicCoverOverlayToggleSwitch_Toggled(object sender, RoutedEventArgs e) {
            ViewModel.SetDynamicCoverOverlay((sender as ToggleSwitch).IsOn);
        }
    }
}
