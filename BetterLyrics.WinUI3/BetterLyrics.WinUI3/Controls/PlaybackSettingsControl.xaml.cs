using BetterLyrics.WinUI3.ViewModels;
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
    public sealed partial class PlaybackSettingsControl : UserControl
    {
        public PlaybackSettingsControlViewModel ViewModel => (PlaybackSettingsControlViewModel)DataContext;

        public PlaybackSettingsControl()
        {
            InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<PlaybackSettingsControlViewModel>();
        }

        private void AlbumArtSearchProvidersListView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            // 让 AlbumArtSearchProvidersInfo 触发 CollectionChanged 事件
            ViewModel.SelectedMediaSourceProvider?.AlbumArtSearchProvidersInfo?.Refresh();
        }

        private void LyricsSearchProvidersListView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            // 让 LyricsSearchProvidersInfo 触发 CollectionChanged 事件
            ViewModel.SelectedMediaSourceProvider?.LyricsSearchProvidersInfo?.Refresh();
        }

        private void MediaSourceProvidersListView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            // 让 MediaSourceProvidersInfo 触发 CollectionChanged 事件
            ViewModel.AppSettings.MediaSourceProvidersInfo?.Refresh();
        }
    }
}
