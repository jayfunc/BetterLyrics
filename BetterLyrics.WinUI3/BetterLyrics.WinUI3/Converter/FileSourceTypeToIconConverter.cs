using BetterLyrics.WinUI3.Enums;
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
                    FileSourceType.Local => "\uE8B7", // Folder
                    FileSourceType.SMB => "\uE839",   // Network
                    FileSourceType.FTP => "\uE838",   // Globe
                    FileSourceType.WebDAV => "\uE753", // Cloud
                    _ => "\uE8B7"
                };
            }
            return "\uE8B7";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}
