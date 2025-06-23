// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Media;

namespace BetterLyrics.WinUI3.Helper
{
    /// <summary>
    /// Defines the <see cref="SystemBackdropHelper" />
    /// </summary>
    public class SystemBackdropHelper
    {
        #region Methods

        /// <summary>
        /// The CreateSystemBackdrop
        /// </summary>
        /// <param name="backdropType">The backdropType<see cref="BackdropType"/></param>
        /// <returns>The <see cref="SystemBackdrop?"/></returns>
        public static SystemBackdrop? CreateSystemBackdrop(BackdropType backdropType)
        {
            return backdropType switch
            {
                BackdropType.None => null,
                BackdropType.Mica => new MicaBackdrop { Kind = MicaKind.Base },
                BackdropType.MicaAlt => new MicaBackdrop { Kind = MicaKind.BaseAlt },
                BackdropType.DesktopAcrylic => new DesktopAcrylicBackdrop(),
                BackdropType.Transparent => new WinUIEx.TransparentTintBackdrop(),
                _ => null,
            };
        }

        #endregion
    }
}
