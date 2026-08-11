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

                // 触发所有剩余的媒体库进行一次自动扫描，以确保任何重叠区域的接管都被处理
                foreach (var remainingFolder in AppSettings.LocalMediaFolders)
                {
                    _ = Task.Run(async () => await _fileSystemService.ScanMediaFolderAsync(remainingFolder));
                }
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
                // 取消所有嵌套拦截，仅在文件系统扫描时通过比对是否是独立媒体库来自动跳过
                // 这样用户可以随意添加父子目录，分别应用不同的匹配规则，且互不干扰

                AppSettings.LocalMediaFolders.Add(tempFolder);
                foreach (var f in AppSettings.LocalMediaFolders)
                {
                    _ = Task.Run(async () => await _fileSystemService.ScanMediaFolderAsync(f));
                }

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
                        
                    foreach (var f in AppSettings.LocalMediaFolders)
                    {
                        _ = Task.Run(async () => await _fileSystemService.ScanMediaFolderAsync(f));
                    }
                    
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
