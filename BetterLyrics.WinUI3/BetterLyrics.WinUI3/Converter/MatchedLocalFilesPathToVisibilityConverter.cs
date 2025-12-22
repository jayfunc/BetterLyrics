// 2025/6/23 by Zhe Fang


using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using WinUI3Localizer;

namespace BetterLyrics.WinUI3.Converter
{
    public partial class MatchedLocalFilesPathToVisibilityConverter : IValueConverter
    {
        private readonly ILocalizer _localizer = Localizer.Get();

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string path)
            {
                if (path == _localizer.GetLocalizedString("MainPageNoLocalFilesMatched"))
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
