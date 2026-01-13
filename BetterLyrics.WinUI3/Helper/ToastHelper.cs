using BetterLyrics.WinUI3.Services.LocalizationService;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace BetterLyrics.WinUI3.Helper
{
    public class ToastHelper
    {
        private static readonly ILocalizationService _localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();

        public static void ShowToast(string localizedTitleKey, string? description, InfoBarSeverity severity)
        {
            AppNotification notification = new AppNotificationBuilder()
                .AddText(_localizationService.GetLocalizedString(localizedTitleKey))
                .AddText(description)
                .BuildNotification();

            AppNotificationManager.Default.Show(notification);
        }
    }
}
