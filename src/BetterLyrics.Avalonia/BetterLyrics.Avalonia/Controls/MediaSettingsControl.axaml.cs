using Avalonia.Controls;
using Avalonia.Interactivity;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace BetterLyrics.Avalonia.Controls;

public partial class MediaSettingsControl : UserControl
{
    public MediaSettingsControl()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<MediaSettingsControlViewModel>();
    }

    public MediaSettingsControlViewModel ViewModel => (MediaSettingsControlViewModel)DataContext!;

    private void SettingsPageRemovePathButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: MediaFolder folder })
        {
            ViewModel.RemoveFolder(folder);
        }
    }

    private void ForceSyncButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: MediaFolder folder })
        {
            ViewModel.SyncFolder(folder, true);
        }
    }

    private void SyncNowButton_Click(object? sender, RoutedEventArgs e)
    {
        // 在 Avalonia (以及 FluentAvalonia) 中，SplitButton 的点击事件参数是标准的 RoutedEventArgs
        if (sender is Control { DataContext: MediaFolder folder })
        {
            ViewModel.SyncFolder(folder, false);
        }
    }
}