// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.LocalizationService;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class SettingsPageViewModel : BaseViewModel
    {
        private readonly ILocalizationService _localizationService;

        [ObservableProperty]
        public partial NavMenuItem? SelectedMenuItem { get; set; }

        public ObservableCollection<NavMenuItem> MenuItems { get; } = [];

        public SettingsPageViewModel(ILocalizationService localizationService)
        {
            _localizationService = localizationService;

            MenuItems.Add(new NavMenuItem { Label = _localizationService.GetLocalizedString("SettingsPageApp"), Glyph = "\uECAA", Section = SettingsSection.App });
            MenuItems.Add(new NavMenuItem { Label = _localizationService.GetLocalizedString("SettingsPageLyricsWindowMgr"), Glyph = "\uE61F", Section = SettingsSection.LyricsWindowMgr });
            MenuItems.Add(new NavMenuItem { Label = _localizationService.GetLocalizedString("SettingsPageMediaLib"), Glyph = "\uE8B7", Section = SettingsSection.MediaLib });
            MenuItems.Add(new NavMenuItem { Label = _localizationService.GetLocalizedString("SettingsPagePlaybackLib"), Glyph = "\uEA69", Section = SettingsSection.PlaybackLib });
            MenuItems.Add(new NavMenuItem { Label = _localizationService.GetLocalizedString("SettingsPageStats"), Glyph = "\uE9D2", Section = SettingsSection.Stats });
            MenuItems.Add(new NavMenuItem { Label = _localizationService.GetLocalizedString("SettingsPagePlugins"), Glyph = "\uE74C", Section = SettingsSection.Plugins });
            MenuItems.Add(new NavMenuItem { Label = _localizationService.GetLocalizedString("SettingsPageAbout"), Glyph = "\uE946", Section = SettingsSection.About });

            SelectedMenuItem = MenuItems[0];
        }

        public void NavigateToSection(SettingsSection section)
        {
            var targetItem = MenuItems.FirstOrDefault(m => m.Section == section);

            if (targetItem != null)
            {
                SelectedMenuItem = targetItem;
            }
        }

    }
}
