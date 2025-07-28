using BetterLyrics.WinUI3.Enums;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Converter
{
    public partial class AlbumArtSearchProviderToDisplayNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is AlbumArtSearchProvider provider)
            {
                return provider switch
                {
                    AlbumArtSearchProvider.Local => App.ResourceLoader!.GetString("AlbumArtSearchLocalProvider"),
                    AlbumArtSearchProvider.SMTC => App.ResourceLoader!.GetString("AlbumArtSearchSMTCProvider"),
                    AlbumArtSearchProvider.iTunes => "iTunes",
                    _ => throw new Exception($"Unknown AlbumArtSearchProvider: {provider}"),
                };
            }
            throw new ArgumentException("Value must be of type AlbumArtSearchProvider", nameof(value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
