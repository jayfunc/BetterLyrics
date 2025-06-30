// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using WinUIEx.Messaging;

namespace BetterLyrics.WinUI3.Views
{
    public sealed partial class LyricsPage : Page
    {
        public LyricsPage()
        {
            this.InitializeComponent();

            DataContext = Ioc.Default.GetService<LyricsPageViewModel>();

            WeakReferenceMessenger.Default.Register<PropertyChangedMessage<LyricsDisplayType>>(
                this,
                async (r, m) =>
                {
                    if (m.Sender is LyricsPageViewModel)
                    {
                        if (m.PropertyName == nameof(LyricsPageViewModel.DisplayType))
                        {
                            switch (m.NewValue)
                            {
                                case LyricsDisplayType.AlbumArtOnly:
                                    await SwitchToAlbumArtOnlyDisplayTypeAsync();
                                    break;
                                case LyricsDisplayType.LyricsOnly:
                                    await SwitchToLyricsOnlyDisplayTypeAsync();
                                    break;
                                case LyricsDisplayType.SplitView:
                                    await SwitchToSplitViewDisplayTypeAsync();
                                    break;
                                case LyricsDisplayType.PlaceholderOnly:
                                    await SwitchToPlaceholderOnlyDisplayTypeAsync();
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            );
        }

        public LyricsPageViewModel ViewModel => (LyricsPageViewModel)DataContext;

        private void BottomCommandGrid_PointerEntered(
            object sender,
            Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e
        )
        {
            if (BottomCommandGrid.Opacity == 0)
                BottomCommandGrid.Opacity = .5;
        }

        private void BottomCommandGrid_PointerExited(
            object sender,
            Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e
        )
        {
            if (BottomCommandGrid.Opacity == .5)
                BottomCommandGrid.Opacity = 0;
        }

        private void CoverArea_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CoverImageGrid.Width = CoverImageGrid.Height = Math.Min(
                CoverArea.ActualWidth,
                CoverArea.ActualHeight
            );
        }

        private void CoverImageGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ViewModel.CoverImageGridActualHeight = e.NewSize.Height;
        }

        private void LyricsPlaceholderGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ViewModel.MaxLyricsWidth = e.NewSize.Width;
        }

        private void WelcomeTeachingTip_Closed(TeachingTip sender, TeachingTipClosedEventArgs args)
        {
            ViewModel.IsFirstRun = false;
        }

        private async void LyricsOnlyRadioButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.PreferredDisplayType = ViewModel.DisplayType = LyricsDisplayType.LyricsOnly;
            await SwitchToLyricsOnlyDisplayTypeAsync();
        }

        private async void AlbumArtOnlyRadioButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.PreferredDisplayType = ViewModel.DisplayType = LyricsDisplayType.AlbumArtOnly;
            await SwitchToAlbumArtOnlyDisplayTypeAsync();
        }

        private async void SplitViewRadioButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.PreferredDisplayType = ViewModel.DisplayType = LyricsDisplayType.SplitView;
            await SwitchToSplitViewDisplayTypeAsync();
        }

        private async Task SwitchToLyricsOnlyDisplayTypeAsync()
        {
            await BeforeSwitchDisplayTypeAsync();

            Grid.SetColumn(LyricsPlaceholderGrid, 0);
            Grid.SetColumnSpan(LyricsPlaceholderGrid, 3);

            LyricsPlaceholderGrid.Opacity = 1;
            LyricsGrid.Opacity = 1;
        }


        private async Task SwitchToAlbumArtOnlyDisplayTypeAsync()
        {
            await BeforeSwitchDisplayTypeAsync();

            Grid.SetColumn(SongInfoInnerGrid, 0);
            Grid.SetColumnSpan(SongInfoInnerGrid, 3);

            SongInfoInnerGrid.Opacity = 1;
            LyricsGrid.Opacity = 1;
        }


        private async Task BeforeSwitchDisplayTypeAsync()
        {
            SongInfoInnerGrid.Opacity = 0;
            LyricsPlaceholderGrid.Opacity = 0;
            //LyricsGrid.Opacity = 0;

            await Task.Delay(300);
        }

        private async Task SwitchToSplitViewDisplayTypeAsync()
        {
            await BeforeSwitchDisplayTypeAsync();

            Grid.SetColumn(SongInfoInnerGrid, 0);
            Grid.SetColumnSpan(SongInfoInnerGrid, 1);

            Grid.SetColumn(LyricsPlaceholderGrid, 2);
            Grid.SetColumnSpan(LyricsPlaceholderGrid, 1);

            SongInfoInnerGrid.Opacity = 1;
            LyricsPlaceholderGrid.Opacity = 1;
            LyricsGrid.Opacity = 1;
        }

        private async Task SwitchToPlaceholderOnlyDisplayTypeAsync()
        {
            await BeforeSwitchDisplayTypeAsync();
        }
    }
}
