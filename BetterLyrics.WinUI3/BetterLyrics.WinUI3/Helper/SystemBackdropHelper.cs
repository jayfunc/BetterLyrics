using BetterLyrics.WinUI3.Enums;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Media;

namespace BetterLyrics.WinUI3.Helper
{
    public class SystemBackdropHelper
    {
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
    }
}
