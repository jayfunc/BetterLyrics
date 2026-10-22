using BetterLyrics.Core.Messages.MediaLibrary;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.ViewModels;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class PlayQueue : BaseUserControl
{
    public PlayQueueViewModel ViewModel => (PlayQueueViewModel)DataContext;

    public PlayQueue()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<PlayQueueViewModel>();
    }

    private void ScrollToPlayingItem()
    {
        if (PlayingQueueListView == null) return;

        var targetItem = ViewModel.SMTCService.TrackPlayingQueue.ElementAtOrDefault(ViewModel.SMTCService.PlaybackState.PlayQueueIndex);

        if (targetItem == null) return;

        PlayingQueueListView.ScrollIntoView(targetItem);
    }

    private void ScrollToPlayingItemButton_Click(object sender, RoutedEventArgs e)
    {
        ScrollToPlayingItem();
    }

    private void PlayingQueueListVireItemGrid_Tapped(object sender, TappedRoutedEventArgs e)
    {
        var item = (PlayQueueItem)((FrameworkElement)sender).DataContext;
        ViewModel.PlayTrackCommand.Execute(item);
    }

    private void RemoveFromPlayingQueueButton_Click(object sender, RoutedEventArgs e)
    {
        var item = (PlayQueueItem)((FrameworkElement)sender).DataContext;
        ViewModel.RemoveFromPlayingQueueCommand.Execute(item);
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        ScrollToPlayingItem();
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        Cleanup();
    }

    private bool _isCleanedUp;

    public new void Cleanup()
    {
        if (_isCleanedUp) return;
        _isCleanedUp = true;

        Bindings?.StopTracking();
        if (PlayingQueueListView != null) PlayingQueueListView.ItemsSource = null;

        DataContext = null;
    }

    private void PlayingQueueListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ScrollToPlayingItem();
    }
}