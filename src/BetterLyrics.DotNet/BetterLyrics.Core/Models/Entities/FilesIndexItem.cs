using LiteDB;

namespace BetterLyrics.Core.Models.Entities;

public class FilesIndexItem
{
    public int Id { get; set; }

    // 关联到 MediaFolder.Id
    public string MediaFolderId { get; set; }

    // 存储父文件夹的标准 URI
    // 允许为空
    public string? ParentUri { get; set; }

    // 唯一索引列
    public string Uri { get; set; }

    public string FileName { get; set; } = "";

    public bool IsDirectory { get; set; }

    public long FileSize { get; set; }
    public DateTime? DateCreated { get; set; }

    public DateTime? LastModified { get; set; }

    // 下面的元数据字段通常不需要索引
    public string Title { get; set; } = "";
    public string Artists { get; set; } = "";
    public string Album { get; set; } = "";
    public int? Year { get; set; }
    public string Genre { get; set; } = "";
    public int? TrackNumber { get; set; }
    public int? DiscNumber { get; set; }
    public int Bitrate { get; set; }
    public double SampleRate { get; set; }
    public int BitDepth { get; set; }
    public int Duration { get; set; }

    public string AudioFormatName { get; set; } = "";

    public string AudioFormatShortName { get; set; } = "";

    public string Encoder { get; set; } = "";

    // 歌词可能会很长，保留默认的 nvarchar(max) 即可
    public string? EmbeddedLyrics { get; set; }

    public string? LocalAlbumArtPath { get; set; }

    public bool IsMetadataParsed { get; set; }
}