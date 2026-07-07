using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Helpers.Lyrics;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using System;

namespace BetterLyrics.Avalonia.Controls;

public partial class PlaybackSettingsControl : UserControl
{
    private readonly IGlobalToastProvider _globalToastProvider =
        Ioc.Default.GetRequiredService<IGlobalToastProvider>();

    private readonly IFilePickerProvider _filePickerProvider =
        Ioc.Default.GetRequiredService<IFilePickerProvider>();

    public PlaybackSettingsControl()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<PlaybackSettingsControlViewModel>();
    }

    public PlaybackSettingsControlViewModel ViewModel => (PlaybackSettingsControlViewModel)DataContext!;

    public bool HideConfigPanelWhenLoaded { get; set; } = true;

    // 注意：Avalonia 原生 ListBox 没有内置类似于 WinUI 的 DragItemsCompleted 事件。
    // 如果你使用了支持拖放的扩展库（如 Avalonia.Xaml.Behaviors），请将事件参数替换为相应的 Avalonia 拖放事件参数。
    // private void MediaSourceProvidersListView_DragItemsCompleted(object? sender, EventArgs args)
    // {
    //     ViewModel.AppSettings.MediaSourceProvidersInfo?.Refresh();
    // }

    private void ConfigButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: MediaSourceProviderInfo info })
        {
            ShowConfigPanel(info);
        }
    }

    private void ShowConfigPanel(MediaSourceProviderInfo? info)
    {
        if (info == null) return;

        ViewModel.SelectedMediaSourceProvider = info;

        // 假设 local:FloatSidePanel 有对应的 Show() 方法实现
        PlaybackConfigPanel.Show();
    }

    public void ShowCurrentConfigPanel()
    {
        ShowConfigPanel(ViewModel.GsmtcService.CurrentMediaSourceProviderInfo);
    }

    private void DeleteButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: MediaSourceProviderInfo data })
        {
            ViewModel.AppSettings.MediaSourceProvidersInfo.Remove(data);
        }
    }

    private void UserControl_Loaded(object? sender, RoutedEventArgs e)
    {
        if (HideConfigPanelWhenLoaded) PlaybackConfigPanel.Hide();
    }

    private async void SaveLyrics(LyricsFormat lyricsFormat)
    {
        var lyricsSearchResult = ViewModel.GsmtcService.CurrentLyricsSearchResult;
        if (lyricsSearchResult == null) return;

        var contentToWrite = LyricsConverter.Convert(
            ViewModel.GsmtcService.CurrentLyricsData,
            lyricsSearchResult.Title,
            lyricsSearchResult.Artist,
            lyricsSearchResult.Album,
            lyricsSearchResult.Duration,
            ViewModel.AppSettings.LyricsSaveConfig,
            lyricsFormat);

        if (contentToWrite == null) return;

        var ext = lyricsFormat.ToFileExtension();
        var safeTitle = FileHelper.SanitizeFileName($"{lyricsSearchResult.Artist} - {lyricsSearchResult.Title}");
        var fileName = $"{safeTitle}{ext}";
        var folderPath = ViewModel.AppSettings.LyricsSaveConfig.SaveLocation;

        try
        {
            // TODO
            // ⚠️ 警告: WinRT 的 StorageFolder API 在标准 Avalonia 跨平台下无法直接运行。
            // 如果你仅针对 Windows 编译，可以通过引入 Microsoft.Windows.SDK.Contracts 正常使用。
            // 对于跨平台(Mac/Linux)，建议替换为 System.IO.File.WriteAllTextAsync 等标准实现。
            //var folder = await StorageFolder.GetFolderFromPathAsync(folderPath);
            //var storageFile = await folder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
            //await FileIO.WriteTextAsync(storageFile, contentToWrite);

            //_globalToastProvider.Show("ActionCompleted", storageFile.Path, MessageSeverity.Success);
        }
        catch (Exception ex)
        {
            _globalToastProvider.Show("Error", ex.Message, MessageSeverity.Error);
        }
    }

    private async void BrowseLyricsSaveLocationButton_Click(object? sender, RoutedEventArgs e)
    {
        var (_, folderPath) = await _filePickerProvider.PickSingleFolderAsync(WindowType.SettingsWindow);
        if (folderPath == null) return;

        ViewModel.AppSettings.LyricsSaveConfig.SaveLocation = folderPath;
    }

    private void SaveLyricsAsLrcMenuFlyoutItem_Click(object? sender, RoutedEventArgs e)
    {
        SaveLyrics(LyricsFormat.Lrc);
    }

    private void SaveLyricsAsTtmlMenuFlyoutItem_Click(object? sender, RoutedEventArgs e)
    {
        SaveLyrics(LyricsFormat.Ttml);
    }

    private void CloseConfigPanelButton_Click(object? sender, RoutedEventArgs e)
    {
        PlaybackConfigPanel.Hide();
    }
}