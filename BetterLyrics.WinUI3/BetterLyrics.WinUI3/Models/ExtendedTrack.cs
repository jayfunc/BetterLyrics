using BetterLyrics.WinUI3.Helper;
using System;
using System.IO;

namespace BetterLyrics.WinUI3.Models
{
    public class ExtendedTrack : ATL.Track
    {
        public new string Path { get; private set; } = "";
        public string RawLyrics { get; set; } = "";
        public string ParentFolderName => Directory.GetParent(Path)?.Name ?? "";
        public string ParentFolderPath => Directory.GetParent(Path)?.FullName ?? "";
        public string FileName => System.IO.Path.GetFileName(Path);

        public ExtendedTrack() : base() { }

        public ExtendedTrack(string path) : base(path)
        {
            Path = path;
        }

        public ExtendedTrack(string path, Stream stream) : base(stream, System.IO.Path.GetExtension(path))
        {
            Path = path;
            SetRawLyrics(new StreamFileAbstraction(path, stream));
        }

        private void SetRawLyrics(StreamFileAbstraction streamFileAbstraction)
        {
            try
            {
                RawLyrics = TagLib.File.Create(streamFileAbstraction).Tag.Lyrics;
            }
            catch (Exception) { }
        }
    }
}
