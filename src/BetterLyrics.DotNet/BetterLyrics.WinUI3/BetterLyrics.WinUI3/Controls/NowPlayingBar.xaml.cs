using System;
using System.Numerics;
using Windows.System;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Events;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using BetterLyrics.Core.ViewModels;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class NowPlayingBar : UserControl
{
    public static readonly DependencyProperty ShowTimeProperty =
        DependencyProperty.Register(nameof(ShowTime), typeof(bool), typeof(NowPlayingBar), new PropertyMetadata(false));

    public static readonly DependencyProperty ShowSongInfoProperty =
        DependencyProperty.Register(nameof(ShowSongInfo), typeof(bool), typeof(NowPlayingBar),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ShowPlayingQueueButtonProperty =
        DependencyProperty.Register(nameof(ShowPlayingQueueButton), typeof(bool), typeof(NowPlayingBar),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ShowStopButtonProperty =
        DependencyProperty.Register(nameof(ShowStopButton), typeof(bool), typeof(NowPlayingBar),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ShowPlaybackOrderButtonProperty =
        DependencyProperty.Register(nameof(ShowPlaybackOrderButton), typeof(bool), typeof(NowPlayingBar),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ShowVolumeButtonProperty =
        DependencyProperty.Register(nameof(ShowVolumeButton), typeof(bool), typeof(NowPlayingBar),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ShowMoreButtonProperty =
        DependencyProperty.Register(nameof(ShowMoreButton), typeof(bool), typeof(NowPlayingBar),
            new PropertyMetadata(true));

    public static readonly DependencyProperty IsCompactModeProperty =
        DependencyProperty.Register(nameof(IsCompactMode), typeof(bool), typeof(NowPlayingBar),
            new PropertyMetadata(false, OnDependencyPropertyChanged));

    public static readonly DependencyProperty IsAutoHideEnabledProperty =
        DependencyProperty.Register(nameof(IsAutoHideEnabled), typeof(bool), typeof(NowPlayingBar),
            new PropertyMetadata(false, OnDependencyPropertyChanged));

    public static readonly DependencyProperty LyricsWindowStatusProperty =
        DependencyProperty.Register(nameof(LyricsWindowStatus), typeof(LyricsWindowStatus), typeof(NowPlayingBar),
            new PropertyMetadata(null));

    private readonly IGlobalToastProvider _globalToastProvider = Ioc.Default.GetRequiredService<IGlobalToastProvider>();

    private readonly IWindowManagerProvider
        _windowManagerProvider = Ioc.Default.GetRequiredService<IWindowManagerProvider>();

    private readonly IProgramProvider _programProvider = Ioc.Default.GetRequiredService<IProgramProvider>();

    private bool _isPointerInBottomCommandGrid;

    public NowPlayingBar()
    {
        InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<NowPlayingBarViewModel>();
        GSMTCService = Ioc.Default.GetRequiredService<IGsmtcService>();
    }

    public NowPlayingBarViewModel ViewModel { get; set; }
    public IGsmtcService GSMTCService { get; set; }

    public bool ShowTime
    {
        get => (bool)GetValue(ShowTimeProperty);
        set => SetValue(ShowTimeProperty, value);
    }

    public bool ShowSongInfo
    {
        get => (bool)GetValue(ShowSongInfoProperty);
        set => SetValue(ShowSongInfoProperty, value);
    }

    public bool ShowPlayingQueueButton
    {
        get => (bool)GetValue(ShowPlayingQueueButtonProperty);
        set => SetValue(ShowPlayingQueueButtonProperty, value);
    }

    public bool ShowPlaybackOrderButton
    {
        get => (bool)GetValue(ShowPlaybackOrderButtonProperty);
        set => SetValue(ShowPlaybackOrderButtonProperty, value);
    }

    public bool ShowStopButton
    {
        get => (bool)GetValue(ShowStopButtonProperty);
        set => SetValue(ShowStopButtonProperty, value);
    }

    public bool ShowVolumeButton
    {
        get => (bool)GetValue(ShowVolumeButtonProperty);
        set => SetValue(ShowVolumeButtonProperty, value);
    }

    public bool ShowMoreButton
    {
        get => (bool)GetValue(ShowMoreButtonProperty);
        set => SetValue(ShowMoreButtonProperty, value);
    }

    public bool IsCompactMode
    {
        get => (bool)GetValue(IsCompactModeProperty);
        set => SetValue(IsCompactModeProperty, value);
    }

    public bool IsAutoHideEnabled
    {
        get => (bool)GetValue(IsAutoHideEnabledProperty);
        set => SetValue(IsAutoHideEnabledProperty, value);
    }

    public LyricsWindowStatus? LyricsWindowStatus
    {
        get => (LyricsWindowStatus?)GetValue(LyricsWindowStatusProperty);
        set => SetValue(LyricsWindowStatusProperty, value);
    }

    public event EventHandler? SongInfoTapped;
    public event EventHandler? TimeTapped;
    public event EventHandler? PlayQueueButtonClick;

    private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NowPlayingBar self)
        {
            if (e.Property == IsCompactModeProperty)
                self.OnIsCompactModeChanged();
            else if (e.Property == IsAutoHideEnabledProperty) self.OnIsAutoHideEnabledChanged();
        }
    }

    private void OnIsAutoHideEnabledChanged()
    {
        if (IsAutoHideEnabled)
        {
            if (!_isPointerInBottomCommandGrid) BottomCommandGrid.Opacity = 0;
        }
        else
        {
            BottomCommandGrid.Opacity = 1;
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
        var content = new PlaybackSettingsControl
        {
            MaxHeight = 500,
            MaxWidth = 850,
            HideConfigPanelWhenLoaded = false
        };
        PlaybackSettingsFlyout.Content = content;
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

    private void PlaybackSettingsFlyout_Opened(object sender, object e)
    {
        var content = (PlaybackSettingsControl)PlaybackSettingsFlyout.Content;
        content.ShowCurrentConfigPanel();
    }

    private void LyricsSettingsFlyout_Closed(object sender, object e)
    {
        LyricsSettingsFlyout.Content = null;
    }

    private async void LyricsSettingsFlyout_Opened(object sender, object e)
    {
        var content = (LyricsWindowSettingsControl)LyricsSettingsFlyout.Content;
        content.ShowConfigPanel(LyricsWindowStatus);
    }

    private void LyricsSettingsShortcutMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        var content = new LyricsWindowSettingsControl
        {
            MaxHeight = 500,
            MaxWidth = 850,
            HideConfigPanelWhenLoaded = false
        };
        LyricsSettingsFlyout.Content = content;
        LyricsSettingsFlyout.ShowAt(BottomRightCommandStackPanel);
    }

    private void TimelineSliderOverlay_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var grid = (Grid)sender;
        var pos = e.GetCurrentPoint(grid).Position;
        var ratio = pos.X / grid.ActualWidth;
        _ = GSMTCService.ChangePositionAsync(TimelineSlider.Maximum * ratio);
    }

    private void TimelineSliderOverlay_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        float targetX;
        var grid = (Grid)sender;
        var pos = e.GetCurrentPoint(grid).Position;
        var ratio = pos.X / grid.ActualWidth;
        ViewModel.TimelineSliderThumbSeconds = TimelineSlider.Maximum * ratio;
        if (pos.X + TimelineSliderLyricsLineInfo.ActualWidth > grid.ActualWidth)
            targetX = (float)(grid.ActualWidth - TimelineSliderLyricsLineInfo.ActualWidth);
        else
            targetX = (float)pos.X;

        TimelineSliderLyricsLineInfo.Translation = new Vector3(targetX, 0, 0);
    }

    private void TimelineSliderOverlay_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (LyricsWindowStatus?.IsTimelineLyricsPreviewEnabled == true)
        {
            TimelineSliderLyricsLineInfo.Opacity = 1f;
        }
    }

    private void TimelineSliderOverlay_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        TimelineSliderLyricsLineInfo.Opacity = 0f;
    }

    private void ExtendedSlider_ValueChangedByUser(object sender, ExtendedSliderValueChangedByUserEventArgs e)
    {
        AudioMixerHook.SetApplicationVolume(GSMTCService.CurrentMediaSourceProviderInfo?.Provider, ViewModel.Volume);
    }

    private void LyricsSearchShortcutButton_Click(object sender, RoutedEventArgs e)
    {
        _windowManagerProvider.OpenOrShowWindow<LyricsSearchWindow>();
    }

    private void SongInfoStackPanel_Tapped(object sender, TappedRoutedEventArgs e)
    {
        SongInfoTapped?.Invoke(sender, EventArgs.Empty);
    }

    private void TimeStackPanel_Tapped(object sender, TappedRoutedEventArgs e)
    {
        TimeTapped?.Invoke(sender, EventArgs.Empty);
    }

    private void BottomCommandGrid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        LyricsOpenHintGrid.Opacity = 1;
        ViewModel.UpdateVolume();
        _isPointerInBottomCommandGrid = true;
        if (IsAutoHideEnabled && BottomCommandGrid.Children.Count != 0) BottomCommandGrid.Opacity = 1f;

        e.Handled = true;
    }

    private void BottomCommandGrid_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        LyricsOpenHintGrid.Opacity = 0;
        _isPointerInBottomCommandGrid = false;
        if (IsAutoHideEnabled && BottomCommandGrid.Children.Count != 0) BottomCommandGrid.Opacity = 0f;

        e.Handled = true;
    }

    private void BottomCommandFlyoutTrigger_PointerEntered(object sender,
        PointerRoutedEventArgs e)
    {
        if (BottomCommandFlyoutContainer.Children.Count != 0) BottomCommandFlyoutTrigger.Opacity = 1f;
    }

    private void BottomCommandFlyoutTrigger_PointerExited(object sender,
        PointerRoutedEventArgs e)
    {
        if (BottomCommandFlyoutContainer.Children.Count != 0) BottomCommandFlyoutTrigger.Opacity = 0f;
    }

    private void BottomCommandFlyoutTrigger_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (BottomCommandFlyoutContainer.Children.Count != 0) BottomCommandFlyout.ShowAt(BottomCommandFlyoutTrigger);
    }

    private void PlayingQueueButton_Click(object sender, RoutedEventArgs e)
    {
        PlayQueueButtonClick?.Invoke(sender, EventArgs.Empty);
    }

    private void PlaybackOrderButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.AppSettings.MusicGallerySettings.PlaybackOrder =
            ViewModel.AppSettings.MusicGallerySettings.PlaybackOrder.GetNext();
    }

    private async void OpenPlaybackSourceButton_Click(object sender, RoutedEventArgs e)
    {
        var amuid = GSMTCService.CurrentMediaSourceProviderInfo?.Provider;
        var path = await _programProvider.GetAppPathByAumidAsync(amuid);
        if (path != null)
            try
            {
                var ok = await Launcher.LaunchUriAsync(new Uri(path));
                if (!ok) _globalToastProvider.Show("Error", $"Fail to launch {path}", MessageSeverity.Warning);
            }
            catch (Exception)
            {
                _globalToastProvider.Show("Error", $"Could't launch {path}", MessageSeverity.Error);
            }
        else
            _globalToastProvider.Show("Error", $"Could't get the path for {amuid}", MessageSeverity.Warning);
    }
}