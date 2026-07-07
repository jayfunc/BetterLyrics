using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using BetterLyrics.Core.Interfaces.Providers;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BetterLyrics.Avalonia.Providers;

public class LauncherProvider : ILauncherProvider
{
    public async Task SelectAndShowFileAsync(string filePath)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var launcher = TopLevel.GetTopLevel(desktop.MainWindow)?.Launcher;
            launcher?.LaunchFileInfoAsync(new FileInfo(filePath));
        }
    }

    public async Task LaunchUriAsync(Uri uri)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var launcher = TopLevel.GetTopLevel(desktop.MainWindow)?.Launcher;
            launcher?.LaunchUriAsync(uri);
        }
    }

    public async Task LaunchFolderPathAsync(string folderPath)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var launcher = TopLevel.GetTopLevel(desktop.MainWindow)?.Launcher;
            launcher?.LaunchDirectoryInfoAsync(new DirectoryInfo(folderPath));
        }
    }
}