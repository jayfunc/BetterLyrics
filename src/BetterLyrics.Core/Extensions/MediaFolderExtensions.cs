using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Implementations.Services.FileSystemService.Providers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace BetterLyrics.Core.Extensions;

public static class MediaFolderExtensions
{
    extension(MediaFolder mediaFolder)
    {
        public IUnifiedFileSystem? CreateFileSystem()
        {
            if (!mediaFolder.IsEnabled) return null;
            if (string.IsNullOrEmpty(mediaFolder.Password) && !mediaFolder.IsLocal)
                mediaFolder.Password = Ioc.Default.GetRequiredService<IPasswordVaultProvider>()
                    .Get(App.AppName, mediaFolder.VaultKey) ?? "";

            return mediaFolder.SourceType switch
            {
                FileSourceType.Local => new LocalFileSystem(mediaFolder),
                FileSourceType.SMB => new SMBFileSystem(mediaFolder),
                FileSourceType.FTP => new FTPFileSystem(mediaFolder),
                FileSourceType.WebDAV => new WebDavFileSystem(mediaFolder),
                _ => throw new NotImplementedException()
            };
        }
    }
}