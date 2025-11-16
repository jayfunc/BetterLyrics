using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services.ResourceService;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class WindowExtensions
    {
        private static readonly IResourceService _resourceService = Ioc.Default.GetRequiredService<IResourceService>();

        extension(Window window)
        {
            public void Init(
                string titleKey,
                TitleBarHeightOption titleBarHeightOption = TitleBarHeightOption.Standard,
                BackdropType backdropType = BackdropType.DesktopAcrylic)
            {
                window.Title = _resourceService.GetLocalizedString(titleKey);
                window.AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;
                window.AppWindow.SetIcons();

                window.ExtendsContentIntoTitleBar = true;
                window.AppWindow.TitleBar.PreferredHeightOption = titleBarHeightOption;

                window.SystemBackdrop = SystemBackdropHelper.CreateSystemBackdrop(backdropType);
            }

        }
    }
}
