// 2025/6/23 by Zhe Fang

using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models;

public partial class SongInfo : ObservableRecipient, ICloneable
{
    [ObservableProperty] public partial string Album { get; set; }

    [ObservableProperty] public partial string Artist { get; set; }

    [ObservableProperty] public partial double DurationMs { get; set; }

    [ObservableProperty] public partial string? PlayerId { get; set; } = null;

    [ObservableProperty] public partial string Title { get; set; }

    [ObservableProperty] public partial string? SongId { get; set; } = null;

    [ObservableProperty] public partial long StartedAt { get; set; } = DateTime.Now.ToBinary();

    public string? LinkedFileName { get; set; }

    public string? AlbumArtUrl { get; set; }

    public double Duration => DurationMs / 1000;

    public object Clone()
    {
        return new SongInfo
        {
            Title = Title,
            Artist = Artist,
            Album = Album,
            DurationMs = DurationMs,
            PlayerId = PlayerId,
            SongId = SongId,
            LinkedFileName = LinkedFileName,
            StartedAt = StartedAt,
            AlbumArtUrl = AlbumArtUrl
        };
    }

    public override string ToString()
    {
        return
            $"Title: {Title}, " +
            $"Artist: {Artist}, " +
            $"Album: {Album}, " +
            $"Duration: {Duration} sec, " +
            $"Plauer ID: {PlayerId}, " +
            $"Song ID: {SongId}, " +
            $"Linked file name: {LinkedFileName}.";
    }

    public string ToSearchString()
    {
        return $"{Artist} {Title} {Album}".Trim();
    }
}