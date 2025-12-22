using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinUI3Localizer;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class WindowExtensions
    {
        private static readonly ILocalizer _localizer = Localizer.Get();

        extension(Window window)
        {
            public void Init(
                string titleKey,
                TitleBarHeightOption titleBarHeightOption = TitleBarHeightOption.Standard,
                BackdropType backdropType = BackdropType.DesktopAcrylic)
            {
                window.Title = _localizer.GetLocalizedString(titleKey);
                window.AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;
                window.AppWindow.SetIcons();

                window.ExtendsContentIntoTitleBar = true;
                window.AppWindow.TitleBar.PreferredHeightOption = titleBarHeightOption;

                window.SystemBackdrop = SystemBackdropHelper.CreateSystemBackdrop(backdropType);
            }

        }
    }
}
