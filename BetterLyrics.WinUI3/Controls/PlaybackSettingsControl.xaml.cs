using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Helper.Lyrics;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using Windows.Storage;

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

        private void ConfigButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            ViewModel.SelectedMediaSourceProvider = (MediaSourceProviderInfo)((Button)sender).DataContext;
            ViewModel.OpenConfigPanel();
        }

        private void DeleteButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var data = (MediaSourceProviderInfo)((Button)sender).DataContext;
            ViewModel.AppSettings.MediaSourceProvidersInfo.Remove(data);
        }

        private void PlaybackListGrid_SizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
        {
            ViewModel.PlaybackListGridHeight = e.NewSize.Height;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.CloseConfigPanelCommand.Execute(null);
        }

        private async void SaveLyricsButton_Click(SplitButton sender, SplitButtonClickEventArgs args)
        {
            var window = WindowHook.GetWindow<SettingsWindow>();
            if (window == null) return;

            var lyricsSearchResult = ViewModel.GSMTCService.CurrentLyricsSearchResult;
            if (lyricsSearchResult == null) return;

            var contentToWrite = LyricsConverter.Convert(
                ViewModel.GSMTCService.CurrentLyricsData,
                lyricsSearchResult!.Title,
                lyricsSearchResult!.Artist,
                lyricsSearchResult!.Album,
                lyricsSearchResult!.Duration,
                ViewModel.AppSettings.LyricsSaveConfig);
            if (contentToWrite == null) return;

            var ext = LyricsFormat.Lrc.ToFileExtension();

            IDictionary<string, IList<string>> fileTypeChoices = new Dictionary<string, IList<string>>()
            {
                { ext.ToUpper(), new List<string>() { ext } },
            };

            var suggestedFileName = $"{lyricsSearchResult.Artist} - {lyricsSearchResult.Title}";

            var file = await PickerHelper.PickSaveFileAsync(window, fileTypeChoices, suggestedFileName);

            if (file != null)
            {
                await FileIO.WriteTextAsync(file, contentToWrite);
                GlobalToastManager.Show("ActionCompleted", null, InfoBarSeverity.Success);
            }
        }
    }
}
