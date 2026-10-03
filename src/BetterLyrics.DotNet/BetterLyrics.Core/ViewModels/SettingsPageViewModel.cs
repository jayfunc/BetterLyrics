// 2025/6/23 by Zhe Fang

using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using BetterLyrics.Core.Extensions;

namespace BetterLyrics.Core.ViewModels;

public partial class SettingsPageViewModel : BaseViewModel,
    IRecipient<PropertyChangedMessage<AppSettingsSection>>,
    IRecipient<PropertyChangedMessage<PlaybackLibSettingsSection>>,
    IRecipient<PropertyChangedMessage<MediaSourceProviderInfo>>,
    IRecipient<PropertyChangedMessage<LyricsWindowManagerSettingsSection>>,
    IRecipient<PropertyChangedMessage<LyricsWindowStatus>>
{
    private readonly ILocalizationService _localizationService;
    private readonly IGlobalToastProvider _globalToastProvider;

    private readonly AppSettingsControlViewModel _appSettingsControlViewModel;
    private readonly PlaybackSettingsControlViewModel _playbackSettingsControlViewModel;
    private readonly LyricsWindowManagerControlViewModel _lyricsWindowSettingsControlViewModel;

    private readonly Debouncer _searchDebouncer = new();

    [ObservableProperty] public partial NavMenuItem? SelectedMenuItem { get; set; }

    [ObservableProperty] public partial string CurrentPath { get; set; } = string.Empty;
    [ObservableProperty] public partial string CurrentDeepLink { get; set; } = string.Empty;

    [ObservableProperty] public partial string SearchQuery { get; set; } = string.Empty;

    public ObservableCollection<NavMenuItem> MenuItems { get; } = [];

    public ObservableCollection<SettingSearchItem> FilteredSettings { get; } = [];

    [ObservableProperty] public partial bool IsSearching { get; set; }

    public SettingsPageViewModel(
        ILocalizationService localizationService,
        IGlobalToastProvider globalToastProvider,
        AppSettingsControlViewModel appSettingsControlViewModel,
        PlaybackSettingsControlViewModel playbackSettingsControlViewModel,
        LyricsWindowManagerControlViewModel lyricsWindowSettingsControlViewModel)
    {
        _localizationService = localizationService;
        _globalToastProvider = globalToastProvider;
        _lyricsWindowSettingsControlViewModel = lyricsWindowSettingsControlViewModel;

        _appSettingsControlViewModel = appSettingsControlViewModel;
        _playbackSettingsControlViewModel = playbackSettingsControlViewModel;

        MenuItems.Add(new NavMenuItem
        {
            Label = _localizationService.GetLocalizedString("SettingsPageApp"),
            Glyph = "\uECAA",
            Section = SettingsSection.App
        });
        MenuItems.Add(new NavMenuItem
        {
            Label = _localizationService.GetLocalizedString("SettingsPageLyricsWindowMgr"),
            Glyph = "\uE61F",
            Section = SettingsSection.LyricsWindowMgr
        });
        MenuItems.Add(new NavMenuItem
        {
            Label = _localizationService.GetLocalizedString("SettingsPageMediaLib"),
            Glyph = "\uE8B7",
            Section = SettingsSection.MediaLib
        });
        MenuItems.Add(new NavMenuItem
        {
            Label = _localizationService.GetLocalizedString("SettingsPagePlaybackLib"),
            Glyph = "\uEA69",
            Section = SettingsSection.PlaybackLib
        });
        MenuItems.Add(new NavMenuItem
        {
            Label = _localizationService.GetLocalizedString("SettingsPagePlugins"),
            Glyph = "\uE74C",
            Section = SettingsSection.Plugins
        });
        MenuItems.Add(new NavMenuItem
        {
            Label = _localizationService.GetLocalizedString("SettingsPageAbout"),
            Glyph = "\uE946",
            Section = SettingsSection.About
        });

        SelectedMenuItem = MenuItems[0];
        UpdateCurrentPath();

    }

    partial void OnSelectedMenuItemChanged(NavMenuItem? value)
    {
        UpdateCurrentPath();
    }

    public void UpdateCurrentPath()
    {
        if (SelectedMenuItem == null) return;

        var displayPathSegments = new List<string> { GetLocalizedTitle("SettingsPageTitle"), GetLocalizedSectionName(SelectedMenuItem.Section) };
        var uriSegments = new List<string> { "betterlyrics://settings", SelectedMenuItem.Section.ToString().ToLowerInvariant() };

        if (SelectedMenuItem.Section == SettingsSection.App)
        {
            var vm = _appSettingsControlViewModel;

            uriSegments.Add(vm.SelectedAppSettingsSection.ToString().ToLowerInvariant());

            displayPathSegments.Add(GetLocalizedSubsectionName(vm.SelectedAppSettingsSection));
        }
        else if (SelectedMenuItem.Section == SettingsSection.PlaybackLib)
        {
            var vm = _playbackSettingsControlViewModel;

            string subsectionName = GetLocalizedSubsectionName(vm.SelectedPlaybackLibSettingsSection);

            if (vm.SelectedPlaybackLibSettingsSection == PlaybackLibSettingsSection.General && vm.SelectedMediaSourceProvider != null)
            {
                uriSegments.Add(vm.SelectedPlaybackLibSettingsSection.ToString().ToLowerInvariant());
                uriSegments.Add(vm.SelectedMediaSourceProvider.Provider);

                displayPathSegments.Add(subsectionName);
                displayPathSegments.Add(GetLocalizedTitle(vm.SelectedMediaSourceProvider.Provider));
            }
            else
            {
                uriSegments.Add(vm.SelectedPlaybackLibSettingsSection.ToString().ToLowerInvariant());
                displayPathSegments.Add(subsectionName);
            }
        }
        else if (SelectedMenuItem.Section == SettingsSection.LyricsWindowMgr)
        {
            var vm = _lyricsWindowSettingsControlViewModel;

            if (vm.SelectedWindowStatus != null)
            {
                uriSegments.Add(vm.SelectedWindowStatus.Id.ToString());
                uriSegments.Add(vm.SelectedLyricsWindowManagerSettingsSection.ToString().ToLowerInvariant());

                displayPathSegments.Add(vm.SelectedWindowStatus.Name);
                displayPathSegments.Add(GetLocalizedSubsectionName(vm.SelectedLyricsWindowManagerSettingsSection));
            }
        }

        CurrentPath = string.Join(" > ", displayPathSegments);
        CurrentDeepLink = string.Join("/", uriSegments);
    }

    public void Receive(PropertyChangedMessage<AppSettingsSection> message)
    {
        if (message.Sender == _appSettingsControlViewModel && message.PropertyName == nameof(AppSettingsControlViewModel.SelectedAppSettingsSection))
        {
            UpdateCurrentPath();
        }
    }

    public void Receive(PropertyChangedMessage<PlaybackLibSettingsSection> message)
    {
        if (message.Sender == _playbackSettingsControlViewModel && message.PropertyName == nameof(PlaybackSettingsControlViewModel.SelectedPlaybackLibSettingsSection))
        {
            UpdateCurrentPath();
        }
    }

    public void Receive(PropertyChangedMessage<MediaSourceProviderInfo> message)
    {
        if (message.Sender == _playbackSettingsControlViewModel && message.PropertyName == nameof(PlaybackSettingsControlViewModel.SelectedMediaSourceProvider))
        {
            UpdateCurrentPath();
        }
    }

    public void Receive(PropertyChangedMessage<LyricsWindowManagerSettingsSection> message)
    {
        if (message.Sender == _lyricsWindowSettingsControlViewModel && message.PropertyName == nameof(LyricsWindowManagerControlViewModel.SelectedLyricsWindowManagerSettingsSection))
        {
            UpdateCurrentPath();
        }
    }

    public void Receive(PropertyChangedMessage<LyricsWindowStatus> message)
    {
        if (message.Sender == _lyricsWindowSettingsControlViewModel && message.PropertyName == nameof(LyricsWindowManagerControlViewModel.SelectedWindowStatus))
        {
            UpdateCurrentPath();
        }
    }

    private string GetLocalizedTitle(string uid)
    {
        var s = _localizationService.GetLocalizedString($"{uid}/Header");
        if (!string.IsNullOrEmpty(s)) return s;

        s = _localizationService.GetLocalizedString($"{uid}/Text");
        if (!string.IsNullOrEmpty(s)) return s;

        s = _localizationService.GetLocalizedString(uid);
        if (!string.IsNullOrEmpty(s)) return s;

        s = _localizationService.GetLocalizedString($"{uid}.Header");
        if (!string.IsNullOrEmpty(s)) return s;

        s = _localizationService.GetLocalizedString($"{uid}.Text");
        if (!string.IsNullOrEmpty(s)) return s;

        return uid;
    }

    private string GetLocalizedSectionName(SettingsSection section)
    {
        string sectionUid = section switch
        {
            SettingsSection.App => "SettingsPageApp",
            SettingsSection.LyricsWindowMgr => "SettingsPageLyricsWindowMgr",
            SettingsSection.MediaLib => "SettingsPageMediaLib",
            SettingsSection.PlaybackLib => "SettingsPagePlaybackLib",
            SettingsSection.Plugins => "SettingsPagePlugins",
            SettingsSection.About => "SettingsPageAbout",
            _ => ""
        };
        return GetLocalizedTitle(sectionUid);
    }

    private string GetLocalizedSubsectionName(Enum? subsection)
    {
        string subsectionUid = subsection switch
        {
            AppSettingsSection.Appearance => "SettingsPageAppAppearance",
            AppSettingsSection.Window => "SettingsPageWindow",
            AppSettingsSection.Shortcut => "SettingsPageShortcut",
            AppSettingsSection.SystemTray => "AppSettingsControlSystemTray",

            LyricsWindowManagerSettingsSection.Window => "SettingsPageWindow",
            LyricsWindowManagerSettingsSection.Layout => "SettingsPageLayout",
            LyricsWindowManagerSettingsSection.AlbumArt => "SettingsPageAlbumArt",
            LyricsWindowManagerSettingsSection.LyricsStyle => "SettingsPageLyricsStyle",
            LyricsWindowManagerSettingsSection.LyricsEffect => "SettingsPageLyricsEffect",
            LyricsWindowManagerSettingsSection.LyricsBackground => "SettingsPageBackgroundOverlay",

            PlaybackLibSettingsSection.General => "SettingsPageConfigPlaybackSource",
            PlaybackLibSettingsSection.LyricsProcessing => "SettingsPageLyricsProcessing",
            PlaybackLibSettingsSection.Integration => "SettingsPageIntegration",
            PlaybackLibSettingsSection.RealtimeStatus => "SettingsPageRealtimeStatus",

            _ => ""
        };
        return GetLocalizedTitle(subsectionUid);
    }

    partial void OnSearchQueryChanged(string value)
    {
        _ = _searchDebouncer.RunAsync(async (token) =>
        {
            try
            {
                IsSearching = true;
                FilteredSettings.Clear();

                if (string.IsNullOrWhiteSpace(value))
                {
                    return;
                }

                FilteredSettings.Add(SettingSearchItemExtensions.LoadingPlaceholder);

                var matchedItems = await Task.Run(() =>
                {
                    var results = new List<SettingSearchItem>();

                    foreach (var staticItem in SettingSearchItemExtensions.AllItems)
                    {
                        if (token.IsCancellationRequested) return null;

                        staticItem.Title = GetLocalizedTitle(staticItem.Uid);

                        if (staticItem.Title.Contains(value, StringComparison.OrdinalIgnoreCase) != true) continue;

                        string secName = GetLocalizedSectionName(staticItem.Section);
                        string basePath = GetLocalizedTitle("SettingsPageTitle") + $" > {secName}";

                        string subsecName = staticItem.Subsection != null ? GetLocalizedSubsectionName(staticItem.Subsection) : "";
                        string parentName = !string.IsNullOrEmpty(staticItem.ParentUid) ? GetLocalizedTitle(staticItem.ParentUid) : "";

                        bool isLyricsWindowMgr = staticItem.Section == SettingsSection.LyricsWindowMgr && staticItem.Subsection != null;
                        bool isPlaybackLib = staticItem.Section == SettingsSection.PlaybackLib && staticItem.Subsection is PlaybackLibSettingsSection.General && staticItem.Uid != "SettingsPageListenNewSession";

                        if (isLyricsWindowMgr || isPlaybackLib)
                        {
                            if (isLyricsWindowMgr)
                            {
                                foreach (var window in _lyricsWindowSettingsControlViewModel.AppSettings.WindowBoundsRecords)
                                {
                                    results.Add(new SettingSearchItem
                                    {
                                        Uid = staticItem.Uid,
                                        Title = staticItem.Title,
                                        Section = staticItem.Section,
                                        Subsection = staticItem.Subsection,
                                        TargetParameter = window.Id,
                                        Path = basePath + $" > {window.Name} > {subsecName}" + (!string.IsNullOrEmpty(parentName) ? $" > {parentName}" : "")
                                    });
                                }
                            }
                            else if (isPlaybackLib)
                            {
                                foreach (var provider in _playbackSettingsControlViewModel.AppSettings.MediaSourceProvidersInfo)
                                {
                                    results.Add(new SettingSearchItem
                                    {
                                        Uid = staticItem.Uid,
                                        Title = staticItem.Title,
                                        Section = staticItem.Section,
                                        Subsection = staticItem.Subsection,
                                        TargetParameter = provider.Provider,
                                        Path = basePath + $" > {subsecName} > {GetLocalizedTitle(provider.Provider)}" + (!string.IsNullOrEmpty(parentName) ? $" > {parentName}" : "")
                                    });
                                }
                            }
                        }
                        else
                        {
                            staticItem.Path = basePath;
                            if (!string.IsNullOrEmpty(subsecName)) staticItem.Path += $" > {subsecName}";
                            if (!string.IsNullOrEmpty(parentName)) staticItem.Path += $" > {parentName}";

                            results.Add(staticItem);
                        }
                    }

                    return results;
                }, token);

                if (token.IsCancellationRequested || matchedItems == null) return;

                FilteredSettings.Clear();
                foreach (var item in matchedItems)
                {
                    FilteredSettings.Add(item);
                }

                if (FilteredSettings.Count == 0)
                {
                    FilteredSettings.Add(SettingSearchItemExtensions.NoResultsPlaceholder);
                }
            }
            finally
            {
                IsSearching = false;
            }
        }, 400);
    }

    public void NavigateToSection(SettingsSection section)
    {
        var targetItem = MenuItems.FirstOrDefault(m => m.Section == section);

        if (targetItem != null) SelectedMenuItem = targetItem;
    }

    public void NavigateToSettingSearchItem(SettingSearchItem item)
    {
        NavigateToSection(item.Section);

        if (item.Subsection != null)
        {
            if (item.Section == SettingsSection.App)
            {
                _appSettingsControlViewModel.SelectedAppSettingsSection = (AppSettingsSection)item.Subsection;
                _appSettingsControlViewModel.IsDeepLinkRequested = true;
            }
            else if (item.Section == SettingsSection.PlaybackLib)
            {
                _playbackSettingsControlViewModel.SelectedPlaybackLibSettingsSection = (PlaybackLibSettingsSection)item.Subsection;

                if (item.TargetParameter is string providerName)
                {
                    var provider = _playbackSettingsControlViewModel.AppSettings.MediaSourceProvidersInfo.FirstOrDefault(p => p.Provider == providerName);
                    if (provider != null)
                    {
                        _playbackSettingsControlViewModel.SelectedMediaSourceProvider = provider;
                    }
                }

                _playbackSettingsControlViewModel.IsDeepLinkRequested = true;
            }
            else if (item.Section == SettingsSection.LyricsWindowMgr)
            {
                _lyricsWindowSettingsControlViewModel.SelectedLyricsWindowManagerSettingsSection = (LyricsWindowManagerSettingsSection)item.Subsection;

                LyricsWindowStatus? status = null;
                if (item.TargetParameter is Guid windowId)
                {
                    status = _lyricsWindowSettingsControlViewModel.AppSettings.WindowBoundsRecords.FirstOrDefault(w => w.Id == windowId);
                }

                if (status == null)
                {
                    status = _lyricsWindowSettingsControlViewModel.AppSettings.WindowBoundsRecords.FirstOrDefault() ?? _lyricsWindowSettingsControlViewModel.AppSettings.MusicGallerySettings.LyricsWindowStatus;
                }

                if (status != null)
                {
                    _lyricsWindowSettingsControlViewModel.SelectedWindowStatus = status;
                    _lyricsWindowSettingsControlViewModel.IsDeepLinkRequested = true;
                }
            }
        }
    }

    [RelayCommand]
    private async Task CopyDeepLinkAsync()
    {
        if (!string.IsNullOrEmpty(CurrentDeepLink))
        {
            try
            {
                await TextCopy.ClipboardService.SetTextAsync(CurrentDeepLink);
                _globalToastProvider.Show("ActionCompleted", null, MessageSeverity.Success);
            }
            catch (Exception ex)
            {
                _globalToastProvider.Show("Error", ex.Message, MessageSeverity.Error);
            }
        }
    }
}