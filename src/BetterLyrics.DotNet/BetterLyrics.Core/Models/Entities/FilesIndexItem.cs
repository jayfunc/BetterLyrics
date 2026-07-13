using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BetterLyrics.Core.Models.Entities;

[Index(nameof(MediaFolderId))] // 普通索引
[Index(nameof(ParentUri))] // 普通索引
[Index(nameof(Uri), IsUnique = true)] // 唯一索引
public class FilesIndexItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // 关联到 MediaFolder.Id
    // 注意：作为索引列，必须限制长度，否则 SQL Server 会报错 (索引最大900字节)
    [MaxLength(450)] public string MediaFolderId { get; set; }

    // 存储父文件夹的标准 URI
    // 允许为空
    [MaxLength(450)] public string? ParentUri { get; set; }

    // 唯一索引列
    // 必须限制长度。450字符 * 2字节/字符 = 900字节 (正好卡在 SQL Server 限制内)
    [Required] [MaxLength(450)] public string Uri { get; set; }

    public string FileName { get; set; } = "";

    public bool IsDirectory { get; set; }

    public long FileSize { get; set; }

    public DateTime? LastModified { get; set; }

    // 下面的元数据字段通常不需要索引，可以使用 MaxLength 稍微优化空间，
    // 或者直接留空（默认为 nvarchar(max)）
    public string Title { get; set; } = "";
    [Column("Artists")] public string Artist { get; set; } = "";
    public string Album { get; set; } = "";
    public int? Year { get; set; }
    public int Bitrate { get; set; }
    public double SampleRate { get; set; }
    public int BitDepth { get; set; }
    public int Duration { get; set; }

    [MaxLength(50)] public string AudioFormatName { get; set; } = "";

    [MaxLength(20)] public string AudioFormatShortName { get; set; } = "";

    public string Encoder { get; set; } = "";

    // 歌词可能会很长，保留默认的 nvarchar(max) 即可
    public string? EmbeddedLyrics { get; set; }

    public string? LocalAlbumArtPath { get; set; }

    public bool IsMetadataParsed { get; set; }
}