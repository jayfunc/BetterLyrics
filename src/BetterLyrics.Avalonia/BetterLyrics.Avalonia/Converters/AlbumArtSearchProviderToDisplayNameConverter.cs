using Avalonia.Data.Converters;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BetterLyrics.Avalonia.Converters
{
    public class AlbumArtSearchProviderToDisplayNameConverter : IValueConverter
    {
        private readonly ILocalizationService _localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is AlbumArtSearchProvider provider)
                return provider switch
                {
                    AlbumArtSearchProvider.Local => _localizationService.GetLocalizedString("AlbumArtSearchLocalProvider"),
                    AlbumArtSearchProvider.SMTC => _localizationService.GetLocalizedString("AlbumArtSearchSMTCProvider"),
                    AlbumArtSearchProvider.iTunes => "iTunes",
                    AlbumArtSearchProvider.Kugou => "酷狗音乐",
                    //AlbumArtSearchProvider.Netease => "网易云音乐",
                    _ => throw new Exception($"Unknown AlbumArtSearchProvider: {provider}")
                };
            throw new ArgumentException("Value must be of type AlbumArtSearchProvider", nameof(value));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
