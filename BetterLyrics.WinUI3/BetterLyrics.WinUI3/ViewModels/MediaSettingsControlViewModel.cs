using BetterLyrics.WinUI3.Controls;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.ResourceService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;

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

        private void AddFolderAsync(string path)
        {
            var normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            if (AppSettings.LocalMediaFolders.Any(x => Path.GetFullPath(x.Path).TrimEnd(Path.DirectorySeparatorChar).Equals(normalizedPath.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)))
            {
                ToastHelper.ShowToast("SettingsPagePathExistedInfo", null, InfoBarSeverity.Warning);
            }
            else if (AppSettings.LocalMediaFolders.Any(item => normalizedPath.StartsWith(Path.GetFullPath(item.Path).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
            {
                // 添加的文件夹是现有文件夹的子文件夹
                ToastHelper.ShowToast("SettingsPagePathBeIncludedInfo", null, InfoBarSeverity.Warning);
            }
            else if (AppSettings.LocalMediaFolders.Any(item => Path.GetFullPath(item.Path).TrimEnd(Path.DirectorySeparatorChar).StartsWith(normalizedPath, StringComparison.OrdinalIgnoreCase))
            )
            {
                // 添加的文件夹是现有文件夹的父文件夹
                ToastHelper.ShowToast("SettingsPagePathIncludingOthersInfo", null, InfoBarSeverity.Warning);
            }
            else
            {
                AppSettings.LocalMediaFolders.Add(new MediaFolder(path));
            }
        }

        public void RemoveFolderAsync(MediaFolder folder)
        {
            AppSettings.LocalMediaFolders.Remove(folder);
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

        [RelayCommand]
        private async Task AddRemoteSourceAsync(string protocolType)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = WindowHook.GetWindow<SettingsWindow>()?.Content.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = protocolType,
                PrimaryButtonText = _resourceService.GetLocalizedString("Add"),
                CloseButtonText = _resourceService.GetLocalizedString("Cancel"),
                DefaultButton = ContentDialogButton.Primary,
                Content = new RemoteServerConfigControl(protocolType)
            };

            dialog.PrimaryButtonClick += async (s, e) =>
            {
                var configControl = (RemoteServerConfigControl)dialog.Content;

                try
                {
                    e.Cancel = true;

                    dialog.IsPrimaryButtonEnabled = false;
                    configControl.IsEnabled = false;
                    dialog.Title = $"Connecting to {protocolType}...";

                    var tempFolder = configControl.GetConfig();

                    var provider = tempFolder.CreateFileSystem();

                    bool isConnected = provider != null && await provider.ConnectAsync();

                    if (isConnected)
                    {
                        await provider!.DisconnectAsync();

                        PasswordVaultHelper.Save(Constants.App.AppName, tempFolder.VaultKey, tempFolder.Password);
                        AppSettings.LocalMediaFolders.Add(tempFolder);

                        e.Cancel = false;
                    }
                    else
                    {
                        ShowErrorTip(configControl, "Connection failed. Check IP/Port.");
                    }
                }
                catch (Exception ex)
                {
                    ShowErrorTip(configControl, $"Error: {ex.Message}");
                }
                finally
                {
                    dialog.IsPrimaryButtonEnabled = true;
                    configControl.IsEnabled = true;
                    dialog.Title = protocolType;
                }
            };

            await dialog.ShowAsync();
        }

        private void ShowErrorTip(RemoteServerConfigControl control, string message)
        {
            // 你可以在 RemoteServerConfigControl 里加一个 InfoBar 用来显示错误
            // 假设你在 UserControl 里公开了一个 ShowError 方法
            control.ShowError(message);
        }

    }
}
