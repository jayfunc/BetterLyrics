using System.IO;

namespace BetterLyrics.WinUI3.Helper
{
    public static class TrackHelper
    {
        public static string GetParentFolderName(this ATL.Track track)
        {
            return Directory.GetParent(track.Path)?.Name ?? "";
        }

        public static string GetParentFolderPath(this ATL.Track track)
        {
            return Directory.GetParent(track.Path)?.FullName ?? "";
        }

        public static string GetLyrics(this ATL.Track track)
        {
            if (track.Path is string path)
            {
                return TagLib.File.Create(path).Tag.Lyrics;
            }
            return "";
        }
    }
}
