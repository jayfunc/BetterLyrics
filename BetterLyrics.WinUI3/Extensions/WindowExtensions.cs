using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services.LocalizationService;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class WindowExtensions
    {
        private static readonly ILocalizationService _localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();

        extension(Window window)
        {
            public void Init(
                string titleKey = "",
                string title = "",
                TitleBarHeightOption titleBarHeightOption = TitleBarHeightOption.Standard,
                BackdropType backdropType = BackdropType.DesktopAcrylic)
            {
                if (titleKey != "")
                {
                    window.Title = _localizationService.GetLocalizedString(titleKey);
                }
                if (title != "")
                {
                    window.Title = title;
                }
                window.AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;
                window.AppWindow.SetIcons();

                window.ExtendsContentIntoTitleBar = true;
                window.AppWindow.TitleBar.PreferredHeightOption = titleBarHeightOption;

                window.SystemBackdrop = SystemBackdropHelper.CreateSystemBackdrop(backdropType);
            }

        }
    }
}
