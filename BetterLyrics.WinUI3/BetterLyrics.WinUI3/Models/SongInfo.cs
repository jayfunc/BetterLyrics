// 2025/6/23 by Zhe Fang

using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace BetterLyrics.WinUI3.Models
{
    public partial class SongInfo : ObservableObject, ICloneable
    {
        [ObservableProperty]
        public partial string Album { get; set; }

        [ObservableProperty]
        public partial string Artist { get; set; }

        [ObservableProperty]
        public partial double DurationMs { get; set; }

        [ObservableProperty]
        public partial string? PlayerId { get; set; } = null;

        [ObservableProperty]
        public partial string Title { get; set; }

        [ObservableProperty]
        public partial string? SongId { get; set; } = null;

        public string? LinkedFileName { get; set; } = null;

        public double Duration => DurationMs / 1000;

        public SongInfo() { }

        public object Clone()
        {
            return new SongInfo()
            {
                Title = this.Title,
                Artist = this.Artist,
                Album = this.Album,
                DurationMs = this.DurationMs,
                PlayerId = this.PlayerId,
                SongId = this.SongId,
                LinkedFileName = this.LinkedFileName,
            };
        }

        public override string ToString()
        {
            return
                $"Title: {Title}\n" +
                $"Artist: {Artist}\n" +
                $"Album: {Album}\n" +
                $"Duration: {Duration} sec\n" +
                $"Plauer ID: {PlayerId}\n" +
                $"Song ID: {SongId}\n" +
                $"Linked file name: {LinkedFileName}";
        }
    }

    public static class SongInfoExtensions
    {
        public static SongInfo Placeholder => new()
        {
            Title = "N/A",
            Album = "N/A",
            Artist = "N/A",
        };
    }
}
