using System;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converters;

public partial class AlbumArtSearchProviderToDisplayNameConverter : IValueConverter
{
    private readonly ILocalizationService _localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is AlbumArtProvider provider)
            return provider switch
            {
                AlbumArtProvider.Local => _localizationService.GetLocalizedString("AlbumArtSearchLocalProvider"),
                AlbumArtProvider.SMTC => _localizationService.GetLocalizedString("AlbumArtSearchSMTCProvider"),
                AlbumArtProvider.iTunes => "iTunes",
                AlbumArtProvider.Kugou => "酷狗音乐",
                AlbumArtProvider.LastFm => "Last.fm",
                //AlbumArtSearchProvider.Netease => "网易云音乐",
                _ => throw new Exception($"Unknown AlbumArtSearchProvider: {provider}")
            };
        throw new ArgumentException("Value must be of type AlbumArtSearchProvider", nameof(value));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}