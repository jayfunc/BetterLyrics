// 2025/6/23 by Zhe Fang

using System;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converter
{
    /// <summary>
    /// Defines the <see cref="IntToCornerRadius" />
    /// </summary>
    public partial class IntToCornerRadius : IValueConverter
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
            if (value is int intValue && parameter is double controlHeight)
            {
                return new Microsoft.UI.Xaml.CornerRadius(intValue / 100f * controlHeight / 2);
            }
            return new Microsoft.UI.Xaml.CornerRadius(0);
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
