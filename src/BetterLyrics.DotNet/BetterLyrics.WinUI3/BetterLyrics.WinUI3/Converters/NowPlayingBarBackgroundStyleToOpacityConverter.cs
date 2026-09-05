using System;
using Microsoft.UI.Xaml.Data;
using BetterLyrics.Core.Enums;

namespace BetterLyrics.WinUI3.Converters;

public partial class NowPlayingBarBackgroundStyleToOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is NowPlayingBarBackgroundStyle style)
        {
            return style switch
            {
                NowPlayingBarBackgroundStyle.Opaque => 1.0,
                NowPlayingBarBackgroundStyle.Translucent => 0.5,
                _ => 0.0,
            };
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
