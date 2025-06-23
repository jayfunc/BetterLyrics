// 2025/6/23 by Zhe Fang

using Microsoft.UI.Xaml.Data;
using System;

namespace BetterLyrics.WinUI3.Converter
{
    /// <summary>
    /// Defines the <see cref="EnumToIntConverter" />
    /// </summary>
    internal class EnumToIntConverter : IValueConverter
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
            if (value is Enum)
            {
                return System.Convert.ToInt32(value);
            }
            return 0;
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
            if (value is int && targetType.IsEnum)
            {
                return Enum.ToObject(targetType, value);
            }
            return Enum.ToObject(targetType, 0);
        }

        #endregion
    }
}
