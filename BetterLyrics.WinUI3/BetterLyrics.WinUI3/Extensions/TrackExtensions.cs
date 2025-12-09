using ATL;
using System.IO;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class TrackExtensions
    {
        extension(Track track)
        {
            public string GetParentFolderName() => Directory.GetParent(track.Path)?.Name ?? "";

            public string GetParentFolderPath() => Directory.GetParent(track.Path)?.FullName ?? "";

            public string GetRawLyrics()
            {
                if (track.Path is string path)
                {
                    try
                    {
                        return TagLib.File.Create(path).Tag.Lyrics;
                    }
                    catch (System.Exception)
                    {
                        return "";
                    }
                }
                return "";
            }

            public string GetFileName() => Path.GetFileName(track.Path);
        }
    }
}
