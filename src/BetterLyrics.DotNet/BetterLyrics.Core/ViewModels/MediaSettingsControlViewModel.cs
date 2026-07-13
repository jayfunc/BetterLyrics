using System.Diagnostics;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BetterLyrics.Core.ViewModels;

public partial class MediaSettingsControlViewModel : BaseViewModel
{
    private readonly IAppUIThreadProvider _appUIThreadProvider;
    private readonly IFileSystemService _fileSystemService;
    private readonly ILocalizationService _localizationService;
    private readonly IPasswordVaultProvider _passwordVaultProvider;
    private readonly ISettingsService _settingsService;
    private readonly IAddMediaSourceDialogProvider _addMediaSourceDialogProvider;

    public MediaSettingsControlViewModel(
        ISettingsService settingsService,
        ILocalizationService localizationService,
        IFileSystemService fileSystemService,
        INavigationService navigationService, IAppUIThreadProvider appUiThreadProvider,
        IAddMediaSourceDialogProvider addMediaSourceDialogProvider,
        IPasswordVaultProvider passwordVaultProvider)
    {
        _localizationService = localizationService;
        _settingsService = settingsService;
        _fileSystemService = fileSystemService;
        _passwordVaultProvider = passwordVaultProvider;

        NavigationService = navigationService;
        _appUIThreadProvider = appUiThreadProvider;
        _addMediaSourceDialogProvider = addMediaSourceDialogProvider;
        AppSettings = _settingsService.AppSettings;
    }

    public INavigationService NavigationService { get; }
    [ObservableProperty] public partial AppSettings AppSettings { get; set; }

    public void RemoveFolder(MediaFolder folder)
    {
        _ = Task.Run(async () =>
        {
            await _fileSystemService.DeleteCacheForMediaFolderAsync(folder);
            _appUIThreadProvider.Execute(() =>
            {
                AppSettings.LocalMediaFolders.Remove(folder);
                _passwordVaultProvider.Delete(Core.Constants.App.AppName, folder.VaultKey);
            });
        });
    }

    public void SyncFolder(MediaFolder folder, bool forceSync)
    {
        if (folder.IsProcessing) return;

        _ = Task.Run(async () =>
            await _fileSystemService.ScanMediaFolderAsync(folder, forceSync, CancellationToken.None));
    }

    [RelayCommand]
    private async Task AddMediaSourceAsync(string fileSourceTypeName)
    {
        var fileSourceType = Enum.Parse<FileSourceType>(fileSourceTypeName);

        await _addMediaSourceDialogProvider.ShowDialogAsync(fileSourceType, async tempFolder =>
        {
            if (fileSourceType == FileSourceType.Local)
            {
                var path = tempFolder.UriPath;

                if (!Directory.Exists(path))
                    return (false, _localizationService.GetLocalizedString("RemoteServerConfigControlPathNotExisted"));

                var normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar) +
                                     Path.DirectorySeparatorChar;

                // 是否完全重复
                if (AppSettings.LocalMediaFolders.Any(x =>
                        Path.GetFullPath(x.UriPath).TrimEnd(Path.DirectorySeparatorChar)
                            .Equals(normalizedPath.TrimEnd(Path.DirectorySeparatorChar),
                                StringComparison.OrdinalIgnoreCase)))
                {
                    return (false, _localizationService.GetLocalizedString("SettingsPagePathExistedInfo"));
                }
                // 是否是子文件夹
                else if (AppSettings.LocalMediaFolders.Any(item =>
                             normalizedPath.StartsWith(
                                 Path.GetFullPath(item.UriPath).TrimEnd(Path.DirectorySeparatorChar) +
                                 Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
                {
                    return (false, _localizationService.GetLocalizedString("SettingsPagePathBeIncludedInfo"));
                }
                // 是否是父文件夹
                else if (AppSettings.LocalMediaFolders.Any(item =>
                             Path.GetFullPath(item.UriPath).TrimEnd(Path.DirectorySeparatorChar)
                                 .StartsWith(normalizedPath, StringComparison.OrdinalIgnoreCase)))
                {
                    return (false, _localizationService.GetLocalizedString("SettingsPagePathIncludingOthersInfo"));
                }

                AppSettings.LocalMediaFolders.Add(tempFolder);
                _ = Task.Run(async () => await _fileSystemService.ScanMediaFolderAsync(tempFolder));

                return (true, null);
            }
            else
            {
                if (fileSourceType == FileSourceType.WebDAV)
                {
                    // 使用辅助类探测协议
                    var detectedScheme = await WebDavProbeHelper.DetectSchemeAsync(
                        tempFolder.UriHost,
                        tempFolder.UriPort,
                        tempFolder.UriPath,
                        tempFolder.UserName,
                        tempFolder.Password
                    );

                    if (detectedScheme == null)
                    {
                        // 探测失败，直接报错返回
                        return (false, _localizationService.GetLocalizedString("SettingsPageServerTestFailedInfo"));
                    }

                    // 将探测到的正确协议 (http 或 https) 写入配置对象
                    tempFolder.UriScheme = detectedScheme;
                }

                var newUriString = tempFolder.GetStandardUri().AbsoluteUri.TrimEnd('/') + "/";

                foreach (var existingFolder in AppSettings.LocalMediaFolders)
                {
                    var existingUriString = existingFolder.GetStandardUri().AbsoluteUri.TrimEnd('/') + "/";

                    // 是否完全重复 (忽略大小写)
                    if (newUriString.Equals(existingUriString, StringComparison.OrdinalIgnoreCase))
                    {
                        return (false, _localizationService.GetLocalizedString("SettingsPagePathExistedInfo"));
                    }

                    // 新路径是否是现有路径的“子文件夹”
                    if (newUriString.StartsWith(existingUriString, StringComparison.OrdinalIgnoreCase))
                    {
                        return (false, _localizationService.GetLocalizedString("SettingsPagePathBeIncludedInfo"));
                    }

                    // 新路径是否是现有路径的“父文件夹”
                    if (existingUriString.StartsWith(newUriString, StringComparison.OrdinalIgnoreCase))
                    {
                        return (false, _localizationService.GetLocalizedString("SettingsPagePathIncludingOthersInfo"));
                    }
                }

                var isConnected = await Task.Run(async () =>
                {
                    try
                    {
                        using var provider = tempFolder.CreateFileSystem();
                        if (provider == null) return false;
                        return await provider.ConnectAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                        return false;
                    }
                });

                if (isConnected)
                {
                    AppSettings.LocalMediaFolders.Add(tempFolder);
                    _passwordVaultProvider.Save(Core.Constants.App.AppName, tempFolder.VaultKey,
                        tempFolder.Password);
                    _ = Task.Run(async () => await _fileSystemService.ScanMediaFolderAsync(tempFolder));
                    return (true, null);
                }
                else
                {
                    return (false, _localizationService.GetLocalizedString("SettingsPageServerTestFailedInfo"));
                }
            }
        });
    }
}
