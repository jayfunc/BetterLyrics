using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Messages.AppSettings;
using BetterLyrics.Core.Messages.SettingsNavigation;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;

namespace BetterLyrics.Core.ViewModels;

/// <summary>
/// Singleton ViewModel
/// </summary>
public partial class LyricsWindowManagerControlViewModel : ObservableRecipient,
    IRecipient<MultiNowPlayingWindowModeChangedMessage>,
    IRecipient<LyricsWindowNavigationRequestedMessage>
{
    private readonly ISettingsService _settingsService;
    private readonly IWindowManagerProvider _windowManagerProvider;
    private readonly ILocalizationService _localizationService;
    private readonly ILauncherProvider _launcherProvider;

    [ObservableProperty] public partial AppSettings AppSettings { get; set; }

    [ObservableProperty]
    public partial LyricsWindowStatus? SelectedWindowStatus { get; set; }

    public ObservableCollection<LyricsWindowManagerSettingsNavMenuItem> NavItems = [];

    [ObservableProperty]
    public partial LyricsWindowManagerSettingsNavMenuItem? SelectedNavItem { get; set; }

    [ObservableProperty]
    public partial bool IsConfigPanelOpen { get; set; }

    public LyricsWindowManagerControlViewModel(ISettingsService settingsService,
        IWindowManagerProvider windowManagerProvider, ILocalizationService localizationService, ILauncherProvider launcherProvider)
    {
        _settingsService = settingsService;
        _windowManagerProvider = windowManagerProvider;
        _localizationService = localizationService;
        _launcherProvider = launcherProvider;

        AppSettings = _settingsService.AppSettings;

        NavItems.Add(new LyricsWindowManagerSettingsNavMenuItem
        {
            Label = _localizationService.GetLocalizedString("AppSettingsControlGeneral/Text"),
            Section = LyricsWindowManagerSettingsSection.Window
        });
        NavItems.Add(new LyricsWindowManagerSettingsNavMenuItem
        {
            Label = _localizationService.GetLocalizedString("SettingsPageLayout/Text"),
            Section = LyricsWindowManagerSettingsSection.Layout
        });
        NavItems.Add(new LyricsWindowManagerSettingsNavMenuItem
        {
            Label = _localizationService.GetLocalizedString("SettingsPageAlbumArt/Text"),
            Section = LyricsWindowManagerSettingsSection.AlbumArt
        });
        NavItems.Add(new LyricsWindowManagerSettingsNavMenuItem
        {
            Label = _localizationService.GetLocalizedString("SettingsPageLyricsStyle/Text"),
            Section = LyricsWindowManagerSettingsSection.LyricsStyle
        });
        NavItems.Add(new LyricsWindowManagerSettingsNavMenuItem
        {
            Label = _localizationService.GetLocalizedString("SettingsPageLyricsEffect/Text"),
            Section = LyricsWindowManagerSettingsSection.LyricsEffect
        });
        NavItems.Add(new LyricsWindowManagerSettingsNavMenuItem
        {
            Label = _localizationService.GetLocalizedString("SettingsPageBackgroundOverlay/Text"),
            Section = LyricsWindowManagerSettingsSection.LyricsBackground
        });
        SelectedNavItem = NavItems[0];

        IsActive = true;
    }

    partial void OnSelectedWindowStatusChanged(LyricsWindowStatus? value) =>
        WeakReferenceMessenger.Default.Send(new SettingsSectionChangedMessage(this));

    partial void OnSelectedNavItemChanged(LyricsWindowManagerSettingsNavMenuItem? value) =>
        WeakReferenceMessenger.Default.Send(new SettingsSectionChangedMessage(this));

    [RelayCommand]
    private void CreateLyricsWindowStatus(LyricsWindowMode mode)
    {
        var status = new LyricsWindowStatus(mode);
        status.LayoutProfileId =
            AppSettings.LayoutProfiles.First(x => x.Mode == status.GetDefaultLayoutProfileMode()).Id;
        AppSettings.WindowBoundsRecords.Add(status);
    }

    [RelayCommand]
    private void CloseConfigPanel()
    {
        SelectedWindowStatus = null;
        IsConfigPanelOpen = false;
    }

    [RelayCommand]
    private void OpenSystemDisplaySettings()
    {
        _launcherProvider.LaunchUriAsync(new Uri("ms-settings:display"));
    }

    [RelayCommand]
    private void ShowConfigPanel(LyricsWindowStatus? status)
    {
        SelectedWindowStatus = status;
        SelectedNavItem = NavItems[0];
        IsConfigPanelOpen = status != null;
    }

    [RelayCommand]
    private void ShowEmbeddedConfigPanel(LyricsWindowStatus? status)
    {
        ShowConfigPanel(_settingsService.AppSettings.MusicGallerySettings.LyricsWindowStatus);
    }

    public void Receive(MultiNowPlayingWindowModeChangedMessage message)
    {
        if (!message.Value &&
            AppSettings.WindowBoundsRecords.Any(x => x.WindowStatus == WindowStatus.Opened))
        {
            var windows = _windowManagerProvider.GetWindows(WindowType.NowPlayingWindow);
            var latest = windows.Last();
            foreach (var item in windows)
                if (item != latest)
                    _windowManagerProvider.CloseWindow(item);
        }
    }

    public void Receive(LyricsWindowNavigationRequestedMessage message)
    {
        // 根据 WindowId 查找目标窗口状态
        var target = message.TargetWindowId.HasValue
            ? AppSettings.WindowBoundsRecords.FirstOrDefault(w => w.Id == message.TargetWindowId.Value)
            : null;

        ShowConfigPanel(target);
        SelectedNavItem = NavItems.FirstOrDefault(x => x.Section == message.TargetSection);
    }
}