using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

namespace BetterLyrics.WinUI3.Helper
{
    public class LauncherHelper
    {
        public static async Task SelectAndShowFile(string filePath)
        {
            var file = await StorageFile.GetFileFromPathAsync(filePath);
            var folder = await file.GetParentAsync();

            var folderOptions = new FolderLauncherOptions();
            folderOptions.ItemsToSelect.Add(file);

            await Launcher.LaunchFolderAsync(folder, folderOptions);
        }
    }
}
