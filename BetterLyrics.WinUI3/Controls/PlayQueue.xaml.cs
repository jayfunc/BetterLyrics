using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class PlayQueue : UserControl, IRecipient<PropertyChangedMessage<int>>
    {
        public PlayQueueViewModel ViewModel => (PlayQueueViewModel)DataContext;
        public PlayQueue()
        {
            InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<PlayQueueViewModel>();
            WeakReferenceMessenger.Default.RegisterAll(this);
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

        private async void PlayingQueueListVireItemGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var item = (PlayQueueItem)((FrameworkElement)sender).DataContext;
            await ViewModel.SMTCService.PlayTrackAsync(item);
        }

        private async void RemoveFromPlayingQueueButton_Click(object sender, RoutedEventArgs e)
        {
            bool playNext = false;
            var item = (PlayQueueItem)((FrameworkElement)sender).DataContext;
            int index = ViewModel.SMTCService.TrackPlayingQueue.IndexOf(item);
            if (item == PlayingQueueListView.SelectedItem)
            {
                playNext = true;
            }
            ViewModel.SMTCService.TrackPlayingQueue.Remove(item);
            if (playNext)
            {
                if (ViewModel.SMTCService.TrackPlayingQueue.Count == 0)
                {
                    index = -1;
                }
                else if (index >= ViewModel.SMTCService.TrackPlayingQueue.Count)
                {
                    index = ViewModel.SMTCService.TrackPlayingQueue.Count - 1;
                }
                ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex = index;
                await ViewModel.SMTCService.PlayTrackAtAsync(ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex);
            }
        }

        private async void EmptyPlayingQueueButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SMTCService.TrackPlayingQueue.Clear();
            ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex = -1;
            await ViewModel.SMTCService.PlayTrackAtAsync(ViewModel.AppSettings.MusicGallerySettings.PlayQueueIndex);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ScrollToPlayingItem();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }

        public void Receive(PropertyChangedMessage<int> message)
        {
            if (message.Sender is MusicGallerySettings)
            {
                if (message.PropertyName == nameof(MusicGallerySettings.PlayQueueIndex))
                {
                    ScrollToPlayingItem();
                }
            }
        }

    }
}
