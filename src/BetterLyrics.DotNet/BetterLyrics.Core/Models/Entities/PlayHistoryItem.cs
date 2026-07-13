using LiteDB;

namespace BetterLyrics.Core.Models.Entities;

public class PlayHistoryItem
{
    public int Id { get; set; }

    // 注意：作为索引列，在使用 LiteDB 时可以通过 Fluent API 或属性设置
    public string Title { get; set; } = "";

    public string Artist { get; set; } = "";

    // Album 没有索引，可以不限制长度，或者为了规范也限制一下
    public string Album { get; set; } = "";

    public DateTime StartedAt { get; set; }

    public double DurationPlayedMs { get; set; }

    public double TotalDurationMs { get; set; }

    // PlayerId 通常是个 GUID 或者短字符串
    public string PlayerId { get; set; } = "";
}