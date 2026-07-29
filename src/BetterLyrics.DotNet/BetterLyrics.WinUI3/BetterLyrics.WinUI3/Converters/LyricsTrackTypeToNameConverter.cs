using System;
using BetterLyrics.Core.Enums;
using Microsoft.UI.Xaml.Data;
using Microsoft.Windows.ApplicationModel.Resources;

namespace BetterLyrics.WinUI3.Converters;

public class LyricsTrackTypeToNameConverter : IValueConverter
{
    private static readonly ResourceLoader _resourceLoader = new();

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is LyricsTrackType trackType)
        {
            return trackType switch
            {
                LyricsTrackType.Original => _resourceLoader.GetString("LyricsTrackTypeOriginal"),
                LyricsTrackType.Transliteration => _resourceLoader.GetString("LyricsTrackTypeTransliteration"),
                LyricsTrackType.Translation => _resourceLoader.GetString("LyricsTrackTypeTranslation"),
                _ => _resourceLoader.GetString("LyricsTrackTypeOriginal")
            };
        }

        return _resourceLoader.GetString("LyricsTrackTypeOriginal");
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
