using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services.LocalizationService;
using BetterLyrics.WinUI3.Services.SettingsService;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class WindowExtensions
    {
        extension(Window window)
        {
            public void Init(
                string titleKey = "",
                string title = "",
                TitleBarHeightOption titleBarHeightOption = TitleBarHeightOption.Standard,
                BackdropType backdropType = BackdropType.DesktopAcrylic)
            {
                var localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();

                if (titleKey != "")
                {
                    window.Title = localizationService.GetLocalizedString(titleKey);
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

            public void SyncTheme()
            {
                var settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
                if (settingsService == null || window == null || window.Content == null) return;

                var appTheme = settingsService.AppSettings.GeneralSettings.AppTheme;
                window.AppWindow.TitleBar.PreferredTheme = appTheme.ToTitleBarTheme();
                ((FrameworkElement)window.Content).RequestedTheme = appTheme;
            }

        }
    }
}
