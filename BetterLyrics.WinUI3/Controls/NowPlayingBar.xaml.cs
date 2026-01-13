using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Numerics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class NowPlayingBar : UserControl
{
    public NowPlayingBarViewModel ViewModel => (NowPlayingBarViewModel)DataContext;

    public event EventHandler? SongInfoTapped;
    public event EventHandler? TimeTapped;
    public event EventHandler? PlayQueueButtonClick;

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

    public bool ShowPlayingQueueButton
    {
        get { return (bool)GetValue(ShowPlayingQueueButtonProperty); }
        set { SetValue(ShowPlayingQueueButtonProperty, value); }
    }

    public static readonly DependencyProperty ShowPlayingQueueButtonProperty =
        DependencyProperty.Register(nameof(ShowPlayingQueueButton), typeof(bool), typeof(NowPlayingBar), new PropertyMetadata(false));

    public bool ShowPlaybackOrderButton
    {
        get { return (bool)GetValue(ShowPlaybackOrderButtonProperty); }
        set { SetValue(ShowPlaybackOrderButtonProperty, value); }
    }

    public static readonly DependencyProperty ShowStopButtonProperty =
        DependencyProperty.Register(nameof(ShowStopButton), typeof(bool), typeof(NowPlayingBar), new PropertyMetadata(false));

    public bool ShowStopButton
    {
        get { return (bool)GetValue(ShowStopButtonProperty); }
        set { SetValue(ShowStopButtonProperty, value); }
    }

    public static readonly DependencyProperty ShowPlaybackOrderButtonProperty =
        DependencyProperty.Register(nameof(ShowPlaybackOrderButton), typeof(bool), typeof(NowPlayingBar), new PropertyMetadata(false));

    public PlaybackOrder PlaybackOrder
    {
        get { return (PlaybackOrder)GetValue(PlaybackOrderProperty); }
        set { SetValue(PlaybackOrderProperty, value); }
    }

    public static readonly DependencyProperty PlaybackOrderProperty =
        DependencyProperty.Register(nameof(PlaybackOrder), typeof(PlaybackOrder), typeof(NowPlayingBar), new PropertyMetadata(PlaybackOrder.RepeatAll));

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
        ViewModel.GSMTCService.ChangePosition(TimelineSlider.Maximum * ratio);
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
        ViewModel.TimelineSliderThumbOpacity = 1f;
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
        SongInfoTapped?.Invoke(sender, EventArgs.Empty);
    }

    private void TimeStackPanel_Tapped(object sender, TappedRoutedEventArgs e)
    {
        TimeTapped?.Invoke(sender, EventArgs.Empty);
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

    private void PlayingQueueButton_Click(object sender, RoutedEventArgs e)
    {
        PlayQueueButtonClick?.Invoke(sender, EventArgs.Empty);
    }

    private void PlaybackOrderButton_Click(object sender, RoutedEventArgs e)
    {
        PlaybackOrder = PlaybackOrder.GetNext();
    }
}
