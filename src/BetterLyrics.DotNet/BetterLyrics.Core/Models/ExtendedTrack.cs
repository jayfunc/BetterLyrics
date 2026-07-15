using System.Net;
using ATL;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Models.Entities;
using MimeMapping;
using File = TagLib.File;

namespace BetterLyrics.Core.Models;

public class ExtendedTrack
{
    public ExtendedTrack()
    {
    }

    public ExtendedTrack(string decodedUriString)
    {
        var atlPath = decodedUriString;
        try
        {
            var u = new Uri(decodedUriString);
            Uri = u.AbsoluteUri;
            if (u.IsFile) atlPath = u.LocalPath;
        }
        catch
        {
        }

        // 用于本地文件
        var track = new Track(atlPath);
        SetFromTrack(track);
    }

    public ExtendedTrack(FilesIndexItem? entity, Stream? stream = null)
    {
        if (entity == null) return;

        MediaFolderId = entity.MediaFolderId;
        Uri = entity.Uri;

        Title = entity.Title;
        Artist = entity.Artists;
        Album = entity.Album;
        Year = entity.Year;
        Genre = entity.Genre;
        TrackNumber = entity.TrackNumber;
        DiscNumber = entity.DiscNumber;
        FileSize = entity.FileSize;
        DateCreated = entity.DateCreated;
        DateModified = entity.LastModified;
        Bitrate = entity.Bitrate;
        SampleRate = entity.SampleRate;
        BitDepth = entity.BitDepth;

        Duration = entity.Duration;

        AudioFormatName = entity.AudioFormatName;
        AudioFormatShortName = entity.AudioFormatShortName;

        Encoder = entity.Encoder;

        RawLyrics = entity.EmbeddedLyrics;
        LocalAlbumArtPath = entity.LocalAlbumArtPath;

        if (string.IsNullOrEmpty(Title)) Title = Path.GetFileNameWithoutExtension(entity.FileName);

        if (stream != null)
        {
            var track = new Track(stream, MimeUtility.GetMimeMapping(FileName));
            SetFromTrack(track);
            SetRawLyrics(new StreamFileAbstraction(Uri, stream));
        }
    }

    public string Uri { get; } = "";

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
                var u = new Uri(Uri);
                if (u.Segments.Length > 1)
                {
                    // 取倒数第二个 segment (如果是文件)
                    // 注意处理末尾斜杠
                    var folder = u.Segments[u.Segments.Length - 2];
                    return WebUtility.UrlDecode(folder.TrimEnd('/', '\\'));
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
                var u = new Uri(Uri);
                if (u.IsFile)
                    // 本地文件：返回目录路径 C:\Music
                    return Path.GetDirectoryName(u.LocalPath) ?? "";

                // 远程文件：返回去掉文件名的 URI
                // new Uri(u, ".") 表示当前目录
                return new Uri(u, ".").AbsoluteUri;
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
                var u = new Uri(Uri);
                if (u.IsFile) return Path.GetFileName(u.LocalPath);

                // 远程文件：获取 AbsolutePath 的最后一段并解码
                // 例如: /Music/My%20Song.mp3 -> My Song.mp3
                var rawName = Path.GetFileName(u.AbsolutePath);
                return WebUtility.UrlDecode(rawName);
            }
            catch
            {
                return Path.GetFileName(Uri);
            }
        }
    }

    public string MediaFolderId { get; set; } = "";

    public string Title { get; set; } = "";
    public string Artist { get; set; } = "";
    public string Album { get; set; } = "";
    public int? Year { get; set; }
    public string Genre { get; set; } = "";
    public int? TrackNumber { get; set; }
    public int? DiscNumber { get; set; }
    public long FileSize { get; set; }
    public DateTime? DateCreated { get; set; }
    public DateTime? DateModified { get; set; }
    public int Bitrate { get; set; }
    public double SampleRate { get; set; }
    public int BitDepth { get; set; }
    public int Duration { get; set; }
    public string AudioFormatName { get; set; } = "";
    public string AudioFormatShortName { get; set; } = "";
    public string Encoder { get; set; } = "";

    private void SetFromTrack(Track? track)
    {
        if (track == null) return;

        Title = track.Title;
        Artist = track.Artist;
        Album = track.Album;
        Year = track.Year;
        Genre = track.Genre;
        TrackNumber = track.TrackNumber;
        DiscNumber = track.DiscNumber;
        Bitrate = track.Bitrate;
        SampleRate = track.SampleRate;
        BitDepth = track.BitDepth;

        Duration = track.Duration;

        AudioFormatName = track.AudioFormat.Name;
        AudioFormatShortName = track.AudioFormat.ShortName;

        Encoder = track.Encoder;

        AlbumArtByteArray = null;

        try
        {
            if (track.EmbeddedPictures != null && track.EmbeddedPictures.Count > 0)
            {
                var validPics = track.EmbeddedPictures.Where(p => p != null).ToList();

                if (validPics.Count > 0)
                {
                    var cover = validPics.FirstOrDefault(p => p.PicType == PictureInfo.PIC_TYPE.Front);

                    if (cover == null) cover = validPics.First();

                    AlbumArtByteArray = cover.PictureData;
                }
            }
        }
        catch (Exception)
        {
        }
    }

    private void SetRawLyrics(StreamFileAbstraction streamFileAbstraction)
    {
        try
        {
            RawLyrics = File.Create(streamFileAbstraction).Tag.Lyrics;
        }
        catch (Exception)
        {
        }
    }
}