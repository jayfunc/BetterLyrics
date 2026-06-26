using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models.Domain;

namespace BetterLyrics.Core.Interfaces.Providers
{
    public interface ISystemUIProvider
    {
        (string, AppRect) GetPrimaryMonitorInfo();
        AppColor GetAccentColor(IntPtr myHwnd, WindowPixelSampleMode mode);

        void ShowToast(string localizedTitleKey, string? message = null,
            MessageSeverity severity = MessageSeverity.Informational, TimeSpan? duration = null);
    }
}