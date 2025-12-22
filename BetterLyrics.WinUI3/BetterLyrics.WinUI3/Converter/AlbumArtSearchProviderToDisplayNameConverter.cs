using BetterLyrics.WinUI3.Enums;
using Microsoft.UI.Xaml.Data;
using System;
using WinUI3Localizer;

namespace BetterLyrics.WinUI3.Converter
{
    public partial class AlbumArtSearchProviderToDisplayNameConverter : IValueConverter
    {
        private readonly ILocalizer _localizer = Localizer.Get();

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is AlbumArtSearchProvider provider)
            {
                return provider switch
                {
                    AlbumArtSearchProvider.Local => _localizer.GetLocalizedString("AlbumArtSearchLocalProvider"),
                    AlbumArtSearchProvider.SMTC => _localizer.GetLocalizedString("AlbumArtSearchSMTCProvider"),
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
