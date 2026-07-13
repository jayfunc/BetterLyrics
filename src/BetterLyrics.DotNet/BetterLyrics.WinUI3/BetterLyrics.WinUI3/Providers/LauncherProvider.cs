using BetterLyrics.Core.Interfaces.Providers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

namespace BetterLyrics.WinUI3.Providers;

public class LauncherProvider : ILauncherProvider
{
    public async Task LaunchUriAsync(Uri uri)
    {
        await Launcher.LaunchUriAsync(uri);
    }

    public async Task LaunchFolderPathAsync(string folderPath)
    {
        await Launcher.LaunchFolderPathAsync(folderPath);
    }

    public async Task SelectAndShowFileAsync(string filePath)
    {
        var file = await StorageFile.GetFileFromPathAsync(filePath);
        var folder = await file.GetParentAsync();

        var folderOptions = new FolderLauncherOptions();
        folderOptions.ItemsToSelect.Add(file);

        await Launcher.LaunchFolderAsync(folder, folderOptions);
    }
}
