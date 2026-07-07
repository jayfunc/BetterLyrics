using System;
using BetterLyrics.Core.Enums;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converters;

public partial class ComponentTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ComponentType type)
            return type switch
            {
                ComponentType.AlbumArt => "\uE93C",
                ComponentType.SongTitle => "\uE8D2",
                ComponentType.SongArtist => "\uE8D2",
                ComponentType.SongAlbum => "\uE8D2",
                ComponentType.Lyrics => "\uE8E3",
                ComponentType.LyricsCard => "\uE7FB",
                _ => "\uE12B"
            };
        return "\uE12B";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}