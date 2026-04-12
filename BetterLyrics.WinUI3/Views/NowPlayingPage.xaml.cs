// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Collections;
using BetterLyrics.WinUI3.Controls;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Helper.Lyrics;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.GSMTCService;
using BetterLyrics.WinUI3.Services.SongSearchMapService;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI;
using DevWinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace BetterLyrics.WinUI3.Views
{
    public sealed partial class NowPlayingPage : Page,
        IRecipient<PropertyChangedMessage<SongInfo>>,
        IRecipient<PropertyChangedMessage<int>>,
        IRecipient<PropertyChangedMessage<double>>,
        IRecipient<PropertyChangedMessage<bool>>,
        IRecipient<PropertyChangedMessage<string>>,
        IRecipient<PropertyChangedMessage<MappedSongSearchQuery?>>,
        IRecipient<PropertyChangedMessage<NowPlayingPalette>>,
        IRecipient<PropertyChangedMessage<FullyObservableCollection<ComponentPlacement>>>,
        IRecipient<PropertyChangedMessage<ObservableCollection<string>>>
    {
        private readonly IGSMTCService _gsmtcService = Ioc.Default.GetRequiredService<IGSMTCService>();
        private readonly ISongSearchMapService _songSearchMapService = Ioc.Default.GetRequiredService<ISongSearchMapService>();

        private DispatcherQueueTimer? _layoutChangedTimer = App.Current.Resources.DispatcherQueue.CreateTimer();
        private DispatcherQueueTimer? _scrollChangedTimer = App.Current.Resources.DispatcherQueue.CreateTimer();

        public NowPlayingPageViewModel ViewModel => (NowPlayingPageViewModel)DataContext;

        public LyricsWindowStatus? LyricsWindowStatus
        {
            get { return (LyricsWindowStatus?)GetValue(LyricsWindowStatusProperty); }
            set { SetValue(LyricsWindowStatusProperty, value); }
        }

        public static readonly DependencyProperty LyricsWindowStatusProperty =
            DependencyProperty.Register(nameof(LyricsWindowStatus), typeof(LyricsWindowStatus), typeof(NowPlayingPage), new PropertyMetadata(null, OnDependencyPropertyChanged));

        public NowPlayingPage()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<NowPlayingPageViewModel>();
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NowPlayingPage page)
            {
                if (e.Property == LyricsWindowStatusProperty)
                {
                    page.OnLayoutChanged();
                    _ = page.RenderSongInfoAsync();
                }
            }
        }

        private void LayoutProfile_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnLayoutChanged();
        }

        private void RenderTextBlock(TextBlock? sender, string? text, double fontSize)
        {
            if (sender == null || text == null || fontSize == 0 || LyricsWindowStatus == null) return;

            var lyricsStyleSettings = LyricsWindowStatus.LyricsStyleSettings;

            sender.Inlines.Clear();
            foreach (var ch in text)
            {
                var fontFamilyName = LanguageHelper.IsCJK(ch) ? lyricsStyleSettings.LyricsCJKFontFamily : lyricsStyleSettings.LyricsWesternFontFamily;
                sender.Inlines.Add(new Run { Text = $"{ch}", FontFamily = new FontFamily(fontFamilyName) });
            }
            sender.FontSize = (int)fontSize;
            sender.Foreground = new SolidColorBrush(LyricsWindowStatus.WindowPalette.NonCurrentLineFillColor);
        }

        private async Task RenderSongInfoAsync()
        {
            if (LyricsWindowStatus == null) return;

            var lyricsLayoutMetrics = LyricsLayoutHelper.CalculateLayout(RootGrid.ActualWidth, RootGrid.ActualHeight);
            var albumArtLayoutSettings = LyricsWindowStatus.AlbumArtLayoutSettings;

            var titleFontSize = albumArtLayoutSettings.IsAutoSongInfoFontSize ? lyricsLayoutMetrics.SongTitleSize : albumArtLayoutSettings.SongInfoFontSize;
            var artistsFontSize = albumArtLayoutSettings.IsAutoSongInfoFontSize ? lyricsLayoutMetrics.ArtistNameSize : albumArtLayoutSettings.SongInfoFontSize * 0.8;
            var albumFontSize = albumArtLayoutSettings.IsAutoSongInfoFontSize ? lyricsLayoutMetrics.AlbumNameSize : albumArtLayoutSettings.SongInfoFontSize * 0.8;

            (string mappedTitle, string mappedArtist, string mappedAlbum) = await _songSearchMapService.GetMappingAsync(_gsmtcService.CurrentSongInfo);

            LyricsCard.Title = mappedTitle;
            LyricsCard.Artist = mappedArtist;

            RenderTextBlock(TitleTextBlock, mappedTitle, titleFontSize);
            RenderTextBlock(ArtistsTextBlock, mappedArtist, artistsFontSize);
            RenderTextBlock(AlbumTextBlock, mappedAlbum, albumFontSize);
        }

        private async Task RefreshSongInfoAsync()
        {
            SongInfoStackPanel.Opacity = 0;
            await Task.Delay(Constants.Time.AnimationDuration);
            await RenderSongInfoAsync();
            SongInfoStackPanel.Opacity = 1;
        }

        private void ApplyLayoutProfile()
        {
            var profile = LyricsWindowStatus?.LayoutProfile;
            if (profile == null) return;

            DynamicLayoutGrid.RowDefinitions.Clear();
            DynamicLayoutGrid.ColumnDefinitions.Clear();

            foreach (var row in profile.RowDefinitions)
                DynamicLayoutGrid.RowDefinitions.Add(new RowDefinition { Height = ParseGridLength(row) });

            foreach (var col in profile.ColumnDefinitions)
                DynamicLayoutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = ParseGridLength(col) });

            LyricsContainer.Visibility = Visibility.Collapsed;
            AlbumArtGrid.Visibility = Visibility.Collapsed;
            SongInfoStackPanel.Visibility = Visibility.Collapsed;

            foreach (var placement in profile.Placements)
            {
                FrameworkElement? targetElement = placement.ComponentType switch
                {
                    ComponentType.AlbumArt => AlbumArtGrid,
                    ComponentType.Lyrics => LyricsContainer,
                    ComponentType.SongInfo => SongInfoStackPanel,
                    _ => null
                };

                if (targetElement != null)
                {
                    targetElement.Visibility = Visibility.Visible;

                    Grid.SetRow(targetElement, placement.Row);
                    Grid.SetColumn(targetElement, placement.Column);
                    Grid.SetRowSpan(targetElement, placement.RowSpan);
                    Grid.SetColumnSpan(targetElement, placement.ColumnSpan);

                    targetElement.Margin = new Thickness(
                        placement.MarginLeft,
                        placement.MarginTop,
                        placement.MarginRight,
                        placement.MarginBottom);

                    targetElement.HorizontalAlignment = placement.HorizontalAlignment;
                    targetElement.VerticalAlignment = placement.VerticalAlignment;
                }
            }
        }

        private GridLength ParseGridLength(string lengthStr)
        {
            if (string.Equals(lengthStr, "Auto", StringComparison.OrdinalIgnoreCase))
                return new GridLength(1, GridUnitType.Auto);

            if (lengthStr.EndsWith("*") && double.TryParse(lengthStr.TrimEnd('*'), out double starVal))
                return new GridLength(starVal, GridUnitType.Star);

            if (double.TryParse(lengthStr, out double pixelVal))
                return new GridLength(pixelVal, GridUnitType.Pixel);

            return new GridLength(1, GridUnitType.Star);
        }

        private void UpdateLyricsLayout()
        {
            if (RootGrid == null || LyricsContainer == null || LyricsCanvas == null) return;
            if (LyricsWindowStatus == null) return;

            if (!LyricsContainer.IsLoaded || !RootGrid.IsLoaded) return;

            if (LyricsContainer.Visibility == Visibility.Collapsed)
            {
                LyricsCanvas.LyricsOpacity = 0;
            }
            else
            {
                LyricsCanvas.LyricsOpacity = 1;

                var transform = LyricsContainer.TransformToVisual(RootGrid);
                var localRect = new Windows.Foundation.Rect(0, 0, LyricsCanvas.ActualWidth, LyricsCanvas.ActualHeight);
                var relativeRect = transform.TransformBounds(localRect);

                LyricsCanvas.LyricsStartX = relativeRect.X;
                LyricsCanvas.LyricsStartY = relativeRect.Y;
                LyricsCanvas.LyricsWidth = LyricsContainer.ActualWidth;
                LyricsCanvas.LyricsHeight = LyricsContainer.ActualHeight;

                if (LyricsWindowStatus.LyricsEffectSettings.Lyrics3DAutoFitLayout)
                {
                    (LyricsCanvas.LyricsHeight, LyricsCanvas.LyricsWidth) = (LyricsCanvas.LyricsWidth, LyricsCanvas.LyricsHeight);
                    LyricsCanvas.LyricsStartX += (LyricsCanvas.LyricsHeight - LyricsCanvas.LyricsWidth) / 2;
                    LyricsCanvas.LyricsStartY += (LyricsCanvas.LyricsWidth - LyricsCanvas.LyricsHeight) / 2;
                }
            }
        }

        private void UpdateAlbumArtLayout()
        {
            if (RootGrid == null || AlbumArtGrid == null) return;
            if (!AlbumArtGrid.IsLoaded || !RootGrid.IsLoaded) return;

            var transform = AlbumArtGrid.TransformToVisual(RootGrid);
            var localRect = new Windows.Foundation.Rect(0, 0, AlbumArtGrid.ActualWidth, AlbumArtGrid.ActualHeight);
            LyricsCanvas.AlbumArtRect = transform.TransformBounds(localRect);
        }

        private void OnLayoutChanged()
        {
            _layoutChangedTimer?.Debounce(async () =>
            {
                ApplyLayoutProfile();

                // Ensure the layout is updated before calculating positions
                await Task.Delay(100);

                UpdateLyricsLayout();
                UpdateAlbumArtLayout();

            }, TimeSpan.FromMilliseconds(250));
        }

        private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _ = RenderSongInfoAsync();
            OnLayoutChanged();
        }

        private void LyricsContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateLyricsLayout();
        }

        private void AlbumArtGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateAlbumArtLayout();
        }

        private void UpdateAutoScrollViewIsPlaying(AutoScrollView element, bool isPointerEntered)
        {
            if (LyricsWindowStatus?.AlbumArtAreaEffectSettings.SongInfoAutoScroll == true)
            {
                element.IsPlaying = true;
            }
            else
            {
                element.IsPlaying = isPointerEntered;
            }
        }

        private void TitleAutoScrollHoverEffectView_PointerCanceled(object sender, PointerRoutedEventArgs e) => UpdateAutoScrollViewIsPlaying(TitleAutoScrollHoverEffectView, false);
        private void TitleAutoScrollHoverEffectView_PointerEntered(object sender, PointerRoutedEventArgs e) => UpdateAutoScrollViewIsPlaying(TitleAutoScrollHoverEffectView, true);
        private void TitleAutoScrollHoverEffectView_PointerExited(object sender, PointerRoutedEventArgs e) => UpdateAutoScrollViewIsPlaying(TitleAutoScrollHoverEffectView, false);
        private void ArtistsAutoScrollHoverEffectView_PointerCanceled(object sender, PointerRoutedEventArgs e) => UpdateAutoScrollViewIsPlaying(ArtistsAutoScrollHoverEffectView, false);
        private void ArtistsAutoScrollHoverEffectView_PointerEntered(object sender, PointerRoutedEventArgs e) => UpdateAutoScrollViewIsPlaying(ArtistsAutoScrollHoverEffectView, true);
        private void ArtistsAutoScrollHoverEffectView_PointerExited(object sender, PointerRoutedEventArgs e) => UpdateAutoScrollViewIsPlaying(ArtistsAutoScrollHoverEffectView, false);
        private void AlbumAutoScrollHoverEffectView_PointerCanceled(object sender, PointerRoutedEventArgs e) => UpdateAutoScrollViewIsPlaying(AlbumAutoScrollHoverEffectView, false);
        private void AlbumAutoScrollHoverEffectView_PointerEntered(object sender, PointerRoutedEventArgs e) => UpdateAutoScrollViewIsPlaying(AlbumAutoScrollHoverEffectView, true);
        private void AlbumAutoScrollHoverEffectView_PointerExited(object sender, PointerRoutedEventArgs e) => UpdateAutoScrollViewIsPlaying(AlbumAutoScrollHoverEffectView, false);

        private void LyricsScrollViewer_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            LyricsCanvas.IsMouseScrolling = true;

            var pointerPoint = e.GetCurrentPoint(LyricsScrollViewer);
            int mouseWheelDelta = pointerPoint.Properties.MouseWheelDelta;

            var value = LyricsCanvas.MouseScrollOffset + mouseWheelDelta;
            if (value > 0)
            {
                value = Math.Min(-LyricsCanvas.CurrentCanvasYScroll, value);
            }
            else
            {
                value = Math.Max(-LyricsCanvas.CurrentCanvasYScroll - LyricsCanvas.ActualLyricsHeight, value);
            }
            LyricsCanvas.MouseScrollOffset = value;

            _scrollChangedTimer?.Debounce(() =>
            {
                LyricsCanvas.MouseScrollOffset = 0;
                LyricsCanvas.IsMouseScrolling = false;
            }, TimeSpan.FromSeconds(3));
        }

        private void LyricsScrollViewer_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var pointerPoint = e.GetCurrentPoint(LyricsScrollViewer);
            LyricsCanvas.MousePosition = pointerPoint.Position;
        }

        private void LyricsScrollViewer_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            LyricsCanvas.IsMousePressing = false;
            _ = _gsmtcService.ChangeLyricsLineAsync(LyricsCanvas.CurrentHoveringLineIndex);
        }

        private void LyricsScrollViewer_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            LyricsCanvas.IsMouseInLyricsArea = false;
        }

        private void LyricsScrollViewer_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            LyricsCanvas.IsMouseInLyricsArea = true;
        }

        private void LyricsScrollViewer_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            LyricsCanvas.IsMousePressing = true;
        }

        private async void SaveAlbumArtButton_Click(object sender, RoutedEventArgs e)
        {
            var imageBytes = ViewModel.MediaSessionsService.AlbumArtBytes;

            if (imageBytes == null || imageBytes.Length == 0) return;

            var window = WindowHook.GetWindows<NowPlayingWindow>().FirstOrDefault(x => x.LyricsWindowStatus == LyricsWindowStatus);
            if (window == null) return;

            IDictionary<string, IList<string>> fileTypeChoices = new Dictionary<string, IList<string>>()
            {
                { "PNG", new List<string>() { ".png" } },
                { "JPEG", new List<string>() { ".jpg", ".jpeg" } }
            };

            var file = await PickerHelper.PickSaveFileAsync(window, fileTypeChoices);

            if (file != null)
            {
                try
                {
                    await FileIO.WriteBytesAsync(file, imageBytes);
                    GlobalToastManager.Show("ActionCompleted", null, InfoBarSeverity.Success);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SaveAlbumArtButton_Click: {ex}");
                }
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);

            _layoutChangedTimer?.Stop();
            _layoutChangedTimer = null;

            _scrollChangedTimer?.Stop();
            _scrollChangedTimer = null;

            DataContext = null;
        }

        private void RootGrid_DragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties.ContainsKey("LyricsCardStyleKey"))
                e.AcceptedOperation = DataPackageOperation.Copy;
            else
                e.AcceptedOperation = DataPackageOperation.None;
        }

        private void RootGrid_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties.TryGetValue("LyricsCardStyleKey", out object styleKey))
            {
                if (LyricsWindowStatus != null)
                {
                    LyricsWindowStatus.ShowLyricsCard = true;
                    LyricsWindowStatus.LyricsCardStyleKey = (string)styleKey;
                }
            }
        }

        private void DeleteLyricsCardMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (LyricsWindowStatus != null)
            {
                LyricsWindowStatus.ShowLyricsCard = false;
            }
        }

        private void DynamicLayoutGrid_Loaded(object sender, RoutedEventArgs e)
        {
            DynamicLayoutGrid.Padding = new Thickness(
                LyricsWindowStatus?.LayoutProfile.PaddingLeft ?? 0,
                LyricsWindowStatus?.LayoutProfile.PaddingTop ?? 0,
                LyricsWindowStatus?.LayoutProfile.PaddingRight ?? 0,
                LyricsWindowStatus?.LayoutProfile.PaddingBottom ?? 0);
        }

        public void Receive(PropertyChangedMessage<SongInfo> message)
        {
            if (message.Sender is IGSMTCService && message.PropertyName == nameof(IGSMTCService.CurrentSongInfo))
            {
                _ = RefreshSongInfoAsync();
                UpdateAutoScrollViewIsPlaying(TitleAutoScrollHoverEffectView, false);
                UpdateAutoScrollViewIsPlaying(ArtistsAutoScrollHoverEffectView, false);
                UpdateAutoScrollViewIsPlaying(AlbumAutoScrollHoverEffectView, false);
            }
        }

        public void Receive(PropertyChangedMessage<int> message)
        {
            if (message.Sender == LyricsWindowStatus?.AlbumArtLayoutSettings)
            {
                if (message.PropertyName == nameof(AlbumArtAreaStyleSettings.SongInfoFontSize))
                {
                    _ = RenderSongInfoAsync();
                }
            }
        }

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender == LyricsWindowStatus?.AlbumArtLayoutSettings)
            {
                if (message.PropertyName == nameof(AlbumArtAreaStyleSettings.IsAutoSongInfoFontSize))
                {
                    _ = RenderSongInfoAsync();
                }
            }
            else if (message.Sender == LyricsWindowStatus?.AlbumArtAreaEffectSettings)
            {
                if (message.PropertyName == nameof(AlbumArtAreaEffectSettings.SongInfoAutoScroll))
                {
                    UpdateAutoScrollViewIsPlaying(TitleAutoScrollHoverEffectView, false);
                    UpdateAutoScrollViewIsPlaying(ArtistsAutoScrollHoverEffectView, false);
                    UpdateAutoScrollViewIsPlaying(AlbumAutoScrollHoverEffectView, false);
                }
            }
            else if (message.Sender == LyricsWindowStatus?.LyricsEffectSettings)
            {
                if (message.PropertyName == nameof(LyricsEffectSettings.Lyrics3DAutoFitLayout))
                {
                    OnLayoutChanged();
                }
            }
        }

        public void Receive(PropertyChangedMessage<string> message)
        {
            if (message.Sender == LyricsWindowStatus?.LyricsStyleSettings)
            {
                if (message.PropertyName == nameof(LyricsStyleSettings.LyricsCJKFontFamily))
                {
                    _ = RenderSongInfoAsync();
                }
                else if (message.PropertyName == nameof(LyricsStyleSettings.LyricsWesternFontFamily))
                {
                    _ = RenderSongInfoAsync();
                }
            }
        }

        public void Receive(PropertyChangedMessage<MappedSongSearchQuery?> message)
        {
            if (message.Sender is LyricsSearchControlViewModel)
            {
                if (message.PropertyName == nameof(LyricsSearchControlViewModel.MappedSongSearchQuery))
                {
                    _ = RefreshSongInfoAsync();
                }
            }
        }

        public void Receive(PropertyChangedMessage<NowPlayingPalette> message)
        {
            if (message.Sender is LyricsWindowStatus)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.WindowPalette))
                {
                    _ = RenderSongInfoAsync();
                }
            }
        }

        public void Receive(PropertyChangedMessage<FullyObservableCollection<ComponentPlacement>> message)
        {
            if (message.Sender == LyricsWindowStatus?.LayoutProfile)
            {
                if (message.PropertyName == nameof(LayoutProfile.Placements))
                {
                    OnLayoutChanged();
                }
            }
        }

        public void Receive(PropertyChangedMessage<ObservableCollection<string>> message)
        {
            if (message.Sender == LyricsWindowStatus?.LayoutProfile)
            {
                if (message.PropertyName == nameof(LayoutProfile.RowDefinitions))
                {
                    OnLayoutChanged();
                }
                else if (message.PropertyName == nameof(LayoutProfile.ColumnDefinitions))
                {
                    OnLayoutChanged();
                }
            }
        }

        public void Receive(PropertyChangedMessage<double> message)
        {
            if (message.Sender == LyricsWindowStatus?.LayoutProfile)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.LayoutProfile.PaddingLeft))
                {
                    DynamicLayoutGrid.Padding = DynamicLayoutGrid.Padding.WithLeft(message.NewValue);
                    OnLayoutChanged();
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.LayoutProfile.PaddingTop))
                {
                    DynamicLayoutGrid.Padding = DynamicLayoutGrid.Padding.WithTop(message.NewValue);
                    OnLayoutChanged();
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.LayoutProfile.PaddingRight))
                {
                    DynamicLayoutGrid.Padding = DynamicLayoutGrid.Padding.WithRight(message.NewValue);
                    OnLayoutChanged();
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.LayoutProfile.PaddingBottom))
                {
                    DynamicLayoutGrid.Padding = DynamicLayoutGrid.Padding.WithBottom(message.NewValue);
                    OnLayoutChanged();
                }
            }
        }
    }
}