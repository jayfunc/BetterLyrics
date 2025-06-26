// 2025/6/23 by Zhe Fang

using System;
using BetterLyrics.WinUI3.Enums;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converter
{
    /// <summary>
    /// Defines the <see cref="LyricsSearchProviderToDisplayNameConverter" />
    /// </summary>
    public class LyricsSearchProviderToDisplayNameConverter : IValueConverter
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
            if (value is LyricsSearchProvider provider)
            {
                return provider switch
                {
                    LyricsSearchProvider.LrcLib => App.ResourceLoader!.GetString(
                        "LyricsSearchProviderLrcLib"
                    ),
                    //LyricsSearchProvider.AmllTtmlDb => App.ResourceLoader!.GetString(
                    //    "LyricsSearchProviderAmllTtmlDb"
                    //),
                    LyricsSearchProvider.LocalLrcFile => App.ResourceLoader!.GetString(
                        "LyricsSearchProviderLocalLrcFile"
                    ),
                    LyricsSearchProvider.LocalMusicFile => App.ResourceLoader!.GetString(
                        "LyricsSearchProviderLocalMusicFile"
                    ),
                    LyricsSearchProvider.LocalEslrcFile => App.ResourceLoader!.GetString(
                        "LyricsSearchProviderEslrcFile"
                    ),
                    LyricsSearchProvider.LocalTtmlFile => App.ResourceLoader!.GetString(
                        "LyricsSearchProviderTtmlFile"
                    ),
                    _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null),
                };
            }
            return "";
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
