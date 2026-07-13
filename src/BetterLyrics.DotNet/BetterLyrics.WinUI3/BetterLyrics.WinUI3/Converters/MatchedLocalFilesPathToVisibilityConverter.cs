// 2025/6/23 by Zhe Fang


using System;
using BetterLyrics.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converters;

public partial class MatchedLocalFilesPathToVisibilityConverter : IValueConverter
{
    private readonly ILocalizationService _localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string path)
        {
            if (path == _localizationService.GetLocalizedString("MainPageNoLocalFilesMatched"))
                return Visibility.Collapsed;

            return Visibility.Visible;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}