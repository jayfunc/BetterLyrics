using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Services.LocalizationService;
using BetterLyrics.WinUI3.Services.SettingsService;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.UI;

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
                BackdropType backdropType = BackdropType.DesktopAcrylic,
                bool isBorderless = false)
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

                if (isBorderless && window.Content is FrameworkElement rootElement)
                {
                    rootElement.Loaded += (s, e) =>
                    {
                        window.SetIsBorderless(true);
                    };
                }

                window.AppWindow.TitleBar.BackgroundColor = Colors.Transparent;
                window.AppWindow.TitleBar.InactiveBackgroundColor = Colors.Transparent;
                window.AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                window.AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                window.AppWindow.TitleBar.ButtonHoverBackgroundColor = Colors.Transparent;
                window.AppWindow.TitleBar.ButtonPressedBackgroundColor = Colors.Transparent;

                window.SystemBackdrop = SystemBackdropHelper.CreateSystemBackdrop(backdropType);
            }

            public void SyncTheme()
            {
                var settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
                if (settingsService == null || window == null || window.Content == null) return;

                var appTheme = settingsService.AppSettings.GeneralSettings.AppTheme;
                window.AppWindow.TitleBar.PreferredTheme = appTheme.ToTitleBarTheme();
                ((FrameworkElement)window.Content).RequestedTheme = appTheme;

                window.AppWindow.TitleBar.BackgroundColor = Colors.Transparent;
                window.AppWindow.TitleBar.InactiveBackgroundColor = Colors.Transparent;
                window.AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                window.AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                window.AppWindow.TitleBar.ButtonHoverBackgroundColor = Colors.Transparent;
                window.AppWindow.TitleBar.ButtonPressedBackgroundColor = Colors.Transparent;
            }

        }
    }
}
