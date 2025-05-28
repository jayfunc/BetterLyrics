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

            App.Current.ThemeService.SetBackdropComboBoxDefaultItem(BackdropComboBox);
            App.Current.ThemeService.SetThemeComboBoxDefaultItem(ThemeComboBox);
        }

        private async void GitHubSettingsCard_Click(object sender, RoutedEventArgs e) {
            var uri = new System.Uri("https://github.com/jayfunc/BetterLyrics/tree/master/BetterLyrics.WinUI3");
            await Launcher.LaunchUriAsync(uri);
        }

        private async void AddFolderButton_Click(object sender, RoutedEventArgs e) {
            var picker = new FolderPicker();

            picker.FileTypeFilter.Add("*");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Current.Window);
            InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();

            if (folder != null) {
                string path = folder.Path;
                ViewModel.AddMusicLibrary(path);
            } else {
            }

        }

        private void OpenInFileExplorerButton_Click(object sender, RoutedEventArgs e) {
            Process.Start(new ProcessStartInfo {
                FileName = "explorer.exe",
                Arguments = (sender as HyperlinkButton).Tag.ToString(),
                UseShellExecute = true
            });
        }

        private void RemoveFromAppButton_Click(object sender, RoutedEventArgs e) {
            ViewModel.RemoveMusicLibrary((sender as HyperlinkButton).Tag.ToString());
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            App.Current.ThemeService.OnThemeComboBoxSelectionChanged(sender);
        }

        private void BackdropComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            App.Current.ThemeService.OnBackdropComboBoxSelectionChanged(sender);
        }
    }
}
