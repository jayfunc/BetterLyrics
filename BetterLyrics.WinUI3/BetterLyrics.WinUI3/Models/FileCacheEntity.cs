using SQLite;
using System;

namespace BetterLyrics.WinUI3.Models
{
    [Preserve(AllMembers = true)]
    [Table("FileCache")]
    public class FileCacheEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // 关联到 MediaFolder.Id。
        // 区分不同配置（即使两个配置连的是同一个 SMB，但在 APP 里视为不同源）。
        // 删除配置时，可以由 MediaFolderId 快速级联删除所有缓存。
        [Indexed]
        public string MediaFolderId { get; set; }

        // 存储父文件夹的标准 URI (smb://host/share/parent)
        // 根目录文件的 ParentUri 可以为空，或者等于 MediaFolder 的 Base Uri
        [Indexed]
        public string? ParentUri { get; set; }

        // 确保它是 URL 编码过且格式统一的
        [Indexed(Unique = true)]
        public string Uri { get; set; }

        public string FileName { get; set; } = "";

        public bool IsDirectory { get; set; }

        // 记录文件大小，同步时用来对比文件是否变化
        public long FileSize { get; set; }

        // 记录修改时间，同步时对比使用
        public DateTime? LastModified { get; set; }

        public string Title { get; set; } = "";
        public string Artists { get; set; } = "";
        public string Album { get; set; } = "";
        public int? Year { get; set; }
        public int Bitrate { get; set; }
        public double SampleRate { get; set; }
        public int BitDepth { get; set; }
        public int Duration { get; set; }
        public string AudioFormatName { get; set; } = "";
        public string AudioFormatShortName { get; set; } = "";
        public string Encoder { get; set; } = "";
        public string? EmbeddedLyrics { get; set; }
        public string? LocalAlbumArtPath { get; set; }
        public bool IsMetadataParsed { get; set; }
    }
}