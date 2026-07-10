using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Input;
using global::Avalonia.Interactivity;
using global::Avalonia.Media;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Events;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.Diagnostics;

namespace BetterLyrics.Avalonia.Controls;

public partial class NowPlayingBar : UserControl
{
    public static readonly StyledProperty<bool> ShowTimeProperty =
        AvaloniaProperty.Register<NowPlayingBar, bool>(nameof(ShowTime), false);

    public static readonly StyledProperty<bool> ShowSongInfoProperty =
        AvaloniaProperty.Register<NowPlayingBar, bool>(nameof(ShowSongInfo), false);

    public static readonly StyledProperty<bool> ShowPlayingQueueButtonProperty =
        AvaloniaProperty.Register<NowPlayingBar, bool>(nameof(ShowPlayingQueueButton), false);

    public static readonly StyledProperty<bool> ShowStopButtonProperty =
        AvaloniaProperty.Register<NowPlayingBar, bool>(nameof(ShowStopButton), false);

    public static readonly StyledProperty<bool> ShowPlaybackOrderButtonProperty =
        AvaloniaProperty.Register<NowPlayingBar, bool>(nameof(ShowPlaybackOrderButton), false);

    public static readonly StyledProperty<bool> ShowVolumeButtonProperty =
        AvaloniaProperty.Register<NowPlayingBar, bool>(nameof(ShowVolumeButton), true);

    public static readonly StyledProperty<bool> ShowMoreButtonProperty =
        AvaloniaProperty.Register<NowPlayingBar, bool>(nameof(ShowMoreButton), true);

    public static readonly StyledProperty<bool> IsCompactModeProperty =
        AvaloniaProperty.Register<NowPlayingBar, bool>(nameof(IsCompactMode), false);

    public static readonly StyledProperty<bool> IsAutoHideEnabledProperty =
        AvaloniaProperty.Register<NowPlayingBar, bool>(nameof(IsAutoHideEnabled), false);

    public static new readonly StyledProperty<Thickness> PaddingProperty =
        AvaloniaProperty.Register<NowPlayingBar, Thickness>(nameof(Padding), new Thickness(0));

    public static readonly StyledProperty<LyricsWindowStatus?> LyricsWindowStatusProperty =
        AvaloniaProperty.Register<NowPlayingBar, LyricsWindowStatus?>(nameof(LyricsWindowStatus));

    private readonly IGlobalToastProvider _globalToastProvider = Ioc.Default.GetRequiredService<IGlobalToastProvider>();
    private readonly IWindowManagerProvider _windowManagerProvider = Ioc.Default.GetRequiredService<IWindowManagerProvider>();
    private readonly IProgramProvider _programProvider = Ioc.Default.GetRequiredService<IProgramProvider>();

    private TranslateTransform? LyricsLineInfoTransform => (TranslateTransform?)TimelineSliderLyricsLineInfo.RenderTransform;
    private TranslateTransform? HintTransform => (TranslateTransform?)BottomCommandFlyoutTriggerHint.RenderTransform;

    private Flyout? VolumeFlyout => (Flyout?)VolumeButton.Flyout;

    private bool _isPointerInBottomCommandGrid;
    private bool _isDraggingTimeline;

    public NowPlayingBar()
    {
        InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<NowPlayingBarViewModel>();
        GSMTCService = Ioc.Default.GetRequiredService<IGsmtcService>();

        DataContext = this;
    }

    public NowPlayingBarViewModel ViewModel { get; set; }
    public IGsmtcService GSMTCService { get; set; }

    public bool ShowTime
    {
        get => GetValue(ShowTimeProperty);
        set => SetValue(ShowTimeProperty, value);
    }

    public bool ShowSongInfo
    {
        get => GetValue(ShowSongInfoProperty);
        set => SetValue(ShowSongInfoProperty, value);
    }

    public bool ShowPlayingQueueButton
    {
        get => GetValue(ShowPlayingQueueButtonProperty);
        set => SetValue(ShowPlayingQueueButtonProperty, value);
    }

    public bool ShowPlaybackOrderButton
    {
        get => GetValue(ShowPlaybackOrderButtonProperty);
        set => SetValue(ShowPlaybackOrderButtonProperty, value);
    }

    public bool ShowStopButton
    {
        get => GetValue(ShowStopButtonProperty);
        set => SetValue(ShowStopButtonProperty, value);
    }

    public bool ShowVolumeButton
    {
        get => GetValue(ShowVolumeButtonProperty);
        set => SetValue(ShowVolumeButtonProperty, value);
    }

    public bool ShowMoreButton
    {
        get => GetValue(ShowMoreButtonProperty);
        set => SetValue(ShowMoreButtonProperty, value);
    }

    public bool IsCompactMode
    {
        get => GetValue(IsCompactModeProperty);
        set => SetValue(IsCompactModeProperty, value);
    }

    public bool IsAutoHideEnabled
    {
        get => GetValue(IsAutoHideEnabledProperty);
        set => SetValue(IsAutoHideEnabledProperty, value);
    }

    public LyricsWindowStatus? LyricsWindowStatus
    {
        get => GetValue(LyricsWindowStatusProperty);
        set => SetValue(LyricsWindowStatusProperty, value);
    }

    public new Thickness? Padding
    {
        get => GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    public event EventHandler? SongInfoTapped;
    public event EventHandler? TimeTapped;
    public event EventHandler? PlayQueueButtonClick;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsCompactModeProperty)
        {
            OnIsCompactModeChanged();
        }
        else if (change.Property == IsAutoHideEnabledProperty)
        {
            OnIsAutoHideEnabledChanged();
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
        // Adjust for Avalonia's visual tree operations. 
        // Note: You must ensure 'BottomCommandFlyoutContainer' exists in your Avalonia visual layout if you retain this specific swapping logic.

        if (IsCompactMode)
        {
            if (HintTransform != null) HintTransform.Y = 0;
        }
        else
        {
            if (HintTransform != null) HintTransform.Y = 12;
        }
    }

    private void VolumeButton_Click(object? sender, RoutedEventArgs e)
    {
        VolumeFlyout?.ShowAt(BottomRightCommandStackPanel);
    }

    private void TimelineSliderOverlay_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control grid)
        {
            _isDraggingTimeline = true;
            e.Pointer.Capture(grid);

            var pos = e.GetPosition(grid);
            var ratio = Math.Clamp(pos.X / grid.Bounds.Width, 0, 1);
            TimelineSlider.Value = TimelineSlider.Maximum * ratio;
        }
    }

    private void TimelineSliderOverlay_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is Control grid)
        {
            var pos = e.GetPosition(grid);
            var ratio = Math.Clamp(pos.X / grid.Bounds.Width, 0, 1);
            ViewModel.TimelineSliderThumbSeconds = TimelineSlider.Maximum * ratio;

            if (_isDraggingTimeline)
            {
                TimelineSlider.Value = TimelineSlider.Maximum * ratio;
            }

            double targetX;
            if (pos.X + TimelineSliderLyricsLineInfo.Bounds.Width > grid.Bounds.Width)
                targetX = grid.Bounds.Width - TimelineSliderLyricsLineInfo.Bounds.Width;
            else
                targetX = pos.X;

            if (LyricsLineInfoTransform != null)
            {
                LyricsLineInfoTransform.X = targetX;
            }
        }
    }

    private void TimelineSliderOverlay_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDraggingTimeline && sender is Control grid)
        {
            _isDraggingTimeline = false;
            e.Pointer.Capture(null);

            var pos = e.GetPosition(grid);
            var ratio = Math.Clamp(pos.X / grid.Bounds.Width, 0, 1);
            _ = GSMTCService.ChangePositionAsync(TimelineSlider.Maximum * ratio);
        }
    }

    private void TimelineSliderOverlay_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (LyricsWindowStatus?.IsTimelineLyricsPreviewEnabled == true)
            ViewModel.TimelineSliderThumbOpacity = 1f;
    }

    private void TimelineSliderOverlay_PointerExited(object? sender, PointerEventArgs e)
    {
        ViewModel.TimelineSliderThumbOpacity = 0f;
    }

    private void ExtendedSlider_ValueChangedByUser(object? sender, ExtendedSliderValueChangedByUserEventArgs e)
    {
        // Assuming AudioMixerHook handles cross-platform or Windows-specific volume operations natively.
        // AudioMixerHook.SetApplicationVolume(GSMTCService.CurrentMediaSourceProviderInfo?.Provider, ViewModel.Volume);
    }

    private void SongInfoStackPanel_Tapped(object? sender, TappedEventArgs e)
    {
        SongInfoTapped?.Invoke(sender, EventArgs.Empty);
    }

    private void TimeStackPanel_Tapped(object? sender, TappedEventArgs e)
    {
        TimeTapped?.Invoke(sender, EventArgs.Empty);
    }

    private void BottomCommandGrid_PointerEntered(object? sender, PointerEventArgs e)
    {
        LyricsOpenHintGrid.Opacity = 1;
        ViewModel.UpdateVolume();
        _isPointerInBottomCommandGrid = true;

        if (IsAutoHideEnabled) BottomCommandGrid.Opacity = 1f;
        e.Handled = true;
    }

    private void BottomCommandGrid_PointerExited(object? sender, PointerEventArgs e)
    {
        LyricsOpenHintGrid.Opacity = 0;
        _isPointerInBottomCommandGrid = false;

        if (IsAutoHideEnabled) BottomCommandGrid.Opacity = 0f;
        e.Handled = true;
    }

    private void BottomCommandFlyoutTrigger_PointerEntered(object? sender, PointerEventArgs e)
    {
        BottomCommandFlyoutTrigger.Opacity = 1f;
    }

    private void BottomCommandFlyoutTrigger_PointerExited(object? sender, PointerEventArgs e)
    {
        BottomCommandFlyoutTrigger.Opacity = 0f;
    }

    private void BottomCommandFlyoutTrigger_Tapped(object? sender, TappedEventArgs e)
    {
        // Logic to trigger layout flyouts goes here
    }

    private void PlayingQueueButton_Click(object? sender, RoutedEventArgs e)
    {
        PlayQueueButtonClick?.Invoke(sender, EventArgs.Empty);
    }

    private void PlaybackOrderButton_Click(object? sender, RoutedEventArgs e)
    {
        // Ensure GetNext() extension method exists in Avalonia core extensions
        ViewModel.AppSettings.MusicGallerySettings.PlaybackOrder =
            ViewModel.AppSettings.MusicGallerySettings.PlaybackOrder.GetNext();
    }

    private async void OpenPlaybackSourceButton_Click(object? sender, RoutedEventArgs e)
    {
        var amuid = GSMTCService.CurrentMediaSourceProviderInfo?.Provider;
        var path = await _programProvider.GetAppPathByAumidAsync(amuid);

        if (path != null)
        {
            try
            {
                // In Avalonia, use System.Diagnostics.Process to launch external executables/URIs.
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch (Exception)
            {
                _globalToastProvider.Show("Error", $"Couldn't launch {path}", MessageSeverity.Error);
            }
        }
        else
        {
            _globalToastProvider.Show("Error", $"Couldn't get the path for {amuid}", MessageSeverity.Warning);
        }
    }
}