using BetterLyrics.WinUI3.Services.ResourceService;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace BetterLyrics.WinUI3.Helper
{
    public class ToastHelper
    {
        private static readonly IResourceService _resourceService = Ioc.Default.GetRequiredService<IResourceService>();

        public static void ShowToast(string localizedTitleKey, string? description, InfoBarSeverity severity)
        {
            AppNotification notification = new AppNotificationBuilder()
                .AddText(_resourceService.GetLocalizedString(localizedTitleKey))
                .AddText(description)
                .BuildNotification();

            AppNotificationManager.Default.Show(notification);
        }
    }
}
