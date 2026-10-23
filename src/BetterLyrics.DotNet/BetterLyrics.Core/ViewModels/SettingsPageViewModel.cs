// 2025/6/23 by Zhe Fang

using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Interfaces.ViewModels;
using BetterLyrics.Core.Messages.SettingsNavigation;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;

namespace BetterLyrics.Core.ViewModels;

/// <summary>
/// Singleton ViewModel
/// </summary>
public partial class SettingsPageViewModel : ObservableRecipient, ISingletonViewModel,
    IRecipient<SettingsSectionChangedMessage>
{
    private readonly ILocalizationService _localizationService;
    private readonly IGlobalToastProvider _globalToastProvider;
    private readonly ISettingsService _settingsService;

    private readonly Debouncer _searchDebouncer = new();

    // 各设置控件通过 SettingsSectionChangedMessage 上报的当前路径状态，
    // 避免本 ViewModel 直接依赖控件的 ViewModel 实例（否则它们无法成为 transient）。
    private AppSettingsSection? _appSubsection;
    private PlaybackLibSettingsSection? _playbackSubsection;
    private string? _playbackProvider;
    private LyricsWindowManagerSettingsSection? _lyricsWindowSubsection;
    private LyricsWindowStatus? _selectedLyricsWindowStatus;

    [ObservableProperty] public partial SettingsNavMenuItem? SelectedNavItem { get; set; }

    [ObservableProperty] public partial string CurrentPath { get; set; } = string.Empty;
    [ObservableProperty] public partial string CurrentDeepLink { get; set; } = string.Empty;

    [ObservableProperty] public partial string SearchQuery { get; set; } = string.Empty;

    public ObservableCollection<SettingsNavMenuItem> NavItems { get; } = [];

    public ObservableCollection<SettingSearchItem> FilteredSettings { get; } = [];

    [ObservableProperty] public partial bool IsSearching { get; set; }

    public SettingsPageViewModel(
        ILocalizationService localizationService,
        IGlobalToastProvider globalToastProvider,
        ISettingsService settingsService)
    {
        _localizationService = localizationService;
        _globalToastProvider = globalToastProvider;
        _settingsService = settingsService;

        NavItems.Add(new SettingsNavMenuItem
        {
            Label = _localizationService.GetLocalizedString("SettingsPageApp"),
            Glyph = "\uECAA",
            Section = SettingsSection.App
        });
        NavItems.Add(new SettingsNavMenuItem
        {
            Label = _localizationService.GetLocalizedString("SettingsPageLyricsWindowMgr"),
            Glyph = "\uE61F",
            Section = SettingsSection.LyricsWindowMgr
        });
        NavItems.Add(new SettingsNavMenuItem
        {
            Label = _localizationService.GetLocalizedString("SettingsPageMediaLib"),
            Glyph = "\uE8B7",
            Section = SettingsSection.MediaLib
        });
        NavItems.Add(new SettingsNavMenuItem
        {
            Label = _localizationService.GetLocalizedString("SettingsPagePlaybackLib"),
            Glyph = "\uEA69",
            Section = SettingsSection.PlaybackLib
        });
        NavItems.Add(new SettingsNavMenuItem
        {
            Label = _localizationService.GetLocalizedString("SettingsPagePlugins"),
            Glyph = "\uE74C",
            Section = SettingsSection.Plugins
        });
        NavItems.Add(new SettingsNavMenuItem
        {
            Label = _localizationService.GetLocalizedString("SettingsPageAbout"),
            Glyph = "\uE946",
            Section = SettingsSection.About
        });
        SelectedNavItem = NavItems[0];

        UpdateCurrentPath();

        IsActive = true;
    }

    partial void OnSelectedNavItemChanged(SettingsNavMenuItem? value)
    {
        UpdateCurrentPath();
    }

    public void UpdateCurrentPath()
    {
        if (SelectedNavItem == null) return;

        var displayPathSegments = new List<string> { GetLocalizedTitle("SettingsPageTitle"), GetLocalizedSectionName(SelectedNavItem.Section) };
        var uriSegments = new List<string> { "betterlyrics://settings", SelectedNavItem.Section.ToString().ToLowerInvariant() };

        if (SelectedNavItem.Section == SettingsSection.App)
        {
            uriSegments.Add(_appSubsection?.ToString().ToLowerInvariant() ?? "");

            displayPathSegments.Add(GetLocalizedSubsectionName(_appSubsection));
        }
        else if (SelectedNavItem.Section == SettingsSection.PlaybackLib)
        {
            string subsectionName = GetLocalizedSubsectionName(_playbackSubsection);

            if (_playbackSubsection == PlaybackLibSettingsSection.General && _playbackProvider != null)
            {
                uriSegments.Add(_playbackSubsection?.ToString().ToLowerInvariant() ?? "");
                uriSegments.Add(_playbackProvider);

                displayPathSegments.Add(subsectionName);
                displayPathSegments.Add(GetLocalizedTitle(_playbackProvider));
            }
            else
            {
                uriSegments.Add(_playbackSubsection?.ToString().ToLowerInvariant() ?? "");
                displayPathSegments.Add(subsectionName);
            }
        }
        else if (SelectedNavItem.Section == SettingsSection.LyricsWindowMgr)
        {
            if (_selectedLyricsWindowStatus != null)
            {
                uriSegments.Add(_selectedLyricsWindowStatus.Id.ToString());
                uriSegments.Add(_lyricsWindowSubsection?.ToString().ToLowerInvariant() ?? "");

                displayPathSegments.Add(_selectedLyricsWindowStatus.Name);
                displayPathSegments.Add(GetLocalizedSubsectionName(_lyricsWindowSubsection));
            }
        }

        CurrentPath = string.Join(" > ", displayPathSegments);
        CurrentDeepLink = string.Join("/", uriSegments);
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
                                foreach (var window in _settingsService.AppSettings.WindowBoundsRecords)
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
                                foreach (var provider in _settingsService.AppSettings.MediaSourceProvidersInfo)
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
        var targetItem = NavItems.FirstOrDefault(m => m.Section == section);

        if (targetItem != null) SelectedNavItem = targetItem;
    }

    public void NavigateToSettingSearchItem(SettingSearchItem item)
    {
        NavigateToSection(item.Section);

        if (item.Section == SettingsSection.App)
        {
            if (item.Subsection != null)
            {
                WeakReferenceMessenger.Default.Send(new AppSettingsNavigationRequestedMessage((AppSettingsSection)item.Subsection));
            }
        }
        else if (item.Section == SettingsSection.PlaybackLib)
        {
            WeakReferenceMessenger.Default.Send(new PlaybackLibSettingsNavigationRequestedMessage(
                (PlaybackLibSettingsSection)(item.Subsection ?? PlaybackLibSettingsSection.General),
                item.TargetParameter as string
            ));
        }
        else if (item.Section == SettingsSection.LyricsWindowMgr)
        {
            WeakReferenceMessenger.Default.Send(new LyricsWindowNavigationRequestedMessage(
                (LyricsWindowManagerSettingsSection)(item.Subsection ?? LyricsWindowManagerSettingsSection.Window),
                item.TargetParameter as Guid?
            ));
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

    public void Receive(SettingsSectionChangedMessage message)
    {
        switch (message.Section)
        {
            case SettingsSection.App:
                _appSubsection = message.Subsection as AppSettingsSection?;
                break;
            case SettingsSection.PlaybackLib:
                _playbackSubsection = message.Subsection as PlaybackLibSettingsSection?;
                _playbackProvider = message.Parameter as string;
                break;
            case SettingsSection.LyricsWindowMgr:
                _lyricsWindowSubsection = message.Subsection as LyricsWindowManagerSettingsSection?;
                _selectedLyricsWindowStatus = message.Parameter as LyricsWindowStatus;
                break;
        }

        UpdateCurrentPath();
    }
}