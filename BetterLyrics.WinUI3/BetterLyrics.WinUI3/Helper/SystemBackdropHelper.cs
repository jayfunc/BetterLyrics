using DevWinUI;
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
                BackdropType.Mica => new MicaSystemBackdrop(MicaKind.Base),
                BackdropType.MicaAlt => new MicaSystemBackdrop(MicaKind.BaseAlt),
                BackdropType.DesktopAcrylic => new DesktopAcrylicBackdrop(),
                BackdropType.AcrylicThin => new AcrylicSystemBackdrop(DesktopAcrylicKind.Thin),
                BackdropType.AcrylicBase => new AcrylicSystemBackdrop(DesktopAcrylicKind.Base),
                BackdropType.Transparent => new TransparentBackdrop(),
                _ => null,
            };
        }
    }
}
