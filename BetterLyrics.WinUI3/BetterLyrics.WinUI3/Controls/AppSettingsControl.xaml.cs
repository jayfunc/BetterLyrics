using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class AppSettingsControl : UserControl
    {
        public AppSettingsControlViewModel ViewModel => (AppSettingsControlViewModel)DataContext;

        public AppSettingsControl()
        {
            InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<AppSettingsControlViewModel>();
        }

        private async void AutoStartupToggleSwitch_Loaded(object sender, RoutedEventArgs e)
        {
            AutoStartupToggleSwitch.IsOn = await ViewModel.DetectIsAutoStartupEnabledAsync();
            AutoStartupToggleSwitch.Toggled += AutoStartupToggleSwitch_Toggled;
        }

        private void AutoStartupToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            ViewModel.ToggleAutoStartupAsync(AutoStartupToggleSwitch.IsOn);
        }

        private void AutoStartupToggleSwitch_Unloaded(object sender, RoutedEventArgs e)
        {
            AutoStartupToggleSwitch.Toggled -= AutoStartupToggleSwitch_Toggled;
        }

        private void DeleteWindowBoundsRecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                var data = button.DataContext as WindowBoundsRecord;
                if (data != null)
                {
                    ViewModel.AppSettings.WindowBoundsRecords.Remove(data);
                }
            }
        }

        private void ApplyWindowBoundsRecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                var data = button.DataContext as WindowBoundsRecord;
                if (data != null)
                {
                    var lyricsWindow = WindowHelper.GetWindowByWindowType<LyricsWindow>();
                    if (lyricsWindow != null)
                    {
                        lyricsWindow.AppWindow.MoveAndResize(data.WindowBounds.ToRectInt32());
                    }
                }
            }
        }
    }
}
