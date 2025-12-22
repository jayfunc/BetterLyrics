using BetterLyrics.WinUI3.Enums;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;

namespace BetterLyrics.WinUI3.Converter
{
    public partial class FileSourceTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is FileSourceType type)
            {
                return type switch
                {
                    FileSourceType.Local => new FontIcon { Glyph = "\uE8B7" }, // Folder
                    FileSourceType.SMB => new FontIcon { Glyph = "\uE839" },   // Network
                    FileSourceType.FTP => new FontIcon { Glyph = "\uE838" },   // Globe
                    FileSourceType.WebDav => new FontIcon { Glyph = "\uE753" }, // Cloud
                    _ => new FontIcon { Glyph = "\uE8B7" }
                };
            }
            return new FontIcon { Glyph = "\uE8B7" };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}
