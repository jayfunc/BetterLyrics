using BetterLyrics.WinUI3.Collections;
using BetterLyrics.WinUI3.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;

namespace BetterLyrics.WinUI3.Models
{
    public partial class LayoutProfile : ObservableRecipient, ICloneable
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial ObservableCollection<string> RowDefinitions { get; set; } = new() { "1*", "1*" };
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial ObservableCollection<string> ColumnDefinitions { get; set; } = new() { "1*", "1*" };

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

        public LayoutProfile(LyricsWindowMode mode) : this()
        {
            switch (mode)
            {
                case LyricsWindowMode.Standard:
                    InitStandardMode();
                    break;
                case LyricsWindowMode.Narrow:
                    InitNarrowMode();
                    break;
                case LyricsWindowMode.Fullscreen:
                    InitFullscreenMode();
                    break;
                case LyricsWindowMode.Desktop:
                    InitDesktopMode();
                    break;
                case LyricsWindowMode.Docked:
                    InitDockedMode();
                    break;
                case LyricsWindowMode.Taskbar:
                    InitTaskbarMode();
                    break;
                case LyricsWindowMode.Wallpaper:
                    InitWallpaperMode();
                    break;
                default:
                    break;
            }
        }

        private void InitStandardMode()
        {
            RowDefinitions = ["1*", "1*", "1*", "1*", "1*"];
            ColumnDefinitions = ["1*", "1*"];

            RowSpacing = 16;
            ColumnSpacing = 16;

            PaddingLeft = 16;
            PaddingTop = 16;
            PaddingRight = 16;
            PaddingBottom = 16;

            Placements =
            [
                new()
                {
                    ComponentType = ComponentType.SongInfo,
                    Row = 3,
                    Column = 0,
                    RowSpan = 1,
                    ColumnSpan = 1,
                    MarginLeft = 32,
                    MarginTop = 0,
                    MarginRight = 32,
                    MarginBottom = 0,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Stretch,
                },
                new()
                {
                    ComponentType = ComponentType.AlbumArt,
                    Row = 1,
                    Column = 0,
                    RowSpan = 2,
                    ColumnSpan = 1,
                    MarginLeft = 0,
                    MarginTop = 0,
                    MarginRight = 0,
                    MarginBottom = 0,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                },
                new()
                {
                    ComponentType = ComponentType.Lyrics,
                    Row = 0,
                    Column = 1,
                    RowSpan = 5,
                    ColumnSpan = 1,
                    MarginLeft = 0,
                    MarginTop = 0,
                    MarginRight = 16,
                    MarginBottom = 0,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                }
            ];
        }

        private void InitNarrowMode()
        {
            RowDefinitions = ["Auto", "1*"];
            ColumnDefinitions = ["1*", "1*", "1*", "1*"];

            RowSpacing = 16;
            ColumnSpacing = 16;

            PaddingLeft = 16;
            PaddingTop = 16;
            PaddingRight = 16;
            PaddingBottom = 16;

            Placements =
            [
                new()
                {
                    ComponentType = ComponentType.SongInfo,
                    Row = 0,
                    Column = 1,
                    RowSpan = 1,
                    ColumnSpan = 3,
                    MarginLeft = 0,
                    MarginTop = 0,
                    MarginRight = 0,
                    MarginBottom = 0,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                },
                new()
                {
                    ComponentType = ComponentType.AlbumArt,
                    Row = 0,
                    Column = 0,
                    RowSpan = 1,
                    ColumnSpan = 1,
                    MarginLeft = 0,
                    MarginTop = 0,
                    MarginRight = 0,
                    MarginBottom = 0,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                },
                new()
                {
                    ComponentType = ComponentType.Lyrics,
                    Row = 1,
                    Column = 0,
                    RowSpan = 1,
                    ColumnSpan = 4,
                    MarginLeft = 0,
                    MarginTop = 0,
                    MarginRight = 0,
                    MarginBottom = 0,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                }
            ];
        }

        private void InitFullscreenMode()
        {
            RowDefinitions = ["1*", "1*", "1*", "1*", "1*"];
            ColumnDefinitions = ["Auto", "1*", "1*", "1*"];

            RowSpacing = 32;
            ColumnSpacing = 32;

            PaddingLeft = 32;
            PaddingTop = 32;
            PaddingRight = 32;
            PaddingBottom = 32;

            Placements =
            [
                new()
                {
                    ComponentType = ComponentType.SongInfo,
                    Row = 0,
                    Column = 1,
                    RowSpan = 1,
                    ColumnSpan = 3,
                    MarginLeft = 0,
                    MarginTop = 0,
                    MarginRight = 0,
                    MarginBottom = 0,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                },
                new()
                {
                    ComponentType = ComponentType.AlbumArt,
                    Row = 0,
                    Column = 0,
                    RowSpan = 1,
                    ColumnSpan = 1,
                    MarginLeft = 0,
                    MarginTop = 0,
                    MarginRight = 0,
                    MarginBottom = 0,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                },
                new()
                {
                    ComponentType = ComponentType.Lyrics,
                    Row = 1,
                    Column = 0,
                    RowSpan = 4,
                    ColumnSpan = 4,
                    MarginLeft = 0,
                    MarginTop = 0,
                    MarginRight = 0,
                    MarginBottom = 0,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                }
            ];
        }

        private void InitDesktopMode()
        {
            RowDefinitions = ["1*"];
            ColumnDefinitions = ["1*"];

            RowSpacing = 16;
            ColumnSpacing = 16;

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
                    MarginLeft = 0,
                    MarginTop = 0,
                    MarginRight = 0,
                    MarginBottom = 0,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                }
            ];
        }

        private void InitDockedMode()
        {
            RowDefinitions = ["1*"];
            ColumnDefinitions = ["1*", "1*", "1*", "1*", "1*"];

            RowSpacing = 16;
            ColumnSpacing = 16;

            PaddingLeft = 0;
            PaddingTop = 0;
            PaddingRight = 0;
            PaddingBottom = 0;

            Placements =
            [
                new()
                {
                    ComponentType = ComponentType.SongInfo,
                    Row = 0,
                    Column = 0,
                    RowSpan = 1,
                    ColumnSpan = 1,
                    MarginLeft = 8,
                    MarginTop = 8,
                    MarginRight = 0,
                    MarginBottom = 8,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                },
                new()
                {
                    ComponentType = ComponentType.AlbumArt,
                    Row = 0,
                    Column = 4,
                    RowSpan = 1,
                    ColumnSpan = 1,
                    MarginLeft = 0,
                    MarginTop = 8,
                    MarginRight = 8,
                    MarginBottom = 8,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Stretch,
                },
                new()
                {
                    ComponentType = ComponentType.Lyrics,
                    Row = 0,
                    Column = 1,
                    RowSpan = 1,
                    ColumnSpan = 3,
                    MarginLeft = 0,
                    MarginTop = 0,
                    MarginRight = 0,
                    MarginBottom = 0,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                }
            ];
        }

        private void InitTaskbarMode()
        {
            RowDefinitions = ["1*"];
            ColumnDefinitions = ["Auto", "1*"];

            RowSpacing = 16;
            ColumnSpacing = 16;

            PaddingLeft = 0;
            PaddingTop = 0;
            PaddingRight = 0;
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
                    MarginLeft = 8,
                    MarginTop = 10,
                    MarginRight = 0,
                    MarginBottom = 10,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                },
                new()
                {
                    ComponentType = ComponentType.Lyrics,
                    Row = 0,
                    Column = 1,
                    RowSpan = 1,
                    ColumnSpan = 1,
                    MarginLeft = 0,
                    MarginTop = 0,
                    MarginRight = 0,
                    MarginBottom = 0,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                }
            ];
        }

        private void InitWallpaperMode()
        {
            RowDefinitions = ["1*"];
            ColumnDefinitions = ["1*"];

            RowSpacing = 16;
            ColumnSpacing = 16;

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
                    MarginLeft = 0,
                    MarginTop = 0,
                    MarginRight = 0,
                    MarginBottom = 0,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                }
            ];
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
                RowDefinitions = new ObservableCollection<string>(this.RowDefinitions),
                ColumnDefinitions = new ObservableCollection<string>(this.ColumnDefinitions),
                RowSpacing = this.RowSpacing,
                ColumnSpacing = this.ColumnSpacing,
                Placements = new FullyObservableCollection<ComponentPlacement>(this.Placements),
                PaddingLeft = this.PaddingLeft,
                PaddingTop = this.PaddingTop,
                PaddingRight = this.PaddingRight,
                PaddingBottom = this.PaddingBottom,
            };
        }
    }
}
