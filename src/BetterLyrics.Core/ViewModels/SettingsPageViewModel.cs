// 2025/6/23 by Zhe Fang

using System.Collections.ObjectModel;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.ViewModels;

public partial class SettingsPageViewModel : BaseViewModel
{
    private readonly ILocalizationService _localizationService;

    public SettingsPageViewModel(ILocalizationService localizationService)
    {
        _localizationService = localizationService;

        MenuItems.Add(new NavMenuItem
        {
            Label = _localizationService.GetLocalizedString("SettingsPageApp"), Glyph = "\uECAA",
            Section = SettingsSection.App
        });
        MenuItems.Add(new NavMenuItem
        {
            Label = _localizationService.GetLocalizedString("SettingsPageLyricsWindowMgr"), Glyph = "\uE61F",
            Section = SettingsSection.LyricsWindowMgr
        });
        MenuItems.Add(new NavMenuItem
        {
            Label = _localizationService.GetLocalizedString("SettingsPageMediaLib"), Glyph = "\uE8B7",
            Section = SettingsSection.MediaLib
        });
        MenuItems.Add(new NavMenuItem
        {
            Label = _localizationService.GetLocalizedString("SettingsPagePlaybackLib"), Glyph = "\uEA69",
            Section = SettingsSection.PlaybackLib
        });
        MenuItems.Add(new NavMenuItem
        {
            Label = _localizationService.GetLocalizedString("SettingsPagePlugins"), Glyph = "\uE74C",
            Section = SettingsSection.Plugins
        });
        MenuItems.Add(new NavMenuItem
        {
            Label = _localizationService.GetLocalizedString("SettingsPageAbout"), Glyph = "\uE946",
            Section = SettingsSection.About
        });

        SelectedMenuItem = MenuItems[0];
    }

    [ObservableProperty] public partial NavMenuItem? SelectedMenuItem { get; set; }

    public ObservableCollection<NavMenuItem> MenuItems { get; } = [];

    public void NavigateToSection(SettingsSection section)
    {
        var targetItem = MenuItems.FirstOrDefault(m => m.Section == section);

        if (targetItem != null) SelectedMenuItem = targetItem;
    }
}