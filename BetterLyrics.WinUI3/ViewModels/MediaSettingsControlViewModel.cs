using BetterLyrics.WinUI3.Controls;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.FileSystemService;
using BetterLyrics.WinUI3.Services.LocalizationService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class MediaSettingsControlViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;
        private readonly ILocalizationService _localizationService;
        private readonly IFileSystemService _fileSystemService;

        [ObservableProperty] public partial AppSettings AppSettings { get; set; }

        public MediaSettingsControlViewModel(ISettingsService settingsService, ILocalizationService localizationService, IFileSystemService fileSystemService)
        {
            _localizationService = localizationService;
            _settingsService = settingsService;
            _fileSystemService = fileSystemService;

            AppSettings = _settingsService.AppSettings;
        }

        public void RemoveFolder(MediaFolder folder)
        {
            _ = Task.Run(async () =>
            {
                await _fileSystemService.DeleteCacheForMediaFolderAsync(folder);
                _dispatcherQueue.TryEnqueue(() =>
                {
                    AppSettings.LocalMediaFolders.Remove(folder);
                    PasswordVaultHelper.Delete(Constants.App.AppName, folder.VaultKey);
                });
            });
        }

        public void SyncFolder(MediaFolder folder, bool forceSync)
        {
            if (folder.IsProcessing) return;

            _ = Task.Run(async () => await _fileSystemService.ScanMediaFolderAsync(folder, forceSync, CancellationToken.None));
        }

        [RelayCommand]
        private async Task AddMediaSourceAsync(string fileSourceTypeName)
        {
            FileSourceType fileSourceType = Enum.Parse<FileSourceType>(fileSourceTypeName);

            var dialog = new ContentDialog
            {
                XamlRoot = WindowHook.GetWindow<SettingsWindow>()?.Content.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = fileSourceType == FileSourceType.Local ? _localizationService.GetLocalizedString("MediaSettingsControlLocalFolder") : Enum.GetName(fileSourceType),
                PrimaryButtonText = _localizationService.GetLocalizedString("Add"),
                CloseButtonText = _localizationService.GetLocalizedString("Cancel"),
                DefaultButton = ContentDialogButton.Primary,
                Content = new RemoteServerConfigControl(fileSourceType)
            };

            dialog.PrimaryButtonClick += async (s, e) =>
            {
                var configControl = (RemoteServerConfigControl)dialog.Content;
                var deferral = e.GetDeferral();
                e.Cancel = true; // 默认阻止关闭，直到验证通过

                dialog.IsPrimaryButtonEnabled = false;
                configControl.IsEnabled = false;
                configControl.SetProgressBarVisibility(Visibility.Visible);
                // 清除之前的错误信息
                configControl.ShowError(null);

                try
                {
                    var tempFolder = configControl.GetConfig();

                    if (fileSourceType == FileSourceType.Local)
                    {
                        string path = tempFolder.UriPath;

                        if (!Directory.Exists(path))
                        {
                            throw new DirectoryNotFoundException(_localizationService.GetLocalizedString("RemoteServerConfigControlPathNotExisted"));
                        }

                        var normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

                        // 是否完全重复
                        if (AppSettings.LocalMediaFolders.Any(x => Path.GetFullPath(x.UriPath).TrimEnd(Path.DirectorySeparatorChar).Equals(normalizedPath.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)))
                        {
                            configControl.ShowError(_localizationService.GetLocalizedString("SettingsPagePathExistedInfo"));
                            deferral.Complete();
                            return;
                        }
                        // 是否是子文件夹
                        else if (AppSettings.LocalMediaFolders.Any(item => normalizedPath.StartsWith(Path.GetFullPath(item.UriPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
                        {
                            configControl.ShowError(_localizationService.GetLocalizedString("SettingsPagePathBeIncludedInfo"));
                            deferral.Complete();
                            return;
                        }
                        // 是否是父文件夹
                        else if (AppSettings.LocalMediaFolders.Any(item => Path.GetFullPath(item.UriPath).TrimEnd(Path.DirectorySeparatorChar).StartsWith(normalizedPath, StringComparison.OrdinalIgnoreCase)))
                        {
                            configControl.ShowError(_localizationService.GetLocalizedString("SettingsPagePathIncludingOthersInfo"));
                            deferral.Complete();
                            return;
                        }

                        AppSettings.LocalMediaFolders.Add(tempFolder);
                        _ = Task.Run(async () => await _fileSystemService.ScanMediaFolderAsync(tempFolder));

                        e.Cancel = false; // 允许关闭
                    }
                    else
                    {
                        if (fileSourceType == FileSourceType.WebDAV)
                        {
                            // 使用辅助类探测协议
                            string? detectedScheme = await WebDavProbeHelper.DetectSchemeAsync(
                                tempFolder.UriHost,
                                tempFolder.UriPort,
                                tempFolder.UriPath,
                                tempFolder.UserName,
                                tempFolder.Password
                            );

                            if (detectedScheme == null)
                            {
                                // 探测失败，直接报错返回
                                configControl.ShowError(_localizationService.GetLocalizedString("SettingsPageServerTestFailedInfo"));
                                deferral.Complete();
                                return;
                            }

                            // 将探测到的正确协议 (http 或 https) 写入配置对象
                            tempFolder.UriScheme = detectedScheme;
                        }

                        var newUriString = tempFolder.GetStandardUri().AbsoluteUri.TrimEnd('/') + "/";

                        foreach (var existingFolder in AppSettings.LocalMediaFolders)
                        {
                            // 只比对同类型的远程源 (可选，或者是比对所有源)
                            // 这里建议比对所有，防止逻辑上的冲突

                            var existingUriString = existingFolder.GetStandardUri().AbsoluteUri.TrimEnd('/') + "/";

                            // 是否完全重复 (忽略大小写)
                            if (newUriString.Equals(existingUriString, StringComparison.OrdinalIgnoreCase))
                            {
                                configControl.ShowError(_localizationService.GetLocalizedString("SettingsPagePathExistedInfo"));
                                deferral.Complete();
                                return;
                            }

                            // 新路径是否是现有路径的“子文件夹”
                            if (newUriString.StartsWith(existingUriString, StringComparison.OrdinalIgnoreCase))
                            {
                                configControl.ShowError(_localizationService.GetLocalizedString("SettingsPagePathBeIncludedInfo"));
                                deferral.Complete();
                                return;
                            }

                            // 新路径是否是现有路径的“父文件夹”
                            if (existingUriString.StartsWith(newUriString, StringComparison.OrdinalIgnoreCase))
                            {
                                configControl.ShowError(_localizationService.GetLocalizedString("SettingsPagePathIncludingOthersInfo"));
                                deferral.Complete();
                                return;
                            }
                        }

                        bool isConnected = await Task.Run(async () =>
                        {
                            try
                            {
                                using var provider = tempFolder.CreateFileSystem();
                                if (provider == null) return false;
                                return await provider.ConnectAsync();
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine(ex);
                                return false;
                            }
                        });

                        if (isConnected)
                        {
                            AppSettings.LocalMediaFolders.Add(tempFolder);
                            PasswordVaultHelper.Save(Constants.App.AppName, tempFolder.VaultKey, tempFolder.Password);
                            _ = Task.Run(async () => await _fileSystemService.ScanMediaFolderAsync(tempFolder));
                            e.Cancel = false; // 允许关闭
                        }
                        else
                        {
                            configControl.ShowError(_localizationService.GetLocalizedString("SettingsPageServerTestFailedInfo"));
                        }
                    }
                }
                catch (Exception ex)
                {
                    configControl.ShowError(ex.Message);
                }
                finally
                {
                    if (e.Cancel)
                    {
                        dialog.IsPrimaryButtonEnabled = true;
                        configControl.IsEnabled = true;
                        configControl.SetProgressBarVisibility(Visibility.Collapsed);
                    }
                }

                deferral.Complete();
            };

            await dialog.ShowAsync();
        }

        [RelayCommand]
        private void OpenMusicGalleryWindow()
        {
            WindowHook.OpenOrShowWindow<MusicGalleryWindow>();
        }

    }
}
