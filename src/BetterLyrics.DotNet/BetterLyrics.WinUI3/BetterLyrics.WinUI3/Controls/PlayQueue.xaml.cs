using System.Linq;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class PlayQueue : UserControl, IRecipient<PropertyChangedMessage<int>>
{
    public PlayQueue()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<PlayQueueViewModel>();
        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    public PlayQueueViewModel ViewModel => (PlayQueueViewModel)DataContext;

    public void Receive(PropertyChangedMessage<int> message)
    {
        if (message.Sender is MusicGallerySettings)
            if (message.PropertyName == nameof(MusicGallerySettings.PlayQueueIndex))
                ScrollToPlayingItem();
    }

    private void ScrollToPlayingItem()
    {
        if (PlayingQueueListView == null) return;

        var targetItem = ViewModel.SMTCService.TrackPlayingQueue
            .ElementAtOrDefault(ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex);
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
        ViewModel.SMTCService.PlayTrack(item);
    }

    private void RemoveFromPlayingQueueButton_Click(object sender, RoutedEventArgs e)
    {
        var item = (PlayQueueItem)((FrameworkElement)sender).DataContext;

        ViewModel.SMTCService.TrackPlayingQueue.Remove(item);

        if (ViewModel.SMTCService.TrackPlayingQueue.Count == 0)
        {
            ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex = -1;
            ViewModel.SMTCService.PlayTrackAt(-1); // 停止
        }
    }

    private void EmptyPlayingQueueButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SMTCService.TrackPlayingQueue.Clear(); // Reset
        ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex = -1;
        ViewModel.SMTCService.PlayTrackAt(-1); // 停止
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        ScrollToPlayingItem();
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}