using ATL;
using BetterLyrics.WinUI3.Helper;
using System;
using System.IO;
using System.Linq;

namespace BetterLyrics.WinUI3.Models
{
    public class ExtendedTrack
    {
        public string Uri { get; private set; } = "";

        public string? RawLyrics { get; set; }
        public string? LocalAlbumArtPath { get; set; }
        public byte[]? AlbumArtByteArray { get; set; }

        public string ParentFolderName
        {
            get
            {
                if (string.IsNullOrEmpty(Uri)) return "";
                try
                {
                    // 使用 Uri Segments 安全获取倒数第二层 (文件夹名)
                    // Segments 示例: "/", "Music/", "Artist/", "Song.mp3"
                    var u = new System.Uri(Uri);
                    if (u.Segments.Length > 1)
                    {
                        // 取倒数第二个 segment (如果是文件)
                        // 注意处理末尾斜杠
                        string folder = u.Segments[u.Segments.Length - 2];
                        return System.Net.WebUtility.UrlDecode(folder.TrimEnd('/', '\\'));
                    }
                    return "";
                }
                catch
                {
                    return "";
                }
            }
        }

        public string ParentFolderPath
        {
            get
            {
                if (string.IsNullOrEmpty(Uri)) return "";
                try
                {
                    var u = new System.Uri(Uri);
                    if (u.IsFile)
                    {
                        // 本地文件：返回目录路径 C:\Music
                        return System.IO.Path.GetDirectoryName(u.LocalPath) ?? "";
                    }
                    else
                    {
                        // 远程文件：返回去掉文件名的 URI
                        // new Uri(u, ".") 表示当前目录
                        return new System.Uri(u, ".").AbsoluteUri;
                    }
                }
                catch
                {
                    return "";
                }
            }
        }

        public string FileName
        {
            get
            {
                if (string.IsNullOrEmpty(Uri)) return "";
                try
                {
                    var u = new System.Uri(Uri);
                    if (u.IsFile) return System.IO.Path.GetFileName(u.LocalPath);

                    // 远程文件：获取 AbsolutePath 的最后一段并解码
                    // 例如: /Music/My%20Song.mp3 -> My Song.mp3
                    string rawName = System.IO.Path.GetFileName(u.AbsolutePath);
                    return System.Net.WebUtility.UrlDecode(rawName);
                }
                catch
                {
                    return System.IO.Path.GetFileName(Uri);
                }
            }
        }
        public string MediaFolderId { get; set; } = "";

        public string Title { get; set; } = "";
        public string Artist { get; set; } = "";
        public string Album { get; set; } = "";
        public int? Year { get; set; }
        public int Bitrate { get; set; }
        public double SampleRate { get; set; }
        public int BitDepth { get; set; }
        public int Duration { get; set; }
        public string AudioFormatName { get; set; } = "";
        public string AudioFormatShortName { get; set; } = "";
        public string Encoder { get; set; } = "";


        public ExtendedTrack() : base() { }

        public ExtendedTrack(string decodedUriString) : base()
        {
            string atlPath = decodedUriString;
            try
            {
                var u = new Uri(decodedUriString);
                Uri = u.AbsoluteUri;
                if (u.IsFile) atlPath = u.LocalPath;
            }
            catch { }

            // 用于本地文件
            var track = new Track(atlPath);
            SetFromTrack(track);
        }

        public ExtendedTrack(FilesIndexItem? entity, Stream? stream = null) : base()
        {
            if (entity == null) return;

            this.MediaFolderId = entity.MediaFolderId;
            this.Uri = entity.Uri;

            this.Title = entity.Title;
            this.Artist = entity.Artists;
            this.Album = entity.Album;
            this.Year = entity.Year;
            this.Bitrate = entity.Bitrate;
            this.SampleRate = entity.SampleRate;
            this.BitDepth = entity.BitDepth;

            this.Duration = entity.Duration;

            this.AudioFormatName = entity.AudioFormatName;
            this.AudioFormatShortName = entity.AudioFormatShortName;

            this.Encoder = entity.Encoder;

            this.RawLyrics = entity.EmbeddedLyrics;
            this.LocalAlbumArtPath = entity.LocalAlbumArtPath;

            if (stream != null)
            {
                var track = new Track(stream, Path.GetExtension(FileName));
                SetFromTrack(track);
                SetRawLyrics(new StreamFileAbstraction(Uri, stream));
            }
        }

        private void SetFromTrack(Track? track)
        {
            if (track == null) return;

            this.Title = track.Title;
            this.Artist = track.Artist;
            this.Album = track.Album;
            this.Year = track.Year;
            this.Bitrate = track.Bitrate;
            this.SampleRate = track.SampleRate;
            this.BitDepth = track.BitDepth;

            this.Duration = track.Duration;

            this.AudioFormatName = track.AudioFormat.Name;
            this.AudioFormatShortName = track.AudioFormat.ShortName;

            this.Encoder = track.Encoder;

            this.AlbumArtByteArray = null;

            if (track.EmbeddedPictures != null && track.EmbeddedPictures.Count > 0)
            {
                try
                {
                    var validPics = track.EmbeddedPictures.Where(p => p != null).ToList();

                    if (validPics.Count > 0)
                    {
                        var cover = validPics.FirstOrDefault(p => p.PicType == PictureInfo.PIC_TYPE.Front);

                        if (cover == null)
                        {
                            cover = validPics.First();
                        }

                        this.AlbumArtByteArray = cover.PictureData;
                    }
                }
                catch (Exception) { }
            }
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