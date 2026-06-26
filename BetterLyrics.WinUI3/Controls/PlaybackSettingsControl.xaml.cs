using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Helpers.Lyrics;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class PlaybackSettingsControl : UserControl
    {
        public PlaybackSettingsControlViewModel ViewModel => (PlaybackSettingsControlViewModel)DataContext;

        public bool HideConfigPanelWhenLoaded { get; set; } = true;

        public PlaybackSettingsControl()
        {
            InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<PlaybackSettingsControlViewModel>();
        }

        private void AlbumArtSearchProvidersListView_DragItemsCompleted(ListViewBase sender,
            DragItemsCompletedEventArgs args)
        {
            // �� AlbumArtSearchProvidersInfo ���� CollectionChanged �¼�
            ViewModel.SelectedMediaSourceProvider?.AlbumArtSearchProvidersInfo?.Refresh();
        }

        private void LyricsSearchProvidersListView_DragItemsCompleted(ListViewBase sender,
            DragItemsCompletedEventArgs args)
        {
            // �� LyricsSearchProvidersInfo ���� CollectionChanged �¼�
            ViewModel.SelectedMediaSourceProvider?.LyricsSearchProvidersInfo?.Refresh();
        }

        private void MediaSourceProvidersListView_DragItemsCompleted(ListViewBase sender,
            DragItemsCompletedEventArgs args)
        {
            // �� MediaSourceProvidersInfo ���� CollectionChanged �¼�
            ViewModel.AppSettings.MediaSourceProvidersInfo?.Refresh();
        }

        private void ConfigButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            ShowConfigPanel((MediaSourceProviderInfo)((Button)sender).DataContext);
        }

        private void ShowConfigPanel(MediaSourceProviderInfo? info)
        {
            if (info == null) return;

            ViewModel.SelectedMediaSourceProvider = info;
            PlaybackConfigPanel.Show();
        }

        public void ShowCurrentConfigPanel()
        {
            ShowConfigPanel(ViewModel.GSMTCService.CurrentMediaSourceProviderInfo);
        }

        private void DeleteButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var data = (MediaSourceProviderInfo)((Button)sender).DataContext;
            ViewModel.AppSettings.MediaSourceProvidersInfo.Remove(data);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (HideConfigPanelWhenLoaded)
            {
                PlaybackConfigPanel.Hide();
            }
        }

        private async void SaveLyrics(LyricsFormat lyricsFormat)
        {
            var lyricsSearchResult = ViewModel.GSMTCService.CurrentLyricsSearchResult;
            if (lyricsSearchResult == null) return;

            var contentToWrite = LyricsConverter.Convert(
                ViewModel.GSMTCService.CurrentLyricsData,
                lyricsSearchResult!.Title,
                lyricsSearchResult!.Artist,
                lyricsSearchResult!.Album,
                lyricsSearchResult!.Duration,
                ViewModel.AppSettings.LyricsSaveConfig,
                lyricsFormat);

            if (contentToWrite == null) return;

            var ext = lyricsFormat.ToFileExtension();
            var safeTitle = FileHelper.SanitizeFileName($"{lyricsSearchResult.Artist} - {lyricsSearchResult.Title}");
            var fileName = $"{safeTitle}{ext}";

            var folderPath = ViewModel.AppSettings.LyricsSaveConfig.SaveLocation;

            try
            {
                var folder = await StorageFolder.GetFolderFromPathAsync(folderPath);
                var storageFile = await folder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
                await FileIO.WriteTextAsync(storageFile, contentToWrite);

                GlobalToastManager.Show("ActionCompleted", storageFile.Path, MessageSeverity.Success);
            }
            catch (Exception ex)
            {
                GlobalToastManager.Show("Error", ex.Message, MessageSeverity.Error);
            }
        }

        private async void BrowseLyricsSaveLocationButton_Click(object sender, RoutedEventArgs e)
        {
            var folder = await PickerHelper.PickSingleFolderAsync<SettingsWindow>();
            if (folder == null) return;

            ViewModel.AppSettings.LyricsSaveConfig.SaveLocation = folder.Path;
        }

        private void SaveLyricsAsLrcMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            SaveLyrics(LyricsFormat.Lrc);
        }

        private void SaveLyricsAsTtmlMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            SaveLyrics(LyricsFormat.Ttml);
        }

        private void CloseConfigPanelButton_Click(object sender, RoutedEventArgs e)
        {
            PlaybackConfigPanel.Hide();
        }
    }
}