// 2025/6/23 by Zhe Fang

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converter
{
    /// <summary>
    /// Defines the <see cref="MatchedLocalFilesPathToVisibilityConverter" />
    /// </summary>
    public partial class MatchedLocalFilesPathToVisibilityConverter : IValueConverter
    {
        #region Methods

        /// <summary>
        /// The Convert
        /// </summary>
        /// <param name="value">The value<see cref="object"/></param>
        /// <param name="targetType">The targetType<see cref="Type"/></param>
        /// <param name="parameter">The parameter<see cref="object"/></param>
        /// <param name="language">The language<see cref="string"/></param>
        /// <returns>The <see cref="object"/></returns>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string path)
            {
                if (path == App.ResourceLoader!.GetString("MainPageNoLocalFilesMatched"))
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

        /// <summary>
        /// The ConvertBack
        /// </summary>
        /// <param name="value">The value<see cref="object"/></param>
        /// <param name="targetType">The targetType<see cref="Type"/></param>
        /// <param name="parameter">The parameter<see cref="object"/></param>
        /// <param name="language">The language<see cref="string"/></param>
        /// <returns>The <see cref="object"/></returns>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
