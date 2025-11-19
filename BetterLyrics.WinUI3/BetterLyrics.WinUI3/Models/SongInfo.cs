// 2025/6/23 by Zhe Fang

using CommunityToolkit.Mvvm.ComponentModel;
using NTextCat.Commons;
using System;

namespace BetterLyrics.WinUI3.Models
{
    public partial class SongInfo : ObservableObject, ICloneable
    {
        [ObservableProperty]
        public partial string Album { get; set; }

        [ObservableProperty]
        public partial string[] Artists { get; set; }

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

        public string DisplayArtists => Artists.Join(ATL.Settings.DisplayValueSeparator.ToString());

        public SongInfo() { }

        public object Clone()
        {
            return new SongInfo()
            {
                Title = this.Title,
                Artists = this.Artists,
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
                $"Title: {Title}, " +
                $"Artist: {DisplayArtists}, " +
                $"Album: {Album}, " +
                $"Duration: {Duration} sec, " +
                $"Plauer ID: {PlayerId}, " +
                $"Song ID: {SongId}, " +
                $"Linked file name: {LinkedFileName}.";
        }

        public string ToFileName()
        {
            return $"{DisplayArtists} - {Title} - {Album} - {Duration}";
        }
    }
}
