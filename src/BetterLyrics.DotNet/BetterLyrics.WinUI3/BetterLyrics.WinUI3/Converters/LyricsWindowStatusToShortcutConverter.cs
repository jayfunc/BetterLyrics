using System;
using System.Linq;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converters;

public partial class LyricsWindowStatusToShortcutConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is LyricsWindowStatus status)
        {
            var settingsService = Ioc.Default.GetService<ISettingsService>();
            if (settingsService == null) return string.Empty;

            var records = settingsService.AppSettings.WindowBoundsRecords;
            int index = records.IndexOf(status);

            if (index == -1) return string.Empty;

            if (index < 9)
            {
                return (index + 1).ToString();
            }
            else if (index == 9)
            {
                return "0";
            }
            else if (index < 36)
            {
                return ((char)('A' + (index - 10))).ToString();
            }
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
