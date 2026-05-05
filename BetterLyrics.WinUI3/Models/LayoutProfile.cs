using BetterLyrics.WinUI3.Collections;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Services.LocalizationService;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace BetterLyrics.WinUI3.Models
{
    public partial class LayoutProfile : ObservableRecipient, ICloneable
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public NowPlayingLayoutMode Mode { get; set; } = NowPlayingLayoutMode.Custom;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string Name { get; set; } = "";

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial ObservableCollection<string> RowDefinitions { get; set; } = new() { "1*" };
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial ObservableCollection<string> ColumnDefinitions { get; set; } = new() { "1*" };

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial double RowSpacing { get; set; } = 16;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial double ColumnSpacing { get; set; } = 16;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial FullyObservableCollection<ComponentPlacement> Placements { get; set; } = new();

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial double PaddingLeft { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial double PaddingTop { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial double PaddingRight { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial double PaddingBottom { get; set; } = 0;

        public LayoutProfile()
        {
            RowDefinitions.CollectionChanged += RowDefinitions_CollectionChanged;
            ColumnDefinitions.CollectionChanged += ColumnDefinitions_CollectionChanged;
            Placements.CollectionChanged += Placements_CollectionChanged;
            Placements.ItemPropertyChanged += Placements_ItemPropertyChanged;
        }

        public LayoutProfile(NowPlayingLayoutMode mode) : this()
        {
            ILocalizationService localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();

            Mode = mode;
            Name = localizationService.GetLocalizedString($"{mode}Layout");

            switch (mode)
            {
                case NowPlayingLayoutMode.LyricsOnly:
                    InitLyricsOnlyMode();
                    break;
                case NowPlayingLayoutMode.AlbumArtOnly:
                    InitAlbumArtOnlyMode();
                    break;
                case NowPlayingLayoutMode.LeftAlbumArtRightLyrics:
                    InitLeftAlbumArtRightLyricsMode();
                    break;
                case NowPlayingLayoutMode.LeftLyricsRightAlbumArt:
                    InitLeftLyricsRightAlbumArtMode();
                    break;
                case NowPlayingLayoutMode.LeftAlbumArtRightLyricsCompact:
                    InitLeftAlbumArtRightLyricsCompactMode();
                    break;
                case NowPlayingLayoutMode.LeftLyricsRightAlbumArtCompact:
                    InitLeftLyricsRightAlbumArtCompactMode();
                    break;
                case NowPlayingLayoutMode.TopAlbumArtBottomLyrics:
                    InitTopAlbumArtBottomLyricsMode();
                    break;
                case NowPlayingLayoutMode.TopAlbumArtBottomLyricsCompact:
                    InitTopAlbumArtBottomLyricsCompactMode();
                    break;
                case NowPlayingLayoutMode.LyricsCardOnly:
                    InitLyricsCardOnlyMode();
                    break;
                default:
                    break;
            }
        }

        private void InitLyricsOnlyMode()
        {
            RowDefinitions = ["1*"];
            ColumnDefinitions = ["1*"];

            RowSpacing = 0;
            ColumnSpacing = 0;

            PaddingLeft = 0;
            PaddingTop = 0;
            PaddingRight = 0;
            PaddingBottom = 0;

            Placements =
            [
                new()
                {
                    ComponentType = ComponentType.Lyrics,
                    Row = 0,
                    Column = 0,
                    RowSpan = 1,
                    ColumnSpan = 1,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                }
            ];
        }

        private void InitAlbumArtOnlyMode()
        {
            RowDefinitions = ["2*", "8*", "0.5*", "1.2*", "1*", "1*", "2*"];
            ColumnDefinitions = ["1*", "10*", "1*"];

            RowSpacing = 0;
            ColumnSpacing = 0;

            PaddingLeft = 0;
            PaddingTop = 0;
            PaddingRight = 0;
            PaddingBottom = 0;

            Placements =
            [
                new()
                {
                    ComponentType = ComponentType.AlbumArt,
                    Row = 1,
                    Column = 1,
                    RowSpan = 1,
                    ColumnSpan = 1,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Stretch,
                },
                new()
                {
                    ComponentType = ComponentType.SongTitle,
                    Row = 3,
                    Column = 1,
                    RowSpan = 1,
                    ColumnSpan = 1,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Stretch,
                },
                new()
                {
                    ComponentType = ComponentType.SongArtist,
                    Row = 4,
                    Column = 1,
                    RowSpan = 1,
                    ColumnSpan = 1,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Stretch,
                },
                new()
                {
                    ComponentType = ComponentType.SongAlbum,
                    Row = 5,
                    Column = 1,
                    RowSpan = 1,
                    ColumnSpan = 1,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Stretch,
                },
            ];
        }

        private void InitLeftAlbumArtRightLyricsMode()
        {
            RowDefinitions = ["1*", "5*", "0.2*", "0.6*", "0.5*", "0.5*", "1*"];
            ColumnDefinitions = ["2*", "6*", "1*", "6*", "2*"];

            RowSpacing = 0;
            ColumnSpacing = 0;

            PaddingLeft = 0;
            PaddingTop = 0;
            PaddingRight = 0;
            PaddingBottom = 0;

            Placements =
            [
                new()
                {
                    ComponentType = ComponentType.SongArtist,
                    Row = 4,
                    Column = 1,
                    RowSpan = 1,
                    ColumnSpan = 1,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Width = double.NaN,
                    Height = double.NaN
                },
                new()
                {
                    ComponentType = ComponentType.SongAlbum,
                    Row = 5,
                    Column = 1,
                    RowSpan = 1,
                    ColumnSpan = 1,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Width = double.NaN,
                    Height = double.NaN
                },
                new()
                {
                    ComponentType = ComponentType.AlbumArt,
                    Row = 1,
                    Column = 1,
                    RowSpan = 1,
                    ColumnSpan = 1,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Width = double.NaN,
                    Height = double.NaN
                },
                new()
                {
                    ComponentType = ComponentType.SongTitle,
                    Row = 3,
                    Column = 1,
                    RowSpan = 1,
                    ColumnSpan = 1,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Width = double.NaN,
                    Height = double.NaN
                },
                new()
                {
                    ComponentType = ComponentType.Lyrics,
                    Row = 0,
                    Column = 3,
                    RowSpan = 7,
                    ColumnSpan = 1,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Width = double.NaN,
                    Height = double.NaN
                }
            ];
        }

        private void InitLeftLyricsRightAlbumArtMode()
        {
            RowDefinitions = ["1*", "5*", "0.2*", "0.6*", "0.5*", "0.5*", "1*"];
            ColumnDefinitions = ["2*", "6*", "1*", "6*", "2*"];
            RowSpacing = 0;
            ColumnSpacing = 0;
            PaddingLeft = 0;
            PaddingTop = 0;
            PaddingRight = 0;
            PaddingBottom = 0;
            Placements =
            [
                new()
                {
                    ComponentType = ComponentType.SongArtist,
                    Row = 4,
                    Column = 3,
                    RowSpan = 1,
                    ColumnSpan = 1,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Width = double.NaN,
                    Height = double.NaN
                },
                new()
                {
                    ComponentType = ComponentType.SongAlbum,
                    Row = 5,
                    Column = 3,
                    RowSpan = 1,
                    ColumnSpan = 1,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Width = double.NaN,
                    Height = double.NaN
                },
                new()
                {
                    ComponentType = ComponentType.AlbumArt,
                    Row = 1,
                    Column = 3,
                    RowSpan = 1,
                    ColumnSpan = 1,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Width = double.NaN,
                    Height = double.NaN
                },
                new()
                {
                    ComponentType = ComponentType.SongTitle,
                    Row = 3,
                    Column = 3,
                    RowSpan = 1,
                    ColumnSpan = 1,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Width = double.NaN,
                    Height = double.NaN
                },
                new()
                {
                    ComponentType = ComponentType.Lyrics,
                    Row = 0,
                    Column = 1,
                    RowSpan = 7,
                    ColumnSpan = 1,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Width = double.NaN,
                    Height = double.NaN
                }
            ];
        }

        private void InitLeftAlbumArtRightLyricsCompactMode()
        {
            RowDefinitions = ["1*"];
            ColumnDefinitions = ["Auto", "1*"];

            RowSpacing = 0;
            ColumnSpacing = 12;

            PaddingLeft = 12;
            PaddingTop = 0;
            PaddingRight = 12;
            PaddingBottom = 0;

            Placements =
            [
                new()
                {
                    ComponentType = ComponentType.AlbumArt,
                    Row = 0,
                    Column = 0,
                    RowSpan = 1,
                    ColumnSpan = 1,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = double.NaN,
                    Height = double.NaN,
                    MarginTop = 10,
                    MarginBottom = 10,
                },
                new()
                {
                    ComponentType = ComponentType.Lyrics,
                    Row = 0,
                    Column = 1,
                    RowSpan = 1,
                    ColumnSpan = 1,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Width = double.NaN,
                    Height = double.NaN
                }
            ];
        }

        private void InitLeftLyricsRightAlbumArtCompactMode()
        {
            RowDefinitions = ["1*"];
            ColumnDefinitions = ["1*", "Auto"];

            RowSpacing = 0;
            ColumnSpacing = 12;

            PaddingLeft = 12;
            PaddingTop = 0;
            PaddingRight = 12;
            PaddingBottom = 0;

            Placements =
            [
                new()
                {
                    ComponentType = ComponentType.AlbumArt,
                    Row = 0,
                    Column = 1,
                    RowSpan = 1,
                    ColumnSpan = 1,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = double.NaN,
                    Height = double.NaN,
                    MarginTop = 10,
                    MarginBottom = 10,
                },
                new()
                {
                    ComponentType = ComponentType.Lyrics,
                    Row = 0,
                    Column = 0,
                    RowSpan = 1,
                    ColumnSpan = 1,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Width = double.NaN,
                    Height = double.NaN
                }
            ];
        }

        private void InitTopAlbumArtBottomLyricsMode()
        {
            RowDefinitions = ["1*", "1*", "0.8*", "0.8*", "16*"];
            ColumnDefinitions = ["0.5*", "Auto", "0.2*", "10*", "0.5*"];

            RowSpacing = 0;
            ColumnSpacing = 0;

            PaddingLeft = 0;
            PaddingTop = 0;
            PaddingRight = 0;
            PaddingBottom = 0;

            Placements =
            [
                new()
                {
                    ComponentType = ComponentType.SongArtist,
                    Row = 2,
                    Column = 3,
                    RowSpan = 1,
                    ColumnSpan = 1,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Width = double.NaN,
                    Height = double.NaN
                },
                new()
                {
                    ComponentType = ComponentType.AlbumArt,
                    Row = 1,
                    Column = 1,
                    RowSpan = 3,
                    ColumnSpan = 1,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Width = double.NaN,
                    Height = double.NaN
                },
                new()
                {
                    ComponentType = ComponentType.SongTitle,
                    Row = 1,
                    Column = 3,
                    RowSpan = 1,
                    ColumnSpan = 1,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Width = double.NaN,
                    Height = double.NaN
                },
                new()
                {
                    ComponentType = ComponentType.Lyrics,
                    Row = 0,
                    Column = 1,
                    RowSpan = 5,
                    ColumnSpan = 3,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Width = double.NaN,
                    Height = double.NaN
                },
                new()
                {
                    ComponentType = ComponentType.SongAlbum,
                    Row = 3,
                    Column = 3,
                    RowSpan = 1,
                    ColumnSpan = 1,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Width = double.NaN,
                    Height = double.NaN
                }
            ];
        }

        private void InitTopAlbumArtBottomLyricsCompactMode()
        {
            RowDefinitions = ["0.8*", "1*", "0.8*", "0.8*", "16*"];
            ColumnDefinitions = ["1*", "Auto", "0.5*", "10*", "1*"];

            RowSpacing = 0;
            ColumnSpacing = 0;

            PaddingLeft = 0;
            PaddingTop = 0;
            PaddingRight = 0;
            PaddingBottom = 0;

            Placements =
            [
                new()
                {
                    ComponentType = ComponentType.SongArtist,
                    Row = 2,
                    Column = 3,
                    RowSpan = 1,
                    ColumnSpan = 1,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Width = double.NaN,
                    Height = double.NaN
                },
                new()
                {
                    ComponentType = ComponentType.AlbumArt,
                    Row = 1,
                    Column = 1,
                    RowSpan = 3,
                    ColumnSpan = 1,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Width = double.NaN,
                    Height = double.NaN
                },
                new()
                {
                    ComponentType = ComponentType.SongTitle,
                    Row = 1,
                    Column = 3,
                    RowSpan = 1,
                    ColumnSpan = 1,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Width = double.NaN,
                    Height = double.NaN
                },
                new()
                {
                    ComponentType = ComponentType.Lyrics,
                    Row = 0,
                    Column = 1,
                    RowSpan = 5,
                    ColumnSpan = 3,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Width = double.NaN,
                    Height = double.NaN
                },
                new()
                {
                    ComponentType = ComponentType.SongAlbum,
                    Row = 3,
                    Column = 3,
                    RowSpan = 1,
                    ColumnSpan = 1,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Width = double.NaN,
                    Height = double.NaN
                }
            ];
        }

        private void InitLyricsCardOnlyMode()
        {
            RowDefinitions = ["1*"];
            ColumnDefinitions = ["1*"];
            RowSpacing = 0;
            ColumnSpacing = 0;
            PaddingLeft = 0;
            PaddingTop = 0;
            PaddingRight = 0;
            PaddingBottom = 0;
            Placements =
            [
                new()
                {
                    ComponentType = ComponentType.LyricsCard,
                    Row = 0,
                    Column = 0,
                    RowSpan = 1,
                    ColumnSpan = 1,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                }
            ];
        }

        public void ApplyFrom(LayoutProfile source)
        {
            RowSpacing = source.RowSpacing;
            ColumnSpacing = source.ColumnSpacing;

            PaddingLeft = source.PaddingLeft;
            PaddingTop = source.PaddingTop;
            PaddingRight = source.PaddingRight;
            PaddingBottom = source.PaddingBottom;

            RowDefinitions.Clear();
            foreach (var r in source.RowDefinitions) RowDefinitions.Add(r);

            ColumnDefinitions.Clear();
            foreach (var c in source.ColumnDefinitions) ColumnDefinitions.Add(c);

            Placements.Clear();
            foreach (var p in source.Placements) Placements.Add(p);
        }

        partial void OnPlacementsChanged(FullyObservableCollection<ComponentPlacement> oldValue, FullyObservableCollection<ComponentPlacement> newValue)
        {
            oldValue.CollectionChanged -= Placements_CollectionChanged;
            oldValue.ItemPropertyChanged -= Placements_ItemPropertyChanged;

            newValue.CollectionChanged += Placements_CollectionChanged;
            newValue.ItemPropertyChanged += Placements_ItemPropertyChanged;
        }

        private void Placements_ItemPropertyChanged(object? sender, ItemPropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Placements));
            Broadcast(Placements, Placements, nameof(Placements));
        }

        private void Placements_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Placements));
            Broadcast(Placements, Placements, nameof(Placements));
        }

        partial void OnColumnDefinitionsChanged(ObservableCollection<string> oldValue, ObservableCollection<string> newValue)
        {
            oldValue.CollectionChanged -= ColumnDefinitions_CollectionChanged;
            newValue.CollectionChanged += ColumnDefinitions_CollectionChanged;
        }

        private void ColumnDefinitions_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(ColumnDefinitions));
            Broadcast(ColumnDefinitions, ColumnDefinitions, nameof(ColumnDefinitions));
        }

        partial void OnRowDefinitionsChanged(ObservableCollection<string> oldValue, ObservableCollection<string> newValue)
        {
            oldValue.CollectionChanged -= RowDefinitions_CollectionChanged;
            newValue.CollectionChanged += RowDefinitions_CollectionChanged;
        }

        private void RowDefinitions_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(RowDefinitions));
            Broadcast(RowDefinitions, RowDefinitions, nameof(RowDefinitions));
        }

        public object Clone()
        {
            return new LayoutProfile
            {
                Name = this.Name,
                Mode = NowPlayingLayoutMode.Custom,

                RowDefinitions = new ObservableCollection<string>(this.RowDefinitions),
                ColumnDefinitions = new ObservableCollection<string>(this.ColumnDefinitions),

                RowSpacing = this.RowSpacing,
                ColumnSpacing = this.ColumnSpacing,

                Placements = new FullyObservableCollection<ComponentPlacement>(this.Placements.Select(p => (ComponentPlacement)p.Clone())),

                PaddingLeft = this.PaddingLeft,
                PaddingTop = this.PaddingTop,
                PaddingRight = this.PaddingRight,
                PaddingBottom = this.PaddingBottom,
            };
        }
    }
}
