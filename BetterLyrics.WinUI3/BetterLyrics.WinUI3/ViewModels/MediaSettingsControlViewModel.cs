using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.ResourceService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class MediaSettingsControlViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;
        private readonly IResourceService _resourceService;

        [ObservableProperty]
        public partial AppSettings AppSettings { get; set; }

        public MediaSettingsControlViewModel(ISettingsService settingsService, IResourceService resourceService)
        {
            _settingsService = settingsService;
            _resourceService = resourceService;
            AppSettings = _settingsService.AppSettings;
        }

        [RelayCommand]
        private async Task SelectAndAddFolderAsync(UIElement sender)
        {
            var folder = await PickerHelper.PickSingleFolderAsync<SettingsWindow>();

            if (folder != null)
            {
                AddFolderAsync(folder.Path);
            }
        }

        private void AddFolderAsync(string path)
        {
            var normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            if (AppSettings.LocalMediaFolders.Any(x => Path.GetFullPath(x.Path).TrimEnd(Path.DirectorySeparatorChar).Equals(normalizedPath.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)))
            {
                DevWinUI.Growl.Warning(_resourceService.GetLocalizedString("SettingsPagePathExistedInfo"));
            }
            else if (AppSettings.LocalMediaFolders.Any(item => normalizedPath.StartsWith(Path.GetFullPath(item.Path).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
            {
                // 添加的文件夹是现有文件夹的子文件夹
                DevWinUI.Growl.Warning(_resourceService.GetLocalizedString("SettingsPagePathBeIncludedInfo"));
            }
            else if (AppSettings.LocalMediaFolders.Any(item => Path.GetFullPath(item.Path).TrimEnd(Path.DirectorySeparatorChar).StartsWith(normalizedPath, StringComparison.OrdinalIgnoreCase))
            )
            {
                // 添加的文件夹是现有文件夹的父文件夹
                DevWinUI.Growl.Warning(_resourceService.GetLocalizedString("SettingsPagePathIncludingOthersInfo"));
            }
            else
            {
                AppSettings.LocalMediaFolders.Add(new LocalMediaFolder(path));
            }
        }

        public void RemoveFolderAsync(LocalMediaFolder folder)
        {
            AppSettings.LocalMediaFolders.Remove(folder);
        }
    }
}
