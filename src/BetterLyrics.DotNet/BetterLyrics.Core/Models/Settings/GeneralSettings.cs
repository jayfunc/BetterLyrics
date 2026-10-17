using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Messages.AppSettings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace BetterLyrics.Core.Models.Settings;

public partial class GeneralSettings : ObservableObject
{
    [ObservableProperty]
    public partial NoLyricsFoundHandlingType NoLyricsFoundHandlingType { get; set; } = NoLyricsFoundHandlingType.ShowMessage;

    [ObservableProperty]
    public partial string CustomNotFoundMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string GlobalFontFamily { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LanguageCode { get; set; } = "";

    [ObservableProperty]
    public partial string LXMusicServer { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AmllTtmlDbBaseUrl { get; set; } = AmllTTmlDB.BaseUrl;

    [ObservableProperty]
    public partial List<string> ShowOrHideLyricsWindowShortcut { get; set; } = new() { "Ctrl", "Alt", "H" };

    [ObservableProperty]
    public partial bool ExitOnLyricsWindowClosed { get; set; } = false;

    [ObservableProperty]
    public partial bool ListenOnNewPlaybackSource { get; set; } = true;

    [ObservableProperty]
    public partial bool IgnoreCacheWhenSearching { get; set; } = false;

    [ObservableProperty]
    public partial int MaxAutoRetryCount { get; set; } = 3;

    [ObservableProperty]
    public partial List<string> LyricsWindowSwitchShortcut { get; set; } = new() { "Ctrl", "Alt", "S" };

    [ObservableProperty]
    public partial List<string> PlayOrPauseShortcut { get; set; } = new() { "Ctrl", "Alt", "P" };

    [ObservableProperty]
    public partial List<string> NextSongShortcut { get; set; } = new() { "Ctrl", "Alt", "Right" };

    [ObservableProperty]
    public partial List<string> PreviousSongShortcut { get; set; } = new() { "Ctrl", "Alt", "Left" };

    [ObservableProperty]
    public partial bool MultiNowPlayingWindowMode { get; set; } = false;

    [ObservableProperty]
    public partial bool AutoStartLyricsWindow { get; set; } = true;

    [ObservableProperty]
    public partial bool EnhanceControlInteractiveAnimations { get; set; } = true;

    [ObservableProperty]
    public partial AppTheme AppTheme { get; set; } = AppTheme.Default;

    [ObservableProperty]
    public partial bool ShowSplashScreen { get; set; } = true;

    [ObservableProperty]
    public partial DateTime LastAppUpateCheckDateTime { get; set; } = DateTime.Now;

    [ObservableProperty]
    public partial bool ShowNowPlayingNotification { get; set; } = false;

    [ObservableProperty]
    public partial NowPlayingNotificationCorner NowPlayingNotificationCorner { get; set; } = NowPlayingNotificationCorner.BottomLeft;

    [ObservableProperty]
    public partial bool NowPlayingNotificationAllMonitors { get; set; } = false;

    [ObservableProperty]
    public partial AppTheme NowPlayingNotificationTheme { get; set; } = AppTheme.Default;

    [ObservableProperty]
    public partial double NowPlayingNotificationDuration { get; set; } = 3500;

    partial void OnNoLyricsFoundHandlingTypeChanged(NoLyricsFoundHandlingType value) =>
        WeakReferenceMessenger.Default.Send(new NoLyricsFoundHandlingChangedMessage());

    partial void OnCustomNotFoundMessageChanged(string value) =>
        WeakReferenceMessenger.Default.Send(new NoLyricsFoundHandlingChangedMessage());

    partial void OnGlobalFontFamilyChanged(string value) =>
        WeakReferenceMessenger.Default.Send(new GlobalFontFamilyChangedMessage(value));

    partial void OnLanguageCodeChanged(string value) =>
        WeakReferenceMessenger.Default.Send(new LanguageCodeChangedMessage(value));

    partial void OnShowOrHideLyricsWindowShortcutChanged(List<string> value) =>
        WeakReferenceMessenger.Default.Send(new GlobalShortcutChangedMessage(ShortcutId.LyricsWindowShowOrHide, value));

    partial void OnLyricsWindowSwitchShortcutChanged(List<string> value) =>
        WeakReferenceMessenger.Default.Send(new GlobalShortcutChangedMessage(ShortcutId.LyricsWindowSwitch, value));

    partial void OnPlayOrPauseShortcutChanged(List<string> value) =>
        WeakReferenceMessenger.Default.Send(new GlobalShortcutChangedMessage(ShortcutId.PlayOrPauseSong, value));

    partial void OnNextSongShortcutChanged(List<string> value) =>
        WeakReferenceMessenger.Default.Send(new GlobalShortcutChangedMessage(ShortcutId.NextSong, value));

    partial void OnPreviousSongShortcutChanged(List<string> value) =>
        WeakReferenceMessenger.Default.Send(new GlobalShortcutChangedMessage(ShortcutId.PreviousSong, value));

    partial void OnMultiNowPlayingWindowModeChanged(bool value) =>
        WeakReferenceMessenger.Default.Send(new MultiNowPlayingWindowModeChangedMessage(value));

    partial void OnEnhanceControlInteractiveAnimationsChanged(bool value) =>
        WeakReferenceMessenger.Default.Send(new EnhanceControlInteractiveAnimationsChangedMessage(value));

    partial void OnAppThemeChanged(AppTheme value) =>
        WeakReferenceMessenger.Default.Send(new AppThemeChangedMessage(value));

    partial void OnNowPlayingNotificationThemeChanged(AppTheme value) =>
        WeakReferenceMessenger.Default.Send(new NowPlayingNotificationThemeChangedMessage(value));
}