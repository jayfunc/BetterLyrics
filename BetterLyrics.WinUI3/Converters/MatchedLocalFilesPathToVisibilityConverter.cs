// 2025/6/23 by Zhe Fang


using BetterLyrics.WinUI3.Services.LocalizationService;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace BetterLyrics.WinUI3.Converters
{
    public partial class MatchedLocalFilesPathToVisibilityConverter : IValueConverter
    {
        private readonly ILocalizationService _localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string path)
            {
                if (path == _localizationService.GetLocalizedString("MainPageNoLocalFilesMatched"))
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
