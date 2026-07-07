namespace BetterLyrics.Core.Interfaces.Providers;

public interface ILauncherProvider
{
    Task SelectAndShowFileAsync(string filePath);
    Task LaunchUriAsync(Uri uri);
    Task LaunchFolderPathAsync(string folderPath);
}
