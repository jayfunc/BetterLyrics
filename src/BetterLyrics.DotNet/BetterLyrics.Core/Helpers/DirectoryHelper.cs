namespace BetterLyrics.Core.Helpers;

public class DirectoryHelper
{
    /// <summary>
    ///     递归查找指定文件夹下所有文件（包括子文件夹）。
    /// </summary>
    /// <param name="folderPath">要查找的文件夹路径</param>
    /// <returns>所有文件的完整路径列表</returns>
    public static List<string> GetAllFiles(string folderPath, string searchPattern = "*")
    {
        var files = new List<string>();
        if (!Directory.Exists(folderPath))
            return files;

        try
        {
            files.AddRange(Directory.GetFiles(folderPath, searchPattern));
            foreach (var dir in Directory.GetDirectories(folderPath)) files.AddRange(GetAllFiles(dir, searchPattern));
        }
        catch (Exception)
        {
            // 可根据需要处理异常，如权限不足等
        }

        return files;
    }

    public static void DeleteAllFiles(string folderPath)
    {
        if (!Directory.Exists(folderPath)) return;

        var di = new DirectoryInfo(folderPath);

        try
        {
            foreach (var file in di.GetFiles())
                try
                {
                    file.Delete();
                }
                catch (Exception ex)
                {
                }
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    ///     https://learn.microsoft.com/zh-cn/dotnet/standard/io/how-to-copy-directories
    /// </summary>
    /// <param name="sourceDir"></param>
    /// <param name="destinationDir"></param>
    /// <param name="recursive"></param>
    /// <exception cref="DirectoryNotFoundException"></exception>
    public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
    {
        // Get information about the source directory
        var dir = new DirectoryInfo(sourceDir);

        // Check if the source directory exists
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        // Cache directories before we start copying
        var dirs = dir.GetDirectories();

        // Create the destination directory
        Directory.CreateDirectory(destinationDir);

        // Get the files in the source directory and copy to the destination directory
        foreach (var file in dir.GetFiles())
        {
            var targetFilePath = Path.Combine(destinationDir, file.Name);

            CopyLockedFile(file.FullName, targetFilePath);
        }

        // If recursive and copying subdirectories, recursively call this method
        if (recursive)
            foreach (var subDir in dirs)
            {
                var newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, true);
            }
    }

    private static void CopyLockedFile(string sourcePath, string targetPath)
    {
        using (var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var destStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
        {
            sourceStream.CopyTo(destStream);
        }
    }
}