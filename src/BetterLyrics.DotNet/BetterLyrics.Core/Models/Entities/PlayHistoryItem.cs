using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BetterLyrics.Core.Models.Entities;

[Index(nameof(Title))]
[Index(nameof(Artist))]
[Index(nameof(StartedAt))] // 用于按时间排序查询（如：最近播放）
[Index(nameof(PlayerId))]
public class PlayHistoryItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // AutoIncrement
    public int Id { get; set; }

    // 注意：作为索引列，必须加 MaxLength。
    // 如果不加，默认为 nvarchar(max)，SQL Server 无法对其建立高效索引。
    [MaxLength(450)] public string Title { get; set; } = "";

    [MaxLength(450)] public string Artist { get; set; } = "";

    // Album 没有索引，可以不限制长度，或者为了规范也限制一下
    public string Album { get; set; } = "";

    public DateTime StartedAt { get; set; }

    public double DurationPlayedMs { get; set; }

    public double TotalDurationMs { get; set; }

    // PlayerId 通常是个 GUID 或者短字符串，给 100 长度通常足够了，节省索引空间
    [MaxLength(100)] public string PlayerId { get; set; } = "";
}