using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.SettingsService;
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
using NTextCat.Commons;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using static Vanara.PInvoke.ComCtl32;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class LyricsWindowSettingsControl : UserControl
    {
        public LyricsWindowSettingsControlViewModel ViewModel => (LyricsWindowSettingsControlViewModel)DataContext;

        private ISettingsService _settingsService;

        public LyricsWindowSettingsControl()
        {
            InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<LyricsWindowSettingsControlViewModel>();
            _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
        }

        private void DeleteMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuFlyoutItem)
            {
                var data = menuFlyoutItem.DataContext as LyricsWindowStatus;
                if (data != null)
                {
                    ViewModel.AppSettings.WindowBoundsRecords.Remove(data);
                }
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel?.ListViewSelectedItemTag = ((sender as ListView)!.SelectedItem as ListViewItem)!.Tag;
        }

        private void SetDefaultMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuFlyoutItem)
            {
                var data = menuFlyoutItem.DataContext as LyricsWindowStatus;
                if (data != null)
                {
                    ViewModel.AppSettings.WindowBoundsRecords.ForEach(x => x.IsDefault = false);
                    data.IsDefault = true;
                }
            }
        }

        private void StackPanel_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is StackPanel stackPanel)
            {
                if (stackPanel.DataContext is MenuBarItemFlyout menuBarItemFlyout)
                {
                    menuBarItemFlyout.ShowAt(stackPanel);
                }
            }
        }
    }
}
