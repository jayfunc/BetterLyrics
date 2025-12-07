using ATL;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class NowPlayingBar : UserControl,
    IRecipient<PropertyChangedMessage<SongInfo?>>,
    IRecipient<PropertyChangedMessage<BitmapImage?>>,
    IRecipient<PropertyChangedMessage<TimeSpan>>
{
    public NowPlayingBarViewModel ViewModel => (NowPlayingBarViewModel)DataContext;

    public event EventHandler? SongInfoTapped;
    public event EventHandler? TimeTapped;

    public bool ShowTime
    {
        get { return (bool)GetValue(ShowTimeProperty); }
        set { SetValue(ShowTimeProperty, value); }
    }

    public static readonly DependencyProperty ShowTimeProperty =
        DependencyProperty.Register(nameof(ShowTime), typeof(bool), typeof(NowPlayingBar), new PropertyMetadata(false));

    public bool ShowSongInfo
    {
        get { return (bool)GetValue(ShowSongInfoProperty); }
        set { SetValue(ShowSongInfoProperty, value); }
    }

    public static readonly DependencyProperty ShowSongInfoProperty =
        DependencyProperty.Register(nameof(ShowSongInfo), typeof(bool), typeof(NowPlayingBar), new PropertyMetadata(false));

    public bool IsCompactMode
    {
        get { return (bool)GetValue(IsCompactModeProperty); }
        set { SetValue(IsCompactModeProperty, value); }
    }

    public static readonly DependencyProperty IsCompactModeProperty =
        DependencyProperty.Register(nameof(IsCompactMode), typeof(bool), typeof(NowPlayingBar), new PropertyMetadata(false, OnDependencyPropertyChanged));

    public bool IsAutoHideEnabled
    {
        get { return (bool)GetValue(IsAutoHideEnabledProperty); }
        set { SetValue(IsAutoHideEnabledProperty, value); }
    }

    public static readonly DependencyProperty IsAutoHideEnabledProperty =
        DependencyProperty.Register(nameof(IsAutoHideEnabled), typeof(bool), typeof(NowPlayingBar), new PropertyMetadata(false, OnDependencyPropertyChanged));

    private bool _isPointerInBottomCommandGrid = false;

    public NowPlayingBar()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<NowPlayingBarViewModel>();

        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NowPlayingBar self)
        {
            if (e.Property == IsCompactModeProperty)
            {
                self.OnIsCompactModeChanged();
            }
            else if (e.Property == IsAutoHideEnabledProperty)
            {
                self.OnIsAutoHideEnabledChanged();
            }
        }
    }

    private void OnIsAutoHideEnabledChanged()
    {
        if (IsAutoHideEnabled)
        {
            if (!_isPointerInBottomCommandGrid)
            {
                ViewModel.BottomCommandGridOpacity = 0;
            }
        }
        else
        {
            ViewModel.BottomCommandGridOpacity = 1;
        }
    }

    private void OnIsCompactModeChanged()
    {
        if (IsCompactMode)
        {
            if (BottomCommandGrid.Children.Count != 0)
            {
                BottomCommandGrid.Children.Remove(BottomCommandContent);
                BottomCommandFlyoutContainer.Children.Add(BottomCommandContent);
            }
            BottomCommandFlyoutTriggerHint.Translation = new Vector3(0, 0, 0);
        }
        else
        {
            if (BottomCommandFlyoutContainer.Children.Count != 0)
            {
                BottomCommandFlyout.Hide();
                BottomCommandFlyoutContainer.Children.Remove(BottomCommandContent);
                BottomCommandGrid.Children.Add(BottomCommandContent);
            }
            BottomCommandFlyoutTriggerHint.Translation = new Vector3(0, 12, 0);
        }
    }

    private void PlaybackSettingsShortcutMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        PlaybackSettingsFlyout.Content = new PlaybackSettingsControl
        {
            MaxHeight = 500,
            MaxWidth = 850,
        };
        PlaybackSettingsFlyout.ShowAt(BottomRightCommandStackPanel);
    }

    private void VolumeButton_Click(object sender, RoutedEventArgs e)
    {
        VolumeFlyout.ShowAt(BottomRightCommandStackPanel);
    }

    private void PlaybackSettingsFlyout_Closed(object sender, object e)
    {
        PlaybackSettingsFlyout.Content = null;
    }

    private void LyricsSettingsFlyout_Closed(object sender, object e)
    {
        LyricsSettingsFlyout.Content = null;
    }

    private void LyricsSettingsShortcutMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        LyricsSettingsFlyout.Content = new LyricsWindowSettingsControl
        {
            MaxHeight = 500,
            MaxWidth = 850,
        };
        LyricsSettingsFlyout.ShowAt(BottomRightCommandStackPanel);
    }

    private void TimelineSliderOverlay_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var grid = (Grid)sender;
        var pos = e.GetCurrentPoint(grid).Position;
        var ratio = pos.X / grid.ActualWidth;
        ViewModel.MediaSessionsService.ChangePosition(TimelineSlider.Maximum * ratio);
    }

    private void TimelineSliderOverlay_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        float targetX;
        var grid = (Grid)sender;
        var pos = e.GetCurrentPoint(grid).Position;
        var ratio = pos.X / grid.ActualWidth;
        ViewModel.TimelineSliderThumbSeconds = TimelineSlider.Maximum * ratio;
        if (pos.X + TimelineSliderLyricsLineInfo.ActualWidth > grid.ActualWidth)
        {
            targetX = (float)(grid.ActualWidth - TimelineSliderLyricsLineInfo.ActualWidth);
        }
        else
        {
            targetX = (float)pos.X;
        }
        TimelineSliderLyricsLineInfo.Translation = new Vector3(targetX, 0, 0);
    }

    private void TimelineSliderOverlay_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        ViewModel.TimelineSliderThumbOpacity = 0.7f;
    }

    private void TimelineSliderOverlay_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        ViewModel.TimelineSliderThumbOpacity = 0f;
    }

    private void ExtendedSlider_ValueChangedByUser(object sender, Events.ExtendedSliderValueChangedByUserEventArgs e)
    {
        SystemVolumeHook.MasterVolume = ViewModel.Volume;
    }

    private void LyricsSearchShortcutButton_Click(object sender, RoutedEventArgs e)
    {
        WindowHook.OpenOrShowWindow<LyricsSearchWindow>();
    }

    private void SongInfoStackPanel_Tapped(object sender, TappedRoutedEventArgs e)
    {
        SongInfoTapped?.Invoke(this, EventArgs.Empty);
    }

    private void TimeStackPanel_Tapped(object sender, TappedRoutedEventArgs e)
    {
        TimeTapped?.Invoke(this, EventArgs.Empty);
    }

    private void BottomCommandGrid_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _isPointerInBottomCommandGrid = true;
        if (IsAutoHideEnabled && BottomCommandGrid.Children.Count != 0)
        {
            ViewModel.BottomCommandGridOpacity = 1f;
        }
        e.Handled = true;
    }

    private void BottomCommandGrid_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _isPointerInBottomCommandGrid = false;
        if (IsAutoHideEnabled && BottomCommandGrid.Children.Count != 0)
        {
            ViewModel.BottomCommandGridOpacity = 0f;
        }
        e.Handled = true;
    }

    private void BottomCommandFlyoutTrigger_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (BottomCommandFlyoutContainer.Children.Count != 0)
        {
            ViewModel.BottomCommandFlyoutTriggerOpacity = 1f;
        }
    }

    private void BottomCommandFlyoutTrigger_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (BottomCommandFlyoutContainer.Children.Count != 0)
        {
            ViewModel.BottomCommandFlyoutTriggerOpacity = 0f;
        }
    }

    private void BottomCommandFlyoutTrigger_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (BottomCommandFlyoutContainer.Children.Count != 0)
        {
            BottomCommandFlyout.ShowAt(BottomCommandFlyoutTrigger);
        }
    }

    public void Receive(PropertyChangedMessage<SongInfo?> message)
    {
        if (message.Sender is IMediaSessionsService)
        {
            if (message.PropertyName == nameof(IMediaSessionsService.CurrentSongInfo))
            {
                TitleTextBlock.Text = message.NewValue?.Title;
                ArtistsTextBlock.Text = message.NewValue?.DisplayArtists;
            }
        }
    }
    public void Receive(PropertyChangedMessage<BitmapImage?> message)
    {
        if (message.Sender is IMediaSessionsService)
        {
            if (message.PropertyName == nameof(IMediaSessionsService.AlbumArtBitmapImage))
            {
                AlbumArtImageSwitcher.Source = message.NewValue;
            }
        }
    }

    public void Receive(PropertyChangedMessage<TimeSpan> message)
    {
        if (message.Sender is IMediaSessionsService)
        {
            if (message.PropertyName == nameof(IMediaSessionsService.CurrentPosition))
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    TimelineSlider.Value = message.NewValue.TotalSeconds;
                });
            }
        }
    }

}
