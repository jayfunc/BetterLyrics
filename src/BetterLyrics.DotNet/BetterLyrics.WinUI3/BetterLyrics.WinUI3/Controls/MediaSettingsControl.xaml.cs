using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class MediaSettingsControl : UserControl
{
    public MediaSettingsControl()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<MediaSettingsControlViewModel>();
    }

    public MediaSettingsControlViewModel ViewModel => (MediaSettingsControlViewModel)DataContext;

    private void SettingsPageRemovePathButton_Click(object sender, RoutedEventArgs e)
    {
        var folder = (MediaFolder)((FrameworkElement)sender).DataContext;
        ViewModel.RemoveFolder(folder);
    }

    private void ForceSyncButton_Click(object sender, RoutedEventArgs e)
    {
        var folder = (MediaFolder)((FrameworkElement)sender).DataContext;
        ViewModel.SyncFolder(folder, true);
    }

    private void SyncNowButton_Click(SplitButton sender, SplitButtonClickEventArgs args)
    {
        var folder = (MediaFolder)sender.DataContext;
        ViewModel.SyncFolder(folder, false);
    }
}