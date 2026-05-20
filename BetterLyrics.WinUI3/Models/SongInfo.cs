// 2025/6/23 by Zhe Fang

using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace BetterLyrics.WinUI3.Models
{
    public partial class SongInfo : ObservableRecipient, ICloneable
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

        [ObservableProperty] public partial long StartedAt { get; set; } = DateTime.Now.ToBinary();

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
                StartedAt = this.StartedAt,
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
}
