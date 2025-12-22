using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using WinUI3Localizer;

namespace BetterLyrics.WinUI3.Helper
{
    public class ToastHelper
    {
        private static readonly ILocalizer _localizer = Localizer.Get();

        public static void ShowToast(string localizedTitleKey, string? description, InfoBarSeverity severity)
        {
            AppNotification notification = new AppNotificationBuilder()
                .AddText(_localizer.GetLocalizedString(localizedTitleKey))
                .AddText(description)
                .BuildNotification();

            AppNotificationManager.Default.Show(notification);
        }
    }
}
