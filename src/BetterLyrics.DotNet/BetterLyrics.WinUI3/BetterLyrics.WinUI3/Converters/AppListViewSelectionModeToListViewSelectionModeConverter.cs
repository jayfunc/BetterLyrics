using System;
using BetterLyrics.Core.Enums;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converters;

public partial class AppListViewSelectionModeToListViewSelectionModeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is AppListViewSelectionMode appListViewSelectionMode)
        {
            return appListViewSelectionMode switch
            {
                AppListViewSelectionMode.None => ListViewSelectionMode.None,
                AppListViewSelectionMode.Single => ListViewSelectionMode.Single,
                AppListViewSelectionMode.Multiple => ListViewSelectionMode.Multiple,
                AppListViewSelectionMode.Extended => ListViewSelectionMode.Extended,
                _ => ListViewSelectionMode.None
            };
        }

        return ListViewSelectionMode.None;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is ListViewSelectionMode listViewSelectionMode)
        {
            return listViewSelectionMode switch
            {
                ListViewSelectionMode.None => AppListViewSelectionMode.None,
                ListViewSelectionMode.Single => AppListViewSelectionMode.Single,
                ListViewSelectionMode.Multiple => AppListViewSelectionMode.Multiple,
                ListViewSelectionMode.Extended => AppListViewSelectionMode.Extended,
                _ => AppListViewSelectionMode.None
            };
        }

        return ListViewSelectionMode.None;
    }
}