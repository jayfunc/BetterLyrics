using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using WinUIEx;

namespace BetterLyrics.WinUI3.Extensions;

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

            if (titleKey != "") window.Title = localizationService.GetLocalizedString(titleKey);

            if (title != "") window.Title = title;

            window.Title += $" - {Core.Constants.App.AppName}";

            window.SystemBackdrop = backdropType switch
            {
                BackdropType.None => null,
                BackdropType.Mica => new MicaBackdrop { Kind = MicaKind.Base },
                BackdropType.MicaAlt => new MicaBackdrop { Kind = MicaKind.BaseAlt },
                BackdropType.DesktopAcrylic => new DesktopAcrylicBackdrop(),
                BackdropType.Transparent => new TransparentTintBackdrop(),
                _ => null
            };

            var appWindow = window.AppWindow;
            appWindow.SetIcons();

            var titleBar = appWindow.TitleBar;
            titleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;
            titleBar.ExtendsContentIntoTitleBar = true;
            titleBar.PreferredHeightOption = titleBarHeightOption;
            window.SetTitleBarBackgroundColors(Colors.Transparent);
        }

        public void SyncTheme()
        {
            var settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
            if (settingsService == null || window == null || window.Content == null) return;

            var appTheme = settingsService.AppSettings.GeneralSettings.AppTheme;
            window.AppWindow.TitleBar.PreferredTheme = appTheme.ToTitleBarTheme();
            ((FrameworkElement)window.Content).RequestedTheme = appTheme.ToElementTheme();
        }
    }
}